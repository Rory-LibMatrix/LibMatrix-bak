using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LibMatrix.EventTypes;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Homeservers;
using LibMatrix.Interfaces;

namespace LibMatrix.Responses;

public class CreateRoomRequest {
    [JsonIgnore] public CreationContentBaseType _creationContentBaseType;

    public CreateRoomRequest() => _creationContentBaseType = new CreationContentBaseType(this);

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("room_alias_name")]
    public string RoomAliasName { get; set; } = null!;

    //we dont want to use this, we want more control
    // [JsonPropertyName("preset")]
    // public string Preset { get; set; } = null!;

    [JsonPropertyName("initial_state")]
    public List<StateEvent> InitialState { get; set; } = null!;

    /// <summary>
    /// One of: ["public", "private"]
    /// </summary>
    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = null!;

    [JsonPropertyName("power_level_content_override")]
    public RoomPowerLevelEventContent PowerLevelContentOverride { get; set; } = null!;

    [JsonPropertyName("creation_content")]
    public JsonObject CreationContent { get; set; } = new();

    [JsonPropertyName("invite")]
    public List<string>? Invite { get; set; }

    /// <summary>
    ///     For use only when you can't use the CreationContent property
    /// </summary>

    public StateEvent this[string event_type, string event_key = ""] {
        get {
            var stateEvent = InitialState.FirstOrDefault(x => x.Type == event_type && x.StateKey == event_key);
            if (stateEvent == null) {
                InitialState.Add(stateEvent = new StateEvent {
                    Type = event_type,
                    StateKey = event_key,
                    TypedContent = (EventContent)Activator.CreateInstance(
                        StateEvent.KnownStateEventTypes.FirstOrDefault(x =>
                            x.GetCustomAttributes<MatrixEventAttribute>()?
                                .Any(y => y.EventName == event_type) ?? false) ?? typeof(object)
                    )
                });
            }

            return stateEvent;
        }
        set {
            var stateEvent = InitialState.FirstOrDefault(x => x.Type == event_type && x.StateKey == event_key);
            if (stateEvent == null)
                InitialState.Add(value);
            else
                InitialState[InitialState.IndexOf(stateEvent)] = value;
        }
    }

    public Dictionary<string, string> Validate() {
        Dictionary<string, string> errors = new();
        if (!Regex.IsMatch(RoomAliasName, @"[a-zA-Z0-9_\-]+$"))
            errors.Add("room_alias_name",
                "Room alias name must only contain letters, numbers, underscores, and hyphens.");

        return errors;
    }

    public static CreateRoomRequest CreatePublic(AuthenticatedHomeserverGeneric hs, string? name = null, string? roomAliasName = null) {
        var request = new CreateRoomRequest {
            Name = name ?? "New public Room",
            Visibility = "public",
            CreationContent = new(),
            PowerLevelContentOverride = new() {
                EventsDefault = 0,
                UsersDefault = 0,
                Kick = 50,
                Ban = 50,
                Invite = 25,
                StateDefault = 10,
                Redact = 50,
                NotificationsPl = new() {
                    Room = 10
                },
                Events = new() {
                    { "m.room.avatar", 50 },
                    { "m.room.canonical_alias", 50 },
                    { "m.room.encryption", 100 },
                    { "m.room.history_visibility", 100 },
                    { "m.room.name", 50 },
                    { "m.room.power_levels", 100 },
                    { "m.room.server_acl", 100 },
                    { "m.room.tombstone", 100 }
                },
                Users = new() {
                    {
                        hs.UserId,
                        101
                    }
                }
            },
            RoomAliasName = roomAliasName,
            InitialState = new()
        };

        return request;
    }
    public static CreateRoomRequest CreatePrivate(AuthenticatedHomeserverGeneric hs, string? name = null, string? roomAliasName = null) {
        var request = new CreateRoomRequest {
            Name = name ?? "New private Room",
            Visibility = "private",
            CreationContent = new(),
            PowerLevelContentOverride = new() {
                EventsDefault = 0,
                UsersDefault = 0,
                Kick = 50,
                Ban = 50,
                Invite = 25,
                StateDefault = 10,
                Redact = 50,
                NotificationsPl = new() {
                    Room = 10
                },
                Events = new() {
                    { "m.room.avatar", 50 },
                    { "m.room.canonical_alias", 50 },
                    { "m.room.encryption", 100 },
                    { "m.room.history_visibility", 100 },
                    { "m.room.name", 50 },
                    { "m.room.power_levels", 100 },
                    { "m.room.server_acl", 100 },
                    { "m.room.tombstone", 100 }
                },
                Users = new() {
                    {
                        hs.UserId,
                        101
                    }
                }
            },
            RoomAliasName = roomAliasName,
            InitialState = new()
        };

        return request;
    }
}
