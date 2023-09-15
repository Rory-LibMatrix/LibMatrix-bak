using LibMatrix.Extensions;
using LibMatrix.Helpers;
using LibMatrix.Services;

namespace LibMatrix.Homeservers;

public class AuthenticatedHomeserverMxApiExtended(TieredStorageService storage, string canonicalHomeServerDomain, string accessToken) : AuthenticatedHomeserverGeneric(storage, canonicalHomeServerDomain,
    accessToken);
