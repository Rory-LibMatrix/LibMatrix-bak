using LibMatrix.Interfaces.Services;

namespace LibMatrix.Services;

public class TieredStorageService(IStorageProvider? cacheStorageProvider, IStorageProvider? dataStorageProvider) {
    public IStorageProvider? CacheStorageProvider { get; } = cacheStorageProvider;
    public IStorageProvider? DataStorageProvider { get; } = dataStorageProvider;
}
