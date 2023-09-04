using LibMatrix.Extensions;
using LibMatrix.Helpers;
using LibMatrix.Services;

namespace LibMatrix.Homeservers;

public class AuthenticatedHomeserverMxApiExtended : AuthenticatedHomeserverGeneric {
    public AuthenticatedHomeserverMxApiExtended(TieredStorageService storage, string canonicalHomeServerDomain, string accessToken) : base(storage, canonicalHomeServerDomain, accessToken) {
        AccessToken = accessToken.Trim();
        HomeServerDomain = canonicalHomeServerDomain.Trim();
        SyncHelper = new SyncHelper(this, storage);
        _httpClient = new MatrixHttpClient();
    }
}
