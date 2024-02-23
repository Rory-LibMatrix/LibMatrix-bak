using LibMatrix.EventTypes.Spec.State;

namespace LibMatrix.HomeserverEmulator.Services;

public class UserStore(RoomStore roomStore) {
    public List<User> _users = new();
    private Dictionary<string, User> _usersById = new();
    private Dictionary<string, User> _usersByToken = new();

    private void RebuildIndexes() {
        _usersById = _users.ToDictionary(u => u.UserId);
        _usersByToken = _users.ToDictionary(u => u.AccessToken);
    }

    public async Task<User?> GetUserById(string userId, bool createIfNotExists = false) {
        if (_usersById.TryGetValue(userId, out var user)) {
            return user;
        }

        if (!createIfNotExists)
            return null;

        return await CreateUser(userId, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new Dictionary<string, object>());
    }

    public async Task<User?> GetUserByToken(string token, bool createIfNotExists = false, string? serverName = null) {
        if (_usersByToken.TryGetValue(token, out var user)) {
            return user;
        }

        if (!createIfNotExists)
            return null;
        if (string.IsNullOrWhiteSpace(serverName)) throw new NullReferenceException("Server name was not passed");
        var uid = $"@{Guid.NewGuid().ToString()}:{serverName}";
        return await CreateUser(uid, Guid.NewGuid().ToString(), token, new Dictionary<string, object>());
    }

    public async Task<User> CreateUser(string userId, string deviceId, string accessToken, Dictionary<string, object> profile) {
        if (!profile.ContainsKey("displayname")) profile.Add("displayname", userId.Split(":")[0]);
        if (!profile.ContainsKey("avatar_url")) profile.Add("avatar_url", null);
        var user = new User {
            UserId = userId,
            DeviceId = deviceId,
            AccessToken = accessToken,
            Profile = profile
        };
        _users.Add(user);
        RebuildIndexes();

        if (roomStore._rooms.Count > 0)
            foreach (var item in Random.Shared.GetItems(roomStore._rooms.ToArray(), Math.Min(roomStore._rooms.Count, 400))) {
                item.AddUser(userId);
            }

        int random = Random.Shared.Next(10);
        for (int i = 0; i < random; i++) {
            var room = roomStore.CreateRoom(new());
            room.AddUser(userId);
        }

        return user;
    }

    public class User {
        public string UserId { get; set; }
        public string AccessToken { get; set; }
        public string DeviceId { get; set; }
        public Dictionary<string, object> Profile { get; set; }

        public List<string> JoinedRooms { get; set; } = new();
    }
}