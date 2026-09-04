using System.Collections.Concurrent;

namespace FanWebsiteAPI.Infrastructure
{    
    public class PresenceTracker
    {
        private readonly ConcurrentDictionary<string, int> _onlineUsers = new();

        public void UserConnected(string userId)
        {
            _onlineUsers.AddOrUpdate(userId, 1, (_, count) => count + 1);
        }

        public void UserDisconnected(string userId)
        {
            _onlineUsers.AddOrUpdate(userId, 0, (_, count) => Math.Max(0, count - 1));
            if (_onlineUsers.TryGetValue(userId, out var count) && count <= 0)
                _onlineUsers.TryRemove(userId, out _);
        }

        public bool IsOnline(string userId) => _onlineUsers.ContainsKey(userId);

        public HashSet<string> GetOnline(IEnumerable<string> userIds) =>
            userIds.Where(_onlineUsers.ContainsKey).ToHashSet();
    }
}
