using LibMatrix.Services;

namespace LibMatrix.Helpers;

public static class MediaResolver {
    public static string ResolveMediaUri(string homeserver, string mxc) => mxc.Replace("mxc://", $"{homeserver}/_matrix/media/v3/download/");
}
