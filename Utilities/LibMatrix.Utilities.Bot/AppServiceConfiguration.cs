namespace PluralContactBotPoC;

public class AppServiceConfiguration {
    public string Id { get; set; } = null!;
    public string? Url { get; set; } = null!;
    public string SenderLocalpart { get; set; } = null!;
    public string AppserviceToken { get; set; } = null!;
    public string HomeserverToken { get; set; } = null!;
    public List<string>? Protocols { get; set; } = null!;
    public bool? RateLimited { get; set; } = null!;

    public AppserviceNamespaces Namespaces { get; set; } = null!;

    public class AppserviceNamespaces {
        public List<AppserviceNamespace>? Users { get; set; } = null;
        public List<AppserviceNamespace>? Aliases { get; set; } = null;
        public List<AppserviceNamespace>? Rooms { get; set; } = null;

        public class AppserviceNamespace {
            public bool Exclusive { get; set; }
            public string Regex { get; set; } = null!;
        }
    }

    /// <summary>
    /// Please dont look at code, it's horrifying but works
    /// </summary>
    /// <returns></returns>
    public string ToYaml() {
        var yaml = $"""
                    id: "{Id ?? throw new NullReferenceException("Id is null")}"
                    url: {(Url is null ? "null" : $"\"{Url}\"")}
                    as_token: "{AppserviceToken ?? throw new NullReferenceException("AppserviceToken is null")}"
                    hs_token: "{HomeserverToken ?? throw new NullReferenceException("HomeserverToken is null")}"
                    sender_localpart: "{SenderLocalpart ?? throw new NullReferenceException("SenderLocalpart is null")}"

                    """;

        if (Protocols is not null && Protocols.Count > 0)
            yaml += $"""
                     protocols:
                        - "{Protocols[0] ?? throw new NullReferenceException("Protocols[0] is null")}"
                     """;
        else
            yaml += "protocols: []";
        yaml += "\n";
        if (RateLimited is not null)
            yaml += $"rate_limited: {RateLimited!.ToString().ToLower()}\n";
        else
            yaml += "rate_limited: false\n";

        yaml += "namespaces: \n";

        if (Namespaces.Users is null || Namespaces.Users.Count == 0)
            yaml += "  users: []";
        else
            Namespaces.Users.ForEach(x =>
                yaml += $"""
                             users:
                                 - exclusive: {x.Exclusive.ToString().ToLower()}
                                   regex: "{x.Regex ?? throw new NullReferenceException("x.Regex is null")}"
                         """);
        yaml += "\n";

        if (Namespaces.Aliases is null || Namespaces.Aliases.Count == 0)
            yaml += "  aliases: []";
        else
            Namespaces.Aliases.ForEach(x =>
                yaml += $"""
                             aliases:
                                 - exclusive: {x.Exclusive.ToString().ToLower()}
                                   regex: "{x.Regex ?? throw new NullReferenceException("x.Regex is null")}"
                         """);
        yaml += "\n";
        if (Namespaces.Rooms is null || Namespaces.Rooms.Count == 0)
            yaml += "  rooms: []";
        else
            Namespaces.Rooms.ForEach(x =>
                yaml += $"""
                             rooms:
                                 - exclusive: {x.Exclusive.ToString().ToLower()}
                                   regex: "{x.Regex ?? throw new NullReferenceException("x.Regex is null")}"
                         """);

        return yaml;
    }
}
