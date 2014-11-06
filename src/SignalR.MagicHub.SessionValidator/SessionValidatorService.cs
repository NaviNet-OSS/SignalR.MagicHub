using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tracing;
using SignalR.MagicHub.Infrastructure;

namespace SignalR.MagicHub.SessionValidator
{
    /// <summary>
    /// Validates sessions and notifies MagicHub and AMQ of expirations
    /// </summary>
    public sealed class SessionValidatorService : ISessionValidatorService
    {

        /// <summary>
        /// Gets or sets the time provider.
        /// </summary>
        /// <value>
        /// The time provider.
        /// </value>
        public ISystemTime TimeProvider { get; set; }
        
        private readonly ConcurrentDictionary<string, ISessionState> _sessions = new ConcurrentDictionary<string, ISessionState>();
        private readonly ITraceManager _traceManager;
        private readonly SessionValidatorConfiguration _config;
        private readonly ISessionStateProvider _sessionStateProvider;


        private CancellationTokenSource _cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionValidatorService" /> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="traceManager">The trace manager.</param>
        /// <param name="sessionStateProvider">The session state provider.</param>
        public SessionValidatorService(SessionValidatorConfiguration config, ITraceManager traceManager, ISessionStateProvider sessionStateProvider)
        {
            TimeProvider = SystemTime.Current;
            _traceManager = traceManager;
            _sessionStateProvider = sessionStateProvider;
            _config = config;
        }

        //public SessionValidatorService(SessionValidatorConfiguration config)
        //    : this(config, GlobalHost.DependencyResolver.Resolve<IMessageHub>(), GlobalHost.DependencyResolver.Resolve<ITraceManager>())
        //{
            
        //}
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionValidatorService"/> class.
        /// </summary>
        //public SessionValidatorService()
        //    : this(new SessionValidatorConfiguration { SessionValidatorRunFrequency = 10, UserSessionExpirationSeconds = 120, WarnUserSessionExpiration = 60 }, GlobalHost.DependencyResolver.Resolve<IMessageHub>(), GlobalHost.DependencyResolver.Resolve<ITraceManager>())
        //{

        //}

        public bool IsRunning
        {
            get { return _cancellationToken != null && !_cancellationToken.IsCancellationRequested; }
        }

        /// <summary>
        /// Starts the service and begins checking sessions.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"><see cref="SessionValidatorService"/> is already running.</exception>
        public void Start()
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("SessionValidator is already running.");
            }

            Trace.TraceInformation("SessionValidator Service starting.");
            _cancellationToken = new CancellationTokenSource();

            // todo: continuewith to detect termination?
            RunSessionValidator(_config.SessionValidatorRunFrequencySeconds * 1000, _cancellationToken.Token);
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"><see cref="SessionValidatorService"/> is not running.</exception>
        public void Stop()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("SessionValidator is not running.");
            }
            Trace.TraceInformation("SessionValidator Service stopping.");
            _cancellationToken.CancelAfter(1);
            _cancellationToken = null;
        }

        /// <summary>
        /// Gets the currently tracked sessions.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ISessionState> GetTrackedSessions()
        {
            return _sessions.Values.ToArray();
        }

        /// <summary>
        /// Adds a session state to the set of sessions tracked by the service.
        /// </summary>
        /// <param name="sessionState">State of the session.</param>
        public void AddTrackedSession(ISessionState sessionState)
        {
            _sessions[sessionState.SessionKey] = sessionState;
        }

        /// <summary>
        /// Removes a session from the list of tracked sessions.
        /// </summary>
        /// <param name="sessionKey">The session key.</param>
        public void RemoveTrackedSession(string sessionKey)
        {
            ISessionState session;
            _sessions.TryRemove(sessionKey, out session);

        }

        /// <summary>
        /// Notifies MagicHub and other listeners that session should be terminated
        /// </summary>
        /// <param name="sessionKey">The session key.</param>
        /// <param name="reason">The reason.</param>
        /// <remarks>
        /// Right now only used in one place w/in this class, but eventual goal is to be able to invoke admin lockout maybe.
        /// don't factor out
        /// </remarks>
        public void KillSession(string sessionKey, SessionEndingReason reason)
        {
            // todo: amq notify 
            ISessionState session;
            _sessions.TryRemove(sessionKey, out session);
            RaiseSessionExpired(session, reason);
        }

        /// <summary>
        /// Resets the expiration time of a session to whatever its timeout is.
        /// </summary>
        /// <param name="cookies">The cookies associated with the session</param>
        public void KeepAlive(IDictionary<string, string> cookies)
        {
            string sessionId = _sessionStateProvider.GetSessionKey(cookies);

            KeepAlive(sessionId);
        }

        /// <summary>
        /// Resets the expiration time of a session to whatever its timeout is.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        public void KeepAlive(string sessionId)
        {
            var session = _sessions[sessionId];
            bool success = _sessionStateProvider.KeepAlive(sessionId, ref session);
            if (success)
            {
                _sessions[sessionId] = session;
            }
            RaiseSessionKeptAlive(session, success);
        }

        #region Private Methods

        private async void RunSessionValidator(int timeoutPeriod, CancellationToken token)
        {
            while (IsRunning)
            {
                Trace.TraceVerbose("SessionValidator checking sessions.");

                // do work
                CheckSessions();

                // wait on cancellation
                try
                {
                    await Task.Delay(timeoutPeriod, token);
                }
                catch (TaskCanceledException)
                {
                }
            }
        }

        private void CheckSessions()
        {
            var now = TimeProvider.Now.ToUniversalTime();
            foreach (var expiredSession in _sessions.Values.AsParallel().Where((s) => s.ExpiresBy(now)))
            {
                ISessionState externalSession = _sessionStateProvider.GetSessionState(expiredSession.SessionKey) ?? expiredSession;
                if (externalSession.ExpiresBy(now))
                {
                    KillSession(expiredSession.SessionKey, SessionEndingReason.EXPIRED);
                }
                else
                {
                    _sessions[expiredSession.SessionKey] = externalSession;
                }
            }

            var expiresBy = now + TimeSpan.FromSeconds(_config.WarnUserSessionExpirationSeconds);
            foreach (var expiringSession in _sessions.Values.AsParallel().Where((s) => s.ExpiresBy(expiresBy)))
            {
                ISessionState externalSession = _sessionStateProvider.GetSessionState(expiringSession.SessionKey) ?? expiringSession;
                _sessions[expiringSession.SessionKey] = externalSession;
                if (externalSession.ExpiresBy(expiresBy))
                {
                    RaiseSessionExpiring(expiringSession, expiringSession.Expires);
                }
            }
        }

        public event EventHandler<SessionStateEventArgs> SessionExpiring;
        public event EventHandler<SessionStateEventArgs> SessionExpired;
        public event EventHandler<SessionStateEventArgs> SessionKeptAlive;

        private void RaiseSessionExpiring(ISessionState sessionState, DateTime expiresAt)
        {
            if (SessionExpiring != null)
            {
                SessionExpiring(this, new SessionStateEventArgs(sessionState) { ExpiresAt = expiresAt });
            }
        }

        private void RaiseSessionExpired(ISessionState sessionState, SessionEndingReason reason)
        {
            if (SessionExpired != null)
            {
                SessionExpired(this, new SessionStateEventArgs(sessionState) { EventDetails = reason.ToString() });
            }
        }

        private void RaiseSessionKeptAlive(ISessionState sessionState, bool success)
        {
            if (SessionKeptAlive != null)
            {
                SessionKeptAlive(this, new SessionStateEventArgs(sessionState) { OperationSuccess = success });
            }
        }
        #endregion

        #region Private Properties

        private TraceSource Trace
        {
            get { return _traceManager["SignalR.MagicHub"]; }
        }
        #endregion
    }
}
