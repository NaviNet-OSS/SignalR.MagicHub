using System;

namespace SignalR.MagicHub.SessionValidator
{
    public interface ISessionState
    {
        string Username { get; }
        string SessionKey { get; }
        DateTime Expires { get; set; }
        bool ExpiresBy(DateTime utcTime);
    }
}
