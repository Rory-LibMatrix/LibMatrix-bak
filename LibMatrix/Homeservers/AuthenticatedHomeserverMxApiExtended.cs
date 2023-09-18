using LibMatrix.Extensions;
using LibMatrix.Helpers;
using LibMatrix.Services;

namespace LibMatrix.Homeservers;

public class AuthenticatedHomeserverMxApiExtended(string canonicalHomeServerDomain, string accessToken) : AuthenticatedHomeserverGeneric(canonicalHomeServerDomain,
    accessToken);
