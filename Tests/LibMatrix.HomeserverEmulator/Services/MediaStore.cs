using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ArcaneLibs;
using ArcaneLibs.Collections;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Responses;

namespace LibMatrix.HomeserverEmulator.Services;

public class MediaStore {
    private readonly HSEConfiguration _config;
    private List<MediaInfo> index = new();

    public MediaStore(HSEConfiguration config) {
        _config = config;
        if (config.StoreData) {
            var path = Path.Combine(config.DataStoragePath, "media");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            if(File.Exists(Path.Combine(path, "index.json")))
                index = JsonSerializer.Deserialize<List<MediaInfo>>(File.ReadAllText(Path.Combine(path, "index.json")));
        }
        else
            Console.WriteLine("Data storage is disabled, not loading rooms from disk");
    }

    public async Task<object> UploadMedia(string userId, string mimeType, Stream stream, string? filename = null) {
        var mediaId = $"mxc://{Guid.NewGuid().ToString()}";
        var path = Path.Combine(_config.DataStoragePath, "media", mediaId);
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        var file = Path.Combine(path, filename ?? "file");
        await using var fs = File.Create(file);
        await stream.CopyToAsync(fs);
        index.Add(new() {
            
        });
        return media;
    }

    public class MediaInfo { }
}