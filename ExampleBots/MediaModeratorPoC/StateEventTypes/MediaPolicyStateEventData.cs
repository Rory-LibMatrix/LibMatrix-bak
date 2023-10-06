using System.Text.Json.Serialization;
using LibMatrix.EventTypes;

namespace MediaModeratorPoC.StateEventTypes;

/// <summary>
///     File policy event, entity is the MXC URI of the file, hashed with SHA3-256.
/// </summary>
[MatrixEvent(EventName = "gay.rory.media_moderator_poc.rule.homeserver")]
[MatrixEvent(EventName = "gay.rory.media_moderator_poc.rule.media")]
public class MediaPolicyEventContent : BasePolicy {
    /// <summary>
    ///     Hash of the file
    /// </summary>
    [JsonPropertyName("file_hash")]
    public byte[]? FileHash { get; set; }
}
