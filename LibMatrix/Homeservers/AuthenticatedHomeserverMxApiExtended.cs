using LibMatrix.Extensions;
using LibMatrix.Helpers;
using LibMatrix.Services;

namespace LibMatrix.Homeservers;

public class AuthenticatedHomeserverMxApiExtended(string baseUrl, string accessToken) : AuthenticatedHomeserverGeneric(baseUrl, accessToken);
