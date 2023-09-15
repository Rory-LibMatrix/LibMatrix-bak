using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace MediaModeratorPoC.Bot.StateEventTypes;

[MatrixEvent(EventName = "gay.rory.media_moderator_poc.rule.homeserver")]
[MatrixEvent(EventName = "gay.rory.media_moderator_poc.rule.media")]
public class MediaPolicyEventContent : EventContent {
    /// <summary>
    ///     This is an MXC URI, hashed with SHA3-256.
    /// </summary>
    [JsonPropertyName("entity")]
    public byte[] Entity { get; set; }

    /// <summary>
    /// Server this ban applies to, can use * and ? as globs.
    /// </summary>
    [JsonPropertyName("server_entity")]
    public string? ServerEntity { get; set; }

    /// <summary>
    ///     Reason this user is banned
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    ///     Suggested action to take, one of `ban`, `kick`, `mute`, `redact`, `spoiler`, `warn` or `warn_admins`
    /// </summary>
    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = "warn";

    /// <summary>
    ///     Expiry time in milliseconds since the unix epoch, or null if the ban has no expiry.
    /// </summary>
    [JsonPropertyName("support.feline.policy.expiry.rev.2")] //stable prefix: expiry, msc pending
    public long? Expiry { get; set; }

    //utils
    /// <summary>
    ///     Readable expiry time, provided for easy interaction
    /// </summary>
    [JsonPropertyName("gay.rory.matrix_room_utils.readable_expiry_time_utc")]
    public DateTime? ExpiryDateTime {
        get => Expiry == null ? null : DateTimeOffset.FromUnixTimeMilliseconds(Expiry.Value).DateTime;
        set => Expiry = value is null ? null : ((DateTimeOffset)value).ToUnixTimeMilliseconds();
    }

    [JsonPropertyName("file_hash")]
    public byte[]? FileHash { get; set; }
}
