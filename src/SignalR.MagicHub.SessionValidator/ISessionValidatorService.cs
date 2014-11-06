using System;
using System.Collections.Generic;

namespace SignalR.MagicHub.SessionValidator
{
    /// <summary>
    /// Defines a service which tracks active sessions and notifies of expirations.
    /// </summary>
    public interface ISessionValidatorService
    {
        /// <summary>
        /// Gets a value indicating whether the service is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if service is running; otherwise, <c>false</c>.
        /// </value>
        bool IsRunning { get; }
        
        /// <summary>
        /// Starts the service and begins checking sessions.
        /// </summary>
        void Start();
        
        /// <summary>
        /// Stops the service.
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Gets the currently tracked sessions.
        /// </summary>
        /// <returns></returns>
        IEnumerable<ISessionState> GetTrackedSessions();

        /// <summary>
        /// Adds a session state to the set of sessions tracked by the service.
        /// </summary>
        /// <param name="sessionState">State of the session.</param>
        void AddTrackedSession(ISessionState sessionState);


        /// <summary>
        /// Removes a session from the list of tracked sessions.
        /// </summary>
        /// <param name="sessionKey">The session key.</param>
        void RemoveTrackedSession(string sessionKey);

        /// <summary>
        /// Notifies MagicHub and other listeners that session should be terminated
        /// </summary>
        /// <param name="sessionKey">The session key.</param>
        /// <param name="reason">The reason.</param>
        void KillSession(string sessionKey, SessionEndingReason reason);

        /// <summary>
        /// Resets the expiration time of a session to whatever its timeout is.
        /// </summary>
        /// <param name="cookies">The cookies associated with the session.</param>
        void KeepAlive(IDictionary<string, string> cookies);

        /// <summary>
        /// Resets the expiration time of a session to whatever its timeout is.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        void KeepAlive(string sessionId);


        event EventHandler<SessionStateEventArgs> SessionExpiring;
        event EventHandler<SessionStateEventArgs> SessionExpired;
        event EventHandler<SessionStateEventArgs> SessionKeptAlive;
    }
}