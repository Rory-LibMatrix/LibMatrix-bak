using System.Collections.Frozen;
using LibMatrix.EventTypes.Spec.State;
using LibMatrix.Filters;

namespace LibMatrix.Utilities;

public static class CommonSyncFilters {
    public const string GetAccountData = "gay.rory.libmatrix.get_account_data.v0";
    public const string GetAccountDataWithRooms = "gay.rory.libmatrix.get_account_data_with_rooms.v0";
    public const string GetBasicRoomInfo = "gay.rory.matrixutils.get_basic_room_info.v0";
    public const string GetSpaceRelations = "gay.rory.matrixutils.get_space_relations.v0";

    public static readonly SyncFilter GetAccountDataFilter = new() {
        Presence = new SyncFilter.EventFilter(notTypes: ["*"]),
        Room = new SyncFilter.RoomFilter() {
            Rooms = []
        }
    };

    public static readonly SyncFilter GetAccountDataWithRoomsFilter = new() {
        Presence = new SyncFilter.EventFilter(notTypes: ["*"]),
        Room = new SyncFilter.RoomFilter() {
            State = new SyncFilter.RoomFilter.StateFilter(notTypes: ["*"]),
            Ephemeral = new SyncFilter.RoomFilter.StateFilter(notTypes: ["*"]),
            Timeline = new SyncFilter.RoomFilter.StateFilter(notTypes: ["*"])
        }
    };

    public static readonly SyncFilter GetBasicRoomDataFilter = new() {
        AccountData = new SyncFilter.EventFilter(notTypes: ["*"], limit: 1),
        Presence = new SyncFilter.EventFilter(notTypes: ["*"], limit: 1),
        Room = new SyncFilter.RoomFilter {
            AccountData = new SyncFilter.RoomFilter.StateFilter(rooms: []),
            Ephemeral = new SyncFilter.RoomFilter.StateFilter(rooms: []),
            State = new SyncFilter.RoomFilter.StateFilter {
                Types = new List<string> {
                    "m.room.create",
                    "m.room.name",
                    "m.room.avatar",
                    "org.matrix.mjolnir.shortcode",
                    "m.room.power_levels"
                },
                LazyLoadMembers = true, IncludeRedundantMembers = false
            },
            Timeline = new SyncFilter.RoomFilter.StateFilter(rooms: [])
        }
    };

    public static readonly SyncFilter GetSpaceRelationsFilter = new() {
        AccountData = new SyncFilter.EventFilter(notTypes: ["*"], limit: 1),
        Presence = new SyncFilter.EventFilter(notTypes: ["*"], limit: 1),
        Room = new SyncFilter.RoomFilter {
            AccountData = new SyncFilter.RoomFilter.StateFilter(rooms: []),
            Ephemeral = new SyncFilter.RoomFilter.StateFilter(rooms: []),
            State = new SyncFilter.RoomFilter.StateFilter {
                Types = new List<string> {
                    "m.space.child",
                    "m.space.parent"
                },
                LazyLoadMembers = true, IncludeRedundantMembers = false
            },
            Timeline = new SyncFilter.RoomFilter.StateFilter(rooms: [])
        }
    };

    // This must be down here, due to statics load order
    public static readonly FrozenDictionary<string, SyncFilter> FilterMap = new Dictionary<string, SyncFilter>() {
        [GetAccountData] = GetAccountDataFilter,
        [GetAccountDataWithRooms] = GetAccountDataWithRoomsFilter,
        [GetBasicRoomInfo] = GetBasicRoomDataFilter,
        [GetSpaceRelations] = GetSpaceRelationsFilter
    }.ToFrozenDictionary();
}