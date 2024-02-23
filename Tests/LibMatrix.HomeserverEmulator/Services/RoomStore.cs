using System.Collections.Concurrent;
using System.Security.Cryptography;
using LibMatrix.EventTypes;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Responses;

namespace LibMatrix.HomeserverEmulator.Services;

public class RoomStore {
    public ConcurrentBag<Room> _rooms = new();
    private Dictionary<string, Room> _roomsById = new();

    private void RebuildIndexes() {
        _roomsById = _rooms.ToDictionary(u => u.RoomId);
    }

    public Room? GetRoomById(string roomId, bool createIfNotExists = false) {
        if (_roomsById.TryGetValue(roomId, out var user)) {
            return user;
        }

        if (!createIfNotExists)
            return null;

        return CreateRoom(new() { });
    }

    public Room CreateRoom(CreateRoomRequest request) {
        var room = new Room {
            RoomId = $"!{Guid.NewGuid().ToString()}"
        };
        if (!string.IsNullOrWhiteSpace(request.Name))
            room.SetStateInternal(new StateEvent() {
                Type = RoomNameEventContent.EventId,
                TypedContent = new RoomNameEventContent() {
                    Name = request.Name
                }
            });

        if (!string.IsNullOrWhiteSpace(request.RoomAliasName))
            room.SetStateInternal(new StateEvent() {
                Type = RoomCanonicalAliasEventContent.EventId,
                TypedContent = new RoomCanonicalAliasEventContent() {
                    Alias = $"#{request.RoomAliasName}:localhost"
                }
            });

        foreach (var stateEvent in request.InitialState ?? []) {
            room.SetStateInternal(stateEvent);
        }

        _rooms.Add(room);
        RebuildIndexes();
        return room;
    }

    public class Room {
        public string RoomId { get; set; }
        public List<StateEventResponse> State { get; set; } = new();
        public Dictionary<string, EventContent> Timeline { get; set; } = new();

        internal StateEventResponse SetStateInternal(StateEvent request) {
            var state = new StateEventResponse() {
                Type = request.Type,
                StateKey = request.StateKey,
                RawContent = request.RawContent,
                EventId = Guid.NewGuid().ToString()
            };
            State.Add(state);
            return state;
        }

        public StateEventResponse AddUser(string userId) {
            return SetStateInternal(new() {
                Type = RoomMemberEventContent.EventId,
                StateKey = userId,
                TypedContent = new RoomMemberEventContent() {
                    Membership = "join"
                }
            });
        }
    }
}