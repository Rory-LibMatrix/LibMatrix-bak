namespace LibMatrix.Homeservers.Extensions.NamedCaches;

public class NamedFileCache(AuthenticatedHomeserverGeneric hs) : NamedCache<string>(hs, "gay.rory.libmatrix.named_cache.media") { }