namespace LibMatrix.Homeservers.Extensions.NamedCaches;

public class NamedCache<T>(AuthenticatedHomeserverGeneric hs, string name) where T : class {
    private Dictionary<string, T>? _cache = new();
    private DateTime _expiry = DateTime.MinValue;
    
    public async Task<Dictionary<string, T>> ReadCacheMapAsync() {
        _cache = await hs.GetAccountDataOrNullAsync<Dictionary<string, T>>(name);

        return _cache ?? new();
    }
    
    public async Task<Dictionary<string,T>> ReadCacheMapCachedAsync() {
        if (_expiry < DateTime.Now || _cache == null) {
            _cache = await ReadCacheMapAsync();
            _expiry = DateTime.Now.AddMinutes(5);
        }

        return _cache;
    }
    
    public virtual async Task<T?> GetValueAsync(string key) {
        return (await ReadCacheMapCachedAsync()).GetValueOrDefault(key);
    }
    
    public virtual async Task<T> SetValueAsync(string key, T value) {
        var cache = await ReadCacheMapCachedAsync();
        cache[key] = value;
        await hs.SetAccountDataAsync(name, cache);

        return value;
    }
    
    public virtual async Task<T> GetOrSetValueAsync(string key, Func<Task<T>> value) {
        return (await ReadCacheMapCachedAsync()).GetValueOrDefault(key) ?? await SetValueAsync(key, await value());
    }
}