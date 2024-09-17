namespace LibMatrix.HomeserverEmulator.Services;

public class TokenService{
    public string? GetAccessTokenOrNull(HttpContext ctx) {
        //qry
        if (ctx.Request.Query.TryGetValue("access_token", out var token)) {
            return token;
        }
        //header
        if (ctx.Request.Headers.TryGetValue("Authorization", out var auth)) {
            var parts = auth.ToString().Split(' ');
            if (parts is ["Bearer", _]) {
                return parts[1];
            }
        }
        return null;
    }

    public string GetAccessToken(HttpContext ctx) {
        return GetAccessTokenOrNull(ctx) ?? throw new MatrixException() {
            ErrorCode = MatrixException.ErrorCodes.M_UNKNOWN_TOKEN,
            Error = "Missing token"
        };
    }

    public string? GenerateServerName(HttpContext ctx) {
        return ctx.Request.Host.ToString();
    }
}