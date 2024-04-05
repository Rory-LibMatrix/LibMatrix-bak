using LibMatrix.Services;

namespace LibMatrix.Homeservers;

public class AuthenticatedHomeserverMxApiExtended(string serverName, HomeserverResolverService.WellKnownUris wellKnownUris, ref string? proxy, string accessToken)
    : AuthenticatedHomeserverGeneric(serverName, wellKnownUris, ref proxy, accessToken);