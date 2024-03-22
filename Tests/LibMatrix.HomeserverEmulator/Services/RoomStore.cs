using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ArcaneLibs;
using ArcaneLibs.Collections;
using ArcaneLibs.Extensions;
using LibMatrix.EventTypes;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Responses;

namespace LibMatrix.HomeserverEmulator.Services;

public class RoomStore {
    public ConcurrentBag<Room> _rooms = new();
    private Dictionary<string, Room> _roomsById = new();

    public RoomStore(HSEConfiguration config) {
        if (config.StoreData) {
            var path = Path.Combine(config.DataStoragePath, "rooms");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            foreach (var file in Directory.GetFiles(path)) {
                var room = JsonSerializer.Deserialize<Room>(File.ReadAllText(file));
                if (room is not null) _rooms.Add(room);
            }
        }
        else
            Console.WriteLine("Data storage is disabled, not loading rooms from disk");

        RebuildIndexes();
    }

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
        var room = new Room(roomId: $"!{Guid.NewGuid().ToString()}");
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

    public class Room : NotifyPropertyChanged {
        private CancellationTokenSource _debounceCts = new();
        private ObservableCollection<StateEventResponse> _timeline;
        private ObservableDictionary<string, List<StateEventResponse>> _accountData;

        public Room(string roomId) {
            if (string.IsNullOrWhiteSpace(roomId)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(roomId));
            if (roomId[0] != '!') throw new ArgumentException("Room ID must start with !", nameof(roomId));
            RoomId = roomId;
            State = FrozenSet<StateEventResponse>.Empty;
            Timeline = new();
            AccountData = new();
        }

        public string RoomId { get; set; }

        public FrozenSet<StateEventResponse> State { get; private set; }

        public ObservableCollection<StateEventResponse> Timeline {
            get => _timeline;
            set {
                if (Equals(value, _timeline)) return;
                _timeline = new(value);
                _timeline.CollectionChanged += (sender, args) => {
                    if (args.Action == NotifyCollectionChangedAction.Add) {
                        foreach (StateEventResponse state in args.NewItems) {
                            if (state.StateKey is not null)
                                // we want state to be deduplicated by type and key, and we want the latest state to be the one that is returned
                                RebuildState();
                        }
                    }

                    SaveDebounced();
                };
                RebuildState();
                OnPropertyChanged();
            }
        }

        public ObservableDictionary<string, List<StateEventResponse>> AccountData {
            get => _accountData;
            set {
                if (Equals(value, _accountData)) return;
                _accountData = new(value);
                _accountData.CollectionChanged += (sender, args) => SaveDebounced();
                OnPropertyChanged();
            }
        }

        public ImmutableList<StateEventResponse> JoinedMembers =>
            State.Where(s => s is { Type: RoomMemberEventContent.EventId, TypedContent: RoomMemberEventContent { Membership: "join" } }).ToImmutableList();

        internal StateEventResponse SetStateInternal(StateEvent request) {
            var state = new StateEventResponse() {
                Type = request.Type,
                StateKey = request.StateKey,
                EventId = Guid.NewGuid().ToString(),
                RoomId = RoomId,
                OriginServerTs = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Sender = "",
                RawContent = request.RawContent ?? (request.TypedContent is not null
                    ? new JsonObject()
                    : JsonSerializer.Deserialize<JsonObject>(JsonSerializer.Serialize(request.TypedContent)))
            };
            Timeline.Add(state);
            if (state.StateKey is not null)
                // we want state to be deduplicated by type and key, and we want the latest state to be the one that is returned
                State = Timeline.Where(s => s.StateKey != null)
                    .OrderByDescending(s => s.OriginServerTs)
                    .DistinctBy(x => (x.Type, x.StateKey))
                    .ToFrozenSet();
            return state;
        }

        public StateEventResponse AddUser(string userId) {
            var state = SetStateInternal(new() {
                Type = RoomMemberEventContent.EventId,
                StateKey = userId,
                TypedContent = new RoomMemberEventContent() {
                    Membership = "join"
                },
            });

            state.Sender = userId;
            return state;
        }

        public async Task SaveDebounced() {
            if (!HSEConfiguration.Current.StoreData) return;
            await _debounceCts.CancelAsync();
            _debounceCts = new CancellationTokenSource();
            try {
                await Task.Delay(250, _debounceCts.Token);
                // Ensure all state events are in the timeline
                State.Where(s => !Timeline.Contains(s)).ToList().ForEach(s => Timeline.Add(s));
                var path = Path.Combine(HSEConfiguration.Current.DataStoragePath, "rooms", $"{RoomId}.json");
                Console.WriteLine($"Saving room {RoomId} to {path}!");
                await File.WriteAllTextAsync(path, this.ToJson(ignoreNull: true));
            }
            catch (TaskCanceledException) { }
        }

        private void RebuildState() {
            State = Timeline //.Where(s => s.Type == state.Type && s.StateKey == state.StateKey)
                .Where(x => x.StateKey != null)
                .OrderByDescending(s => s.OriginServerTs)
                .DistinctBy(x => (x.Type, x.StateKey))
                .ToFrozenSet();
        }
    }
}