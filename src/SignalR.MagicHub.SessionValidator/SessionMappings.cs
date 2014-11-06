using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SignalR.MagicHub.Infrastructure
{
    public interface ISessionMappings
    {
        /// <summary>
        /// Adds the session mapping.
        /// </summary>
        /// <param name="token">The session identifier.</param>
        /// <param name="connectionId">The connection identifier.</param>
        /// <returns>true if this is the only connectionId mapped to the session after the result of the operation</returns>
        bool AddOrUpdate(string token, string connectionId);

        /// <summary>
        /// Removes all session mappings for session id.
        /// </summary>
        /// <param name="token">The session identifier.</param>
        /// <param name="connectionsRemoved">The connections removed.</param>
        /// <returns></returns>
        bool TryRemoveAll(string token, out ICollection<string> connectionsRemoved);


        /// <summary>
        /// Removes the session mapping.
        /// </summary>
        /// <param name="token">The session identifier.</param>
        /// <param name="connectionId">The connection identifier.</param>
        /// <returns>true if the session contains no more connections as a result of the operation</returns>
        bool TryRemove(string token, string connectionId);

        /// <summary>
        /// Gets the connection ids.
        /// </summary>
        /// <param name="token">The session identifier.</param>
        /// <returns></returns>
        ICollection<string> GetConnectionIds(string token);
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class SessionMappings : ISessionMappings
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _sessionMappingStore = new ConcurrentDictionary<string, HashSet<string>>();

        /// <summary>
        /// Adds the session mapping.
        /// </summary>
        /// <param name="token">The session identifier.</param>
        /// <param name="connectionId">The connection identifier.</param>
        /// <returns>true if this is the only connectionId mapped to the session after the result of the operation</returns>
        public bool AddOrUpdate(string token, string connectionId)
        {
            return _sessionMappingStore.AddOrUpdate(
                token,
                (s) => new HashSet<string> { connectionId },
                (s, list) =>
                    {
                        lock (list)
                        {
                            list.Add(connectionId);
                            return list;
                        }
                    }
                ).Count == 1;
        }


        /// <summary>
        /// Removes all session mappings for session id.
        /// </summary>
        /// <param name="token">The session identifier.</param>
        /// <returns>true if successful</returns>
        public bool TryRemoveAll(string token, out ICollection<string> connectionsRemoved)
        {
            HashSet<string> tempList;
            bool rVal =_sessionMappingStore.TryRemove(token, out tempList);
            connectionsRemoved = tempList;
            return rVal;
        }

        /// <summary>
        /// Removes the session mapping.
        /// </summary>
        /// <param name="token">The session identifier.</param>
        /// <param name="connectionId">The connection identifier.</param>
        /// <returns>true if the session contains no more connections as a result of the operation</returns>
        public bool TryRemove(string token, string connectionId)
        {
            //_sessionMappingStore
            HashSet<string> list;
            if (_sessionMappingStore.TryGetValue(token, out list))
            {
                lock (list)
                {
                    list.Remove(connectionId);
                    if (list.Count == 0)
                    {
                        HashSet<string> tempList;
                        _sessionMappingStore.TryRemove(token, out tempList);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the connection ids.
        /// </summary>
        /// <param name="token">The session identifier.</param>
        /// <returns></returns>
        public ICollection<string> GetConnectionIds(string token)
        {
            HashSet<string> list;
            if (_sessionMappingStore.TryGetValue(token, out list))
            {
                lock (list)
                {
                    return list.ToArray();
                }
            }
            else
            {
                return new string[0];
            }
        }
    }
}