using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Tracing;
using SignalR.MagicHub.Infrastructure;
using SignalR.MagicHub.SessionValidator;

namespace SignalR.MagicHub
{
    /// <summary>
    /// Implementation of a SignalR hub that is responsible for receiving subscriptions to topics
    /// from the SignalR client component and publishing messages to connected clients. This class
    /// requires authorization in whatever form is loaded in the application.
    /// See web.config for authorization details. Also see <see cref="AuthorizeAttribute" />
    /// </summary>
    [Authorize]
    public class TopicBroker : Hub
    {
        public const string TopicDebug = "debug/mode";
        public const string TopicSessionExpired = "nnt:session/expired";
        public const string TopicSessionExpiring = "nnt:session/expiring";
        public const string TopicSessionKeepAlive = "nnt:session/keep-alive";
        public const string TopicSessionKeptAlive = "nnt:session/kept-alive";
        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="TopicBroker"/> class. Uses Dependencies from 
        /// the default <see cref="GlobalHost"/> resolver.
        /// </summary>
        public TopicBroker()
            : this(
                GlobalHost.DependencyResolver.Resolve<ITraceManager>(), 
                GlobalHost.DependencyResolver.Resolve<IMessageHub>(), 
                GlobalHost.DependencyResolver.Resolve<ISessionValidatorService>(),
                GlobalHost.DependencyResolver.Resolve<ISessionStateProvider>(),
                GlobalHost.DependencyResolver.Resolve<ISessionMappings>()) { }


        /// <summary>
        /// Initializes a new instance of the <see cref="TopicBroker" /> class.
        /// </summary>
        /// <param name="traceManager">The trace manager to use for tracing. Should contain a trace
        /// source named SignalR.MagicHub</param>
        /// <param name="messageHub">The message hub.</param>
        /// <param name="sessionValidatorService">The session validator service.</param>
        public TopicBroker(ITraceManager traceManager, IMessageHub messageHub, ISessionValidatorService sessionValidatorService, ISessionStateProvider sessionStateProvider, ISessionMappings sessionMappingStore)
        {
            _traceManager = traceManager;
            _messageHub = messageHub;
            _sessionValidatorService = sessionValidatorService;
            _sessionStateProvider = sessionStateProvider;
            _sessionToConnectionId = sessionMappingStore;
        }

        #endregion

        #region Private Properties

        private TraceSource Trace
        {
            get { return _traceManager[AppConstants.SignalRMagicHub]; }
        }

        #endregion

        #region Hub Implementation

        /// <summary>
        /// Called when connects.
        /// </summary>
        /// <returns></returns>
        public override Task OnConnected()
        {
            Trace.TraceVerbose(Context.ConnectionId + " connected.");
            if (Context.User != null && Context.User.Identity.IsAuthenticated)
            {
                var sessionState =
                    _sessionStateProvider.GetSessionState(Context.RequestCookies.ToDictionary((pair) => pair.Key,
                                                                                              (pair) => pair.Value.Value));
                _sessionToConnectionId.AddOrUpdate(sessionState.SessionKey, Context.ConnectionId);
                _sessionValidatorService.AddTrackedSession(sessionState);
            }
            return base.OnConnected();
        }

        /// <summary>
        /// Called when a client disconnects. This cleans up open connection data.
        /// </summary>
        /// <returns></returns>
        public override Task OnDisconnected()
        {
            Trace.TraceVerbose(Context.ConnectionId + " disconnected.");
            if (Context.User != null && Context.User.Identity.IsAuthenticated)
            {
                var sessionKey =
                    _sessionStateProvider.GetSessionKey(Context.RequestCookies.ToDictionary((pair) => pair.Key,
                                                                                              (pair) => pair.Value.Value));
                if (_sessionToConnectionId.TryRemove(sessionKey, Context.ConnectionId))
                {
                    _sessionValidatorService.RemoveTrackedSession(sessionKey);
                }
            }
            
            //remove from the subscription
            _messageHub.Unsubscribe(Context.ConnectionId);
            return base.OnDisconnected();
        }

        public override Task OnReconnected()
        {
            Trace.TraceVerbose(Context.ConnectionId + " reconnected.");
            if (Context.User != null && Context.User.Identity.IsAuthenticated)
            {
                var sessionState =
                    _sessionStateProvider.GetSessionState(Context.RequestCookies.ToDictionary((pair) => pair.Key,
                                                                                              (pair) => pair.Value.Value));
                _sessionToConnectionId.AddOrUpdate(sessionState.SessionKey, Context.ConnectionId);
                _sessionValidatorService.AddTrackedSession(sessionState);
            }
            return base.OnReconnected();
        }

        #endregion

        #region SignalR Actions

        /// <summary>
        ///     Sends message to message bus.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <param name="headers"></param>
        [Authorize(
            PermissionType = "write", 
            WhiteList = new [] { TopicDebug, TopicSessionKeepAlive }, 
            Blacklist = new [] { TopicSessionExpired, TopicSessionExpiring, TopicSessionKeptAlive })]
        public async Task Send(string topic, string message, IDictionary<string,object> headers = null)
        {
            if (topic == TopicSessionKeepAlive)
            {
                try
                {
                    var cookies = Context.RequestCookies.ToDictionary((pair) => pair.Key,
                                                                      (pair) => pair.Value.Value);
                    _sessionValidatorService.KeepAlive(cookies);
                    return;
                }
                catch (Exception ex)
                {
                    string errorMessage = "Error keeping session alive.";
                    Trace.TraceError(ex, errorMessage);
                    throw new Exception(errorMessage, ex);
                }
            }

            try
            {
                await _messageHub.Publish(topic, message, headers);
            }
            catch (Exception ex)
            {
                string errorMessage = "Error sending message. topic: " + topic + "; message: " + message;
                Trace.TraceError(ex, errorMessage);
                throw new Exception(errorMessage, ex);
            }
        }

        /// <summary>
        ///     Subscribes topic to message bus.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="filter"></param>
        [Authorize(
            PermissionType = "read", 
            Blacklist = new [] { TopicDebug }, 
            WhiteList = new [] { TopicSessionExpired, TopicSessionExpiring, TopicSessionKeptAlive })]
        public async Task Subscribe(string topic, string filter = null)
        {
            try
            {
                await _messageHub.Subscribe(Context.ConnectionId, topic, filter);
            }
            catch (Exception ex)
            {
                string message = "Error subscribing to topics: " + topic;
                Trace.TraceError(ex, message);
                throw new Exception(message, ex);
            }
        }

        /// <summary>
        ///     Unsubscribes topic from message bus.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="filter"></param>
        public async Task Unsubscribe(string topic, string filter = null)
        {
            try
            {
                await _messageHub.Unsubscribe(Context.ConnectionId, topic, filter);
            }
            catch (Exception ex)
            {
                string message = "Error unsubscribing from topic: " + topic;
                Trace.TraceError(ex, message);
                throw new Exception(message, ex);
            }
        }

        #endregion

        private readonly IMessageHub _messageHub;
        private readonly ITraceManager _traceManager;
        private readonly ISessionStateProvider _sessionStateProvider;
        private readonly ISessionValidatorService _sessionValidatorService;
        private readonly ISessionMappings _sessionToConnectionId;
    }
}