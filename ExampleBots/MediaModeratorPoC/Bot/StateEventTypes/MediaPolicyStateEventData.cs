using System.Text.Json.Serialization;
using LibMatrix.Helpers;
using LibMatrix.Interfaces;

namespace MediaModeratorPoC.Bot.StateEventTypes;


[MatrixEvent(EventName = "gay.rory.media_moderator_poc.rule.media")]
public class MediaPolicyStateEventData : IStateEventType {
    /// <summary>
    ///     Entity this ban applies to, can use * and ? as globs.
    ///     This is an MXC URI.
    /// </summary>
    [JsonPropertyName("entity")]
    public string Entity { get; set; }

    /// <summary>
    ///     Reason this user is banned
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    ///     Suggested action to take
    /// </summary>
    [JsonPropertyName("recommendation")]
    public string? Recommendation { get; set; }

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
        set => Expiry = ((DateTimeOffset)value).ToUnixTimeMilliseconds();
    }
}
