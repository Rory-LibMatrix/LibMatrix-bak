using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArcaneLibs.Extensions;

namespace LibMatrix.Extensions;

public static class HttpClientExtensions {
    public static async Task<bool> CheckSuccessStatus(this HttpClient hc, string url) {
        //cors causes failure, try to catch
        try {
            var resp = await hc.GetAsync(url);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception e) {
            Console.WriteLine($"Failed to check success status: {e.Message}");
            return false;
        }
    }
}

public class MatrixHttpClient : HttpClient {
    public Dictionary<string, string> AdditionalQueryParameters { get; set; } = new();
    internal string? AssertedUserId { get; set; }

    private JsonSerializerOptions GetJsonSerializerOptions(JsonSerializerOptions? options = null) {
        options ??= new JsonSerializerOptions();
        options.Converters.Add(new JsonFloatStringConverter());
        options.Converters.Add(new JsonDoubleStringConverter());
        options.Converters.Add(new JsonDecimalStringConverter());
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        return options;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        if (request.RequestUri is null) throw new NullReferenceException("RequestUri is null");
        if (!request.RequestUri.IsAbsoluteUri) request.RequestUri = new Uri(BaseAddress, request.RequestUri);
        // if (AssertedUserId is not null) request.RequestUri = request.RequestUri.AddQuery("user_id", AssertedUserId);
        foreach (var (key, value) in AdditionalQueryParameters) request.RequestUri = request.RequestUri.AddQuery(key, value);

        // Console.WriteLine($"Sending request to {request.RequestUri}");

        try {
            var webAssemblyEnableStreamingResponseKey =
                new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingResponse");
            request.Options.Set(webAssemblyEnableStreamingResponseKey, true);
        }
        catch (Exception e) {
            Console.WriteLine("Failed to set browser response streaming:");
            Console.WriteLine(e);
        }

        HttpResponseMessage responseMessage;
        // try {
        responseMessage = await base.SendAsync(request, cancellationToken);
        // }
        // catch (Exception e) {
        // if (requestSettings is { Retries: 0 }) throw;
        // typeof(HttpRequestMessage).GetField("_sendStatus", BindingFlags.NonPublic | BindingFlags.Instance)
        // ?.SetValue(request, 0);
        // await Task.Delay(requestSettings?.RetryDelay ?? 2500, cancellationToken);
        // if(requestSettings is not null) requestSettings.Retries--;
        // return await SendAsync(request, cancellationToken);
        // throw;
        // }

        if (responseMessage.IsSuccessStatusCode) return responseMessage;

        //error handling
        var content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        if (content.Length == 0)
            throw new MatrixException() {
                ErrorCode = "M_UNKNOWN",
                Error = "Unknown error, server returned no content"
            };
        if (!content.StartsWith('{')) throw new InvalidDataException("Encountered invalid data:\n" + content);
        //we have a matrix error
        var ex = JsonSerializer.Deserialize<MatrixException>(content);
        Debug.Assert(ex != null, nameof(ex) + " != null");
        ex.RawContent = content;
        // Console.WriteLine($"Failed to send request: {ex}");
        if (ex?.RetryAfterMs is null) throw ex!;
        //we have a ratelimit error
        await Task.Delay(ex.RetryAfterMs.Value, cancellationToken);
        typeof(HttpRequestMessage).GetField("_sendStatus", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(request, 0);
        return await SendAsync(request, cancellationToken);
    }

    // GetAsync
    public Task<HttpResponseMessage> GetAsync([StringSyntax("Uri")] string? requestUri, CancellationToken? cancellationToken = null) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken ?? CancellationToken.None);

    // GetFromJsonAsync
    public async Task<T> GetFromJsonAsync<T>(string requestUri, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default) {
        options = GetJsonSerializerOptions(options);
        // Console.WriteLine($"GetFromJsonAsync called for {requestUri} with json options {options?.ToJson(ignoreNull:true)} and cancellation token {cancellationToken}");
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
#if DEBUG && false // This is only used for testing, so it's disabled by default
        try {
            await PostAsync("http://localhost:5116/validate/" + typeof(T).AssemblyQualifiedName, new StreamContent(responseStream), cancellationToken);
        }
        catch (Exception e) {
            Console.WriteLine("[!!] Checking sync response failed: " + e);
        }
#endif
        return await JsonSerializer.DeserializeAsync<T>(responseStream, options, cancellationToken) ??
               throw new InvalidOperationException("Failed to deserialize response");
    }

    // GetStreamAsync
    public new async Task<Stream> GetStreamAsync(string requestUri, CancellationToken cancellationToken = default) {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsJsonAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, T value, JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default) where T : notnull {
        options = GetJsonSerializerOptions(options);
        var request = new HttpRequestMessage(HttpMethod.Put, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        Console.WriteLine($"Sending PUT {requestUri}");
        // Console.WriteLine($"Content: {JsonSerializer.Serialize(value, value.GetType(), options)}");
        // Console.WriteLine($"Type: {value.GetType().FullName}");
        request.Content = new StringContent(JsonSerializer.Serialize(value, value.GetType(), options),
            Encoding.UTF8, "application/json");
        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, T value, JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default) where T : notnull {
        options ??= new JsonSerializerOptions();
        options.Converters.Add(new JsonFloatStringConverter());
        options.Converters.Add(new JsonDoubleStringConverter());
        options.Converters.Add(new JsonDecimalStringConverter());
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(value, value.GetType(), options),
            Encoding.UTF8, "application/json");
        return await SendAsync(request, cancellationToken);
    }

    public async IAsyncEnumerable<T?> GetAsyncEnumerableFromJsonAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, JsonSerializerOptions? options = null) {
        options = GetJsonSerializerOptions(options);
        var res = await GetAsync(requestUri);
        var result = JsonSerializer.DeserializeAsyncEnumerable<T>(await res.Content.ReadAsStreamAsync(), options);
        await foreach (var resp in result) yield return resp;
    }
}

public class JsonFloatStringConverter : JsonConverter<float> {
    public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => float.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
}

public class JsonDoubleStringConverter : JsonConverter<double> {
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => double.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
}

public class JsonDecimalStringConverter : JsonConverter<decimal> {
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => decimal.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
}