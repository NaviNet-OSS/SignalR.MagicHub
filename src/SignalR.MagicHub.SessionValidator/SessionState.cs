using System;

namespace SignalR.MagicHub.SessionValidator
{
    public class SessionState : ISessionState
    {
        public SessionState(string sessionKey, string username, DateTime expires)
        {
            SessionKey = sessionKey;
            Username = username;
            Expires = expires;
        }
        public string Username { get; private set; }
        public string SessionKey { get; private set; }
        public DateTime Expires { get; set; }
        public bool ExpiresBy(DateTime utcTime)
        {
            return Expires <= utcTime;
        }

        public override bool Equals(object obj)
        {
            return obj is SessionState && ((SessionState)obj).SessionKey == SessionKey;
        }

        public override int GetHashCode()
        {
            return SessionKey.GetHashCode();
        }
    }
}