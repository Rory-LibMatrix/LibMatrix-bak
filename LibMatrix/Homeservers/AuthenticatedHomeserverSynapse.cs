using ArcaneLibs.Extensions;
using LibMatrix.Filters;
using LibMatrix.Responses.Admin;

namespace LibMatrix.Homeservers;

public class AuthenticatedHomeserverSynapse : AuthenticatedHomeserverGeneric {
    public readonly SynapseAdminApi Admin;
    public class SynapseAdminApi {
        private readonly AuthenticatedHomeserverGeneric _authenticatedHomeserver;

        public SynapseAdminApi(AuthenticatedHomeserverGeneric authenticatedHomeserver) => _authenticatedHomeserver = authenticatedHomeserver;

        public async IAsyncEnumerable<AdminRoomListingResult.AdminRoomListingResultRoom> SearchRoomsAsync(int limit = int.MaxValue, string orderBy = "name", string dir = "f", string? searchTerm = null, LocalRoomQueryFilter? localFilter = null) {
            AdminRoomListingResult? res = null;
            var i = 0;
            int? totalRooms = null;
            do {
                var url = $"/_synapse/admin/v1/rooms?limit={Math.Min(limit, 100)}&dir={dir}&order_by={orderBy}";
                if (!string.IsNullOrEmpty(searchTerm)) url += $"&search_term={searchTerm}";

                if (res?.NextBatch is not null) url += $"&from={res.NextBatch}";

                Console.WriteLine($"--- ADMIN Querying Room List with URL: {url} - Already have {i} items... ---");

                res = await _authenticatedHomeserver.ClientHttpClient.GetFromJsonAsync<AdminRoomListingResult>(url);
                totalRooms ??= res?.TotalRooms;
                Console.WriteLine(res.ToJson(false));
                foreach (var room in res.Rooms) {
                    if (localFilter is not null) {
                        if (!room.RoomId.Contains(localFilter.RoomIdContains)) {
                            totalRooms--;
                            continue;
                        }
                        if (!room.Name?.Contains(localFilter.NameContains) == true) {
                            totalRooms--;
                            continue;
                        }
                        if (!room.CanonicalAlias?.Contains(localFilter.CanonicalAliasContains) == true) {
                            totalRooms--;
                            continue;
                        }
                        if (!room.Version.Contains(localFilter.VersionContains)) {
                            totalRooms--;
                            continue;
                        }
                        if (!room.Creator.Contains(localFilter.CreatorContains)) {
                            totalRooms--;
                            continue;
                        }
                        if (!room.Encryption?.Contains(localFilter.EncryptionContains) == true) {
                            totalRooms--;
                            continue;
                        }
                        if (!room.JoinRules?.Contains(localFilter.JoinRulesContains) == true) {
                            totalRooms--;
                            continue;
                        }
                        if(!room.GuestAccess?.Contains(localFilter.GuestAccessContains) == true) {
                            totalRooms--;
                            continue;
                        }
                        if(!room.HistoryVisibility?.Contains(localFilter.HistoryVisibilityContains) == true) {
                            totalRooms--;
                            continue;
                        }

                        if(localFilter.CheckFederation && room.Federatable != localFilter.Federatable) {
                            totalRooms--;
                            continue;
                        }
                        if(localFilter.CheckPublic && room.Public != localFilter.Public) {
                            totalRooms--;
                            continue;
                        }

                        if(room.JoinedMembers < localFilter.JoinedMembersGreaterThan || room.JoinedMembers > localFilter.JoinedMembersLessThan) {
                            totalRooms--;
                            continue;
                        }
                        if(room.JoinedLocalMembers < localFilter.JoinedLocalMembersGreaterThan || room.JoinedLocalMembers > localFilter.JoinedLocalMembersLessThan) {
                            totalRooms--;
                            continue;
                        }
                    }
                    // if (contentSearch is not null && !string.IsNullOrEmpty(contentSearch) &&
                    //     !(
                    //         room.Name?.Contains(contentSearch, StringComparison.InvariantCultureIgnoreCase) == true ||
                    //         room.CanonicalAlias?.Contains(contentSearch, StringComparison.InvariantCultureIgnoreCase) == true ||
                    //         room.Creator?.Contains(contentSearch, StringComparison.InvariantCultureIgnoreCase) == true
                    //     )
                    //    ) {
                    //     totalRooms--;
                    //     continue;
                    // }

                    i++;
                    yield return room;
                }
            } while (i < Math.Min(limit, totalRooms ?? limit));
        }
    }

    public AuthenticatedHomeserverSynapse(string baseUrl, string accessToken) : base(baseUrl, accessToken) {
        Admin = new(this);
    }
}
