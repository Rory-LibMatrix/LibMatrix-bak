namespace LibMatrix.HomeserverEmulator.Services;

public class TokenService(IHttpContextAccessor accessor) {
    public string? GetAccessToken() {
        var ctx = accessor.HttpContext;
        if (ctx is null) return null;
        //qry
        if (ctx.Request.Query.TryGetValue("access_token", out var token)) {
            return token;
        }
        //header
        if (ctx.Request.Headers.TryGetValue("Authorization", out var auth)) {
            var parts = auth.ToString().Split(' ');
            if (parts.Length == 2 && parts[0] == "Bearer") {
                return parts[1];
            }
        }
        return null;
    }

    public string? GenerateServerName() {
        var ctx = accessor.HttpContext;
        if (ctx is null) return null;
        return ctx.Request.Host.ToString();
    }
}