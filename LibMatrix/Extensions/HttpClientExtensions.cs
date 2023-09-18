using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
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
    internal string? AssertedUserId { get; set; }

    public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken) {
        if (request.RequestUri is null) throw new NullReferenceException("RequestUri is null");
        if (AssertedUserId is not null) request.RequestUri = request.RequestUri.AddQuery("user_id", AssertedUserId);

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

        var a = await base.SendAsync(request, cancellationToken);
        if (a.IsSuccessStatusCode) return a;

        //error handling
        var content = await a.Content.ReadAsStringAsync(cancellationToken);
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

    // GetFromJsonAsync
    public async Task<T> GetFromJsonAsync<T>(string requestUri, CancellationToken cancellationToken = default) {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(responseStream, cancellationToken: cancellationToken);
    }

    // GetStreamAsync
    public new async Task<Stream> GetStreamAsync(string requestUri, CancellationToken cancellationToken = default) {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public new async Task<HttpResponseMessage> PutAsJsonAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, T value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default) {
        var request = new HttpRequestMessage(HttpMethod.Put, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(value, value.GetType()), Encoding.UTF8, "application/json");
        return await SendAsync(request, cancellationToken);
    }
}
