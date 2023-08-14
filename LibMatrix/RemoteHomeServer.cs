using LibMatrix.Extensions;
using LibMatrix.Interfaces;

namespace LibMatrix;

public class RemoteHomeServer : IHomeServer {
    public RemoteHomeServer(string canonicalHomeServerDomain) {
        HomeServerDomain = canonicalHomeServerDomain;
        _httpClient = new MatrixHttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

}
