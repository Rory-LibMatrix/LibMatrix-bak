using LibMatrix.Services;

namespace LibMatrix.Homeservers;

public class AuthenticatedHomeserverMxApiExtended(string serverName, HomeserverResolverService.WellKnownUris wellKnownUris, string? proxy, string accessToken)
    : AuthenticatedHomeserverGeneric(serverName, wellKnownUris, proxy, accessToken);