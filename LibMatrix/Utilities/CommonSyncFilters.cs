using System.Collections.Frozen;
using LibMatrix.LegacyEvents.EventTypes.Common;
using LibMatrix.LegacyEvents.EventTypes.Spec.State;
using LibMatrix.LegacyEvents.EventTypes.Spec.State.RoomInfo;
using LibMatrix.Filters;

namespace LibMatrix.Utilities;

public static class CommonSyncFilters {
    public const string GetAccountData = "gay.rory.libmatrix.get_account_data.v0";
    public const string GetAccountDataWithRooms = "gay.rory.libmatrix.get_account_data_with_rooms.v0";
    public const string GetBasicRoomInfo = "gay.rory.matrixutils.get_basic_room_info.v0";
    public const string GetSpaceRelations = "gay.rory.matrixutils.get_space_relations.v0";
    public const string GetOwnMemberEvents = "gay.rory.matrixutils.get_own_member_events.v0";

    public static MatrixFilter GetAccountDataFilter => new() {
        Presence = new MatrixFilter.EventFilter(notTypes: ["*"]),
        Room = new MatrixFilter.RoomFilter() {
            Rooms = []
        }
    };

    public static MatrixFilter GetAccountDataWithRoomsFilter => new() {
        Presence = new MatrixFilter.EventFilter(notTypes: ["*"]),
        Room = new MatrixFilter.RoomFilter() {
            State = new MatrixFilter.RoomFilter.StateFilter(notTypes: ["*"]),
            Ephemeral = new MatrixFilter.RoomFilter.StateFilter(notTypes: ["*"]),
            Timeline = new MatrixFilter.RoomFilter.StateFilter(notTypes: ["*"])
        }
    };

    public static MatrixFilter GetBasicRoomDataFilter => new() {
        AccountData = new MatrixFilter.EventFilter(notTypes: ["*"], limit: 1),
        Presence = new MatrixFilter.EventFilter(notTypes: ["*"], limit: 1),
        Room = new MatrixFilter.RoomFilter {
            AccountData = new MatrixFilter.RoomFilter.StateFilter(rooms: []),
            Ephemeral = new MatrixFilter.RoomFilter.StateFilter(rooms: []),
            State = new MatrixFilter.RoomFilter.StateFilter {
                Types = new List<string> {
                    RoomCreateLegacyEventContent.EventId,
                    RoomNameLegacyEventContent.EventId,
                    RoomAvatarLegacyEventContent.EventId,
                    MjolnirShortcodeLegacyEventContent.EventId,
                    RoomPowerLevelLegacyEventContent.EventId
                },
                LazyLoadMembers = true, IncludeRedundantMembers = false
            },
            Timeline = new MatrixFilter.RoomFilter.StateFilter(rooms: [])
        }
    };

    public static MatrixFilter GetSpaceRelationsFilter => new() {
        AccountData = new MatrixFilter.EventFilter(notTypes: ["*"], limit: 1),
        Presence = new MatrixFilter.EventFilter(notTypes: ["*"], limit: 1),
        Room = new MatrixFilter.RoomFilter {
            AccountData = new MatrixFilter.RoomFilter.StateFilter(rooms: []),
            Ephemeral = new MatrixFilter.RoomFilter.StateFilter(rooms: []),
            State = new MatrixFilter.RoomFilter.StateFilter {
                Types = new List<string> {
                    SpaceChildLegacyEventContent.EventId,
                    SpaceParentLegacyEventContent.EventId
                },
                LazyLoadMembers = true, IncludeRedundantMembers = false
            },
            Timeline = new MatrixFilter.RoomFilter.StateFilter(rooms: [])
        }
    };

    public static MatrixFilter GetOwnMemberEventsFilter => new() {
        AccountData = new MatrixFilter.EventFilter(types: ["m.room.member"], limit: 1),
        Presence = new MatrixFilter.EventFilter(notTypes: ["*"], limit: 1),
        Room = new MatrixFilter.RoomFilter {
            AccountData = new MatrixFilter.RoomFilter.StateFilter(rooms: []),
            Ephemeral = new MatrixFilter.RoomFilter.StateFilter(rooms: []),
            State = new MatrixFilter.RoomFilter.StateFilter {
                Types = new List<string> {
                    RoomMemberLegacyEventContent.EventId
                },
                LazyLoadMembers = true, IncludeRedundantMembers = false,
                Senders = ["@me"]
            },
            Timeline = new MatrixFilter.RoomFilter.StateFilter(rooms: [])
        }
    };

    // This must be down here, due to statics load order
    public static FrozenDictionary<string, MatrixFilter> FilterMap => new Dictionary<string, MatrixFilter>() {
        [GetAccountData] = GetAccountDataFilter,
        [GetAccountDataWithRooms] = GetAccountDataWithRoomsFilter,
        [GetBasicRoomInfo] = GetBasicRoomDataFilter,
        [GetSpaceRelations] = GetSpaceRelationsFilter,
        [GetOwnMemberEvents] = GetOwnMemberEventsFilter
    }.ToFrozenDictionary();
}