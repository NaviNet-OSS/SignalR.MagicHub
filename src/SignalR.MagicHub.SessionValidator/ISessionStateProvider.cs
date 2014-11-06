using System.Collections.Generic;

namespace SignalR.MagicHub.SessionValidator
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISessionStateProvider
    {
        /// <summary>
        /// Gets the state of the session.
        /// </summary>
        /// <param name="requestCookies">The request cookies.</param>
        /// <returns>A session state; if session is already past expiration and out of the store, then returns null.</returns>
        ISessionState GetSessionState(IDictionary<string, string> requestCookies);

        /// <summary>
        /// Gets the state of the session.
        /// </summary>
        /// <param name="token">The session token.</param>
        /// <returns>A session state; if session is already past expiration and out of the store, then returns null.</returns>
        ISessionState GetSessionState(string token);

        /// <summary>
        /// Gets the session key.
        /// </summary>
        /// <param name="requestCookies">The request cookies containing the session token</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Request cookies doesn't contain a NaviNet cookie.;requestCookies</exception>
        string GetSessionKey(IDictionary<string, string> requestCookies);

        /// <summary>
        /// Keeps a session alive, resetting its expiration
        /// </summary>
        /// <param name="requestCookies">The request cookies.</param>
        /// <param name="sessionState">State of the session. Updated after return if operation succeeded</param>
        /// <returns>
        /// If <c>true</c> - success, and <c>sessionState has been updated</c>; otherwise false
        /// </returns>
        bool KeepAlive(IDictionary<string, string> requestCookies, ref ISessionState sessionState);

        /// <summary>
        /// Keeps a session alive, resetting its expiration
        /// </summary>
        /// <param name="token">The session token.</param>
        /// <param name="sessionState">State of the session. Updated after return if operation succeeded</param>
        /// <returns>
        /// If <c>true</c> - success, and <c>sessionState has been updated</c>; otherwise false
        /// </returns>
        bool KeepAlive(string token, ref ISessionState sessionState);

    }
}
