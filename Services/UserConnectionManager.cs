using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SmartRoomFinder.Services
{
    public interface IUserConnectionManager
    {
        void KeepUserConnection(string userId, string connectionId);
        void RemoveUserConnection(string connectionId);
        List<string> GetUserConnections(string userId);
        bool IsUserOnline(string userId);
    }

    public class UserConnectionManager : IUserConnectionManager
    {
        // Dictionary mapping UserId -> List of ConnectionIds
        private static ConcurrentDictionary<string, List<string>> userConnectionMap = new ConcurrentDictionary<string, List<string>>();
        private static string userConnectionMapLocker = string.Empty;

        public void KeepUserConnection(string userId, string connectionId)
        {
            lock (userConnectionMapLocker)
            {
                if (!userConnectionMap.ContainsKey(userId))
                {
                    userConnectionMap[userId] = new List<string>();
                }
                userConnectionMap[userId].Add(connectionId);
            }
        }

        public void RemoveUserConnection(string connectionId)
        {
            lock (userConnectionMapLocker)
            {
                foreach (var userId in userConnectionMap.Keys)
                {
                    if (userConnectionMap.ContainsKey(userId))
                    {
                        if (userConnectionMap[userId].Contains(connectionId))
                        {
                            userConnectionMap[userId].Remove(connectionId);
                            if (userConnectionMap[userId].Count == 0)
                            {
                                userConnectionMap.TryRemove(userId, out _);
                            }
                            break;
                        }
                    }
                }
            }
        }

        public List<string> GetUserConnections(string userId)
        {
            var conn = new List<string>();
            lock (userConnectionMapLocker)
            {
                if (userConnectionMap.ContainsKey(userId))
                {
                    conn = userConnectionMap[userId];
                }
            }
            return conn;
        }

        public bool IsUserOnline(string userId)
        {
            lock (userConnectionMapLocker)
            {
                return userConnectionMap.ContainsKey(userId) && userConnectionMap[userId].Count > 0;
            }
        }
    }
}
