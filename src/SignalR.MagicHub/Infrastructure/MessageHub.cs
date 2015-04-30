using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;
using SignalR.MagicHub.MessageContracts;
using SignalR.MagicHub.Messaging;
using SignalR.MagicHub.Performance;
using SignalR.MagicHub.SessionValidator;

namespace SignalR.MagicHub.Infrastructure
{
    /// <summary>
    ///     MessageHub provides singleton access to MessageBus
    /// </summary>
    /// <remarks>
    ///     Internally keep tracks of connection and it's subscriptions. Helps in cleaning up the resources.
    /// </remarks>
    public class MessageHub : IMessageHub
    {
        private readonly IMessageBus _messageBus;

        private readonly ConcurrentDictionary<string, List<string>> _subscribedSelectorsForConnection =
            new ConcurrentDictionary<string, List<string>>();
        private readonly SemaphoreSlim _subscriptionMutex = new SemaphoreSlim(1);

        private readonly ConcurrentDictionary<string, uint> _subscriptionsToSelector =
            new ConcurrentDictionary<string, uint>();

        private readonly ITraceManager _traceManager;

        private readonly TraceStrategy _traceStrategy = new TraceStrategy();
        private readonly ISessionMappings _sessionMappings;
        private readonly IMagicHubPerformanceCounterManager _counters;
        private bool _unblockGroupSend;

        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHub"/> class.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <param name="connectionManager">The connection manager.</param>
        public MessageHub(IDependencyResolver resolver, IConnectionManager connectionManager)
            : this(
                resolver.Resolve<IMessageBus>() ?? new MessageBus(),
                connectionManager,
                resolver.Resolve<ITraceManager>(),
                resolver.Resolve<ISessionValidatorService>(),
                resolver.Resolve<ISessionMappings>(),
                resolver.Resolve<IMagicHubPerformanceCounterManager>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHub"/> class.
        /// </summary>
        /// <param name="messageBus">The message bus.</param>
        /// <param name="connectionManager">The connection manager.</param>
        /// <param name="traceManager">The trace manager.</param>
        /// <param name="sessionValidatorService">The session validator service.</param>
        /// <param name="sessionMappings">The session mappings.</param>
        /// <param name="counters">The counters.</param>
        /// <param name="unblockGroupSend">if set to <c>true</c> [unblock group send].</param>
        public MessageHub(
            IMessageBus messageBus, 
            IConnectionManager connectionManager, 
            ITraceManager traceManager,
            ISessionValidatorService sessionValidatorService, 
            ISessionMappings sessionMappings,
            IMagicHubPerformanceCounterManager counters, 
            bool unblockGroupSend)
            : this(messageBus, connectionManager, traceManager,sessionValidatorService,sessionMappings,counters)
        {
            _unblockGroupSend = unblockGroupSend;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHub"/> class.
        /// </summary>
        /// <param name="messageBus">The message bus.</param>
        /// <param name="connectionManager">The connection manager.</param>
        /// <param name="traceManager">The trace manager.</param>
        /// <param name="sessionValidatorService">The session validator service.</param>
        /// <param name="sessionMappings">The session mappings.</param>
        /// <param name="counters">The counters.</param>
        public MessageHub(
            IMessageBus messageBus, 
            IConnectionManager connectionManager, 
            ITraceManager traceManager, 
            ISessionValidatorService sessionValidatorService,
            ISessionMappings sessionMappings, 
            IMagicHubPerformanceCounterManager counters)
        {
            var context = connectionManager.GetHubContext<TopicBroker, ITopicBrokerClientProxy>();
            _messageBus = messageBus;
            Clients = context.Clients;
            Groups = context.Groups;
            _traceManager = traceManager;
            _sessionMappings = sessionMappings;
            _counters = counters;
            if (sessionValidatorService != null)
            {
                sessionValidatorService.SessionExpired += SessionValidatorServiceOnSessionExpired;
                sessionValidatorService.SessionKeptAlive += SessionValidatorServiceOnSessionKeptAlive;
                sessionValidatorService.SessionExpiring += SessionValidatorServiceSessionExpiring;
            }
        }

        private void SessionValidatorServiceSessionExpiring(object sender, SessionStateEventArgs e)
        {
            var msg = new SessionExpiringMessage { ExpiresAt = e.ExpiresAt };
            DispatchConnections(_sessionMappings.GetConnectionIds(e.SessionState.SessionKey), TopicBroker.TopicSessionExpiring, msg.ToString());
            msg.User = e.SessionState;
            // todo: AMQ notify
            MessageBus.Publish(TopicBroker.TopicSessionExpiring, msg.ToString());
        }

        private void SessionValidatorServiceOnSessionKeptAlive(object sender, SessionStateEventArgs e)
        {
            var msg = new SessionKeptAliveMessage { Status = e.OperationSuccess ? "succeeded" : "failed" };
            DispatchConnections(_sessionMappings.GetConnectionIds(e.SessionState.SessionKey), TopicBroker.TopicSessionKeptAlive, msg.ToString());

            msg.User = e.SessionState;
            MessageBus.Publish(TopicBroker.TopicSessionKeptAlive, msg.ToString());
        }

        private void SessionValidatorServiceOnSessionExpired(object sender, SessionStateEventArgs e)
        {
            var msg = new SessionExpiredMessage { Status = e.EventDetails };
            ICollection<string> associatedConnections;
            if (_sessionMappings.TryRemoveAll(e.SessionState.SessionKey, out associatedConnections))
            {
                DispatchConnections(associatedConnections, TopicBroker.TopicSessionExpired, msg.ToString());
                msg.User = e.SessionState;
                MessageBus.Publish(TopicBroker.TopicSessionExpired, msg.ToString());
                foreach (string connectionId in associatedConnections)
                {
                    Unsubscribe(connectionId);
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the hub connection context for performing invocations on clients
        /// </summary>
        /// <value>
        /// The clients.
        /// </value>
        protected IHubConnectionContext<ITopicBrokerClientProxy> Clients { get; private set; }

        /// <summary>
        /// Gets the group manager proxy for communicating with clients
        /// </summary>
        /// <value>
        /// The groups.
        /// </value>
        protected IGroupManager Groups { get; private set; }

        /// <summary>
        /// Gets the message bus.
        /// </summary>
        /// <value>
        /// The message bus.
        /// </value>
        protected IMessageBus MessageBus
        {
            get { return _messageBus; }
        }

        /// <summary>
        /// Gets the trace source.
        /// </summary>
        /// <value>
        /// The trace.
        /// </value>
        protected TraceSource Trace
        {
            get { return _traceManager[AppConstants.SignalRMagicHub]; }
        }

        /// <summary>
        /// Gets the trace strategy which helps determine whether message should be logged
        /// </summary>
        /// <value>
        /// The trace strategy.
        /// </value>
        protected TraceStrategy TraceStrategy
        {
            get { return _traceStrategy; }
        }

        #endregion


        #region IMessageHub

        /// <summary>
        ///     Subscribe topic to message bus
        /// </summary>
        /// <param name="connectionId">The SignalR connectionID associated with this subscription.</param>
        /// <param name="topic">The topic, without filter, with nnt: qualifier, if applicable.</param>
        /// <returns></returns>
        public async Task Subscribe(string connectionId, string topic)
        {
            await Subscribe(connectionId, topic, null);
        }

        /// <summary>
        ///     Subscribe topic to message bus
        /// </summary>
        /// <param name="connectionId">The SignalR connectionID associated with this subscription.</param>
        /// <param name="topic">The topic, without filter, with nnt: qualifier, if applicable.</param>
        /// <param name="filter">The filter, in SQL-92 format.</param>
        /// <returns></returns>
        public async Task Subscribe(string connectionId, string topic, string filter)
        {
            Trace.TraceVerbose("Subscribe called. ConnectionId={0} Topic={1} Filter={2}", connectionId, topic, filter);
            var subscription = new SubscriptionIdentifier(topic, filter);

            //Trace.TraceVerbose("Subscribe. Entering Monitor. Conn: {0}, Topic: {1}", connectionId, topic);
            try
            {
                await _subscriptionMutex.WaitAsync();
            }
            catch (Exception)
            {
                Trace.TraceInformation("MessageHub failed to get mutex during subscription. ConnectionId={0} Topic={1} Filter={2}", connectionId, topic, filter);
                throw;
            }
            //Trace.TraceVerbose("Subscribe. Entered Monitor. Conn: {0}, Topic: {1}", connectionId, topic);
            try
            {
                //succeded in susbcription
                List<string> subscribedSelectors = _subscribedSelectorsForConnection.GetOrAdd(connectionId,
                    (s) => new List<string>());

                try
                {
                    await MessageBus.Subscribe(topic, filter, MessageBusCallback);
                    _counters.NumberOfSubscriptionsTotal.Increment();
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex, string.Format("MessageBus subscribe timed out. ConnectionId={0} Topic={1} Filter={2}", connectionId, topic, filter));
                    throw;
                }
                Trace.TraceVerbose("MessageBus subscribe finished. ConnectionId={0} Topic={1} Filter={2}", connectionId, topic, filter);
                if (subscribedSelectors.All(x => x != subscription.Selector))
                {
                    Trace.TraceVerbose("Subscribe. Adding group. Conn: {0}, Topic: {1}", connectionId, topic);

                    subscribedSelectors.Add(subscription.Selector);
// ReSharper disable CSharpWarnings::CS4014
                    Groups.Add(connectionId, subscription.Selector)
                        .ContinueWith(t => Trace.TraceVerbose("Subscribe. Group added. ConnectionId={0} Topic={1} Filter={2}", connectionId, topic, filter), 
                        TaskContinuationOptions.OnlyOnRanToCompletion)
                        .ContinueWith(t =>
// ReSharper restore CSharpWarnings::CS4014
                            Trace.TraceWarning("Groups add seems to have timed out. ConnectionId={0} Topic={1} Filter={2}", connectionId, topic, filter), 
                        TaskContinuationOptions.OnlyOnCanceled); 
                    // Sometimes groups.add times out on longpolling. Seems to be a SignalR bug. 
                    // This seems to happen while waiting for an Ack message from the client.
                    // We have obvserved no negative effects from this case, but it is good to log
                }

                Trace.TraceVerbose("Subscribed successfully. ConnectionId={0} Topic={1} Filter={2} ", connectionId, topic, filter);
            }
            catch (AggregateException agex)
            {
                agex.Handle(e =>
                {
                    Trace.TraceError(e);
                    return false;
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex);
                throw;
            }
            finally
            {
                Trace.TraceVerbose("Subscribe. Releasing monitor. ConnectionId={0} Topic={1} Filter={2}", connectionId, topic, filter);
                _subscriptionMutex.Release();
                Trace.TraceVerbose("Subscribe. Released monitor. ConnectionId={0} Topic={1} Filter={2}", connectionId, topic, filter);
            }
        }

        /// <summary>
        ///     Unsubscribe topic
        /// </summary>
        /// <param name="connectionId">The SignalR connectionID associated with this subscription.</param>
        /// <param name="topic">The topic, without filter, with nnt: qualifier, if applicable.</param>
        /// <returns></returns>
        public async Task Unsubscribe(string connectionId, string topic)
        {
            await Unsubscribe(connectionId, topic, null);
        }

        /// <summary>
        ///     Unsubscribe topic
        /// </summary>
        /// <param name="connectionId">The SignalR connectionID associated with this subscription.</param>
        /// <param name="topic">The topic, without filter, with nnt: qualifier, if applicable.</param>
        /// <param name="filter">The filter, in SQL-92 format.</param>
        /// <returns></returns>
        public async Task Unsubscribe(string connectionId, string topic, string filter)
        {

            await _subscriptionMutex.WaitAsync();
            Trace.TraceVerbose("Unsubscribe. Entered monitor. ConnectionId={0} Topic={1} Filter={2}", connectionId, topic, filter);
            try
            {
                List<string> subscribedSelectors;

                if (_subscribedSelectorsForConnection.TryGetValue(connectionId, out subscribedSelectors))
                {
                    var subscription = new SubscriptionIdentifier(topic, filter);


                    await MessageBus.Unsubscribe(topic, filter);
                    _counters.NumberOfSubscriptionsTotal.Decrement();
                    Trace.TraceVerbose("Unsubscribe. MessageBus unsubscribed. ConnectionId={0} Topic={1} Filter={2}", connectionId, topic, filter);
                    subscribedSelectors.Remove(subscription.Selector);

                    if (subscribedSelectors.Count == 0)
                    {
                        _subscribedSelectorsForConnection.TryRemove(connectionId, out subscribedSelectors);
                    }


                    await Groups.Remove(connectionId, subscription.Selector);
                    Trace.TraceVerbose("Unsubscribe. Group removed. ConnectionId={0} Topic={1} Filter={2}", connectionId, topic, filter);

                    // if it's the last topic, clean up resource
                }

            }
            catch (AggregateException agx)
            {
                agx.Handle((e) =>
                {
                    Trace.TraceError(e);
                    return false;
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex);
                throw;
            }
            finally
            {
                Trace.TraceVerbose("Unsubscribe. Releasing monitor. ConnectionId={0} Topic={1} Filter={2}", connectionId, topic, filter);
                _subscriptionMutex.Release();
                Trace.TraceVerbose("Unsubscribe. Released monitor. ConnectionId={0} Topic={1} Filter={2}", connectionId, topic, filter);
            }
        }

        /// <summary>
        ///     Unsubscribe all topics for connection
        /// </summary>
        /// <param name="connectionId">SignallR connection for which to unsubscribe</param>
        public async Task Unsubscribe(string connectionId)
        {
            await _subscriptionMutex.WaitAsync();
            Trace.TraceVerbose("Unsubscribe all. Entered Monitor. ConnectionId={0}", connectionId);

            try
            {
                List<string> selectorsToUnsubscribe;
                if (_subscribedSelectorsForConnection.TryRemove(connectionId, out selectorsToUnsubscribe))
                {
                    Trace.TraceVerbose("Unsubscribe all Removing all groups. ConnectionId={0}", connectionId);

                    foreach (var selector in selectorsToUnsubscribe)
                    {
                        var subscription = new SubscriptionIdentifier(selector);
                        await _messageBus.Unsubscribe(subscription.Topic, subscription.Filter);
                        _counters.NumberOfSubscriptionsTotal.Decrement();
                        Groups.Remove(connectionId, selector);
                    }
                    //await Task.WhenAll(
                    //    selectorsToUnsubscribe.Select((selector) => Groups.Remove(connectionId, selector)));
                    Trace.TraceVerbose("Unsubscribe all removed all groups. ConnectionId={0}", connectionId);
                }
            }
            catch (Exception exception)
            {
                //eat the exception as unsubscribe for connectionid need not be propogated as client is already disconnected.
                Trace.TraceError(exception);
            }
            finally
            {
                _subscriptionMutex.Release();
                Trace.TraceVerbose("Unsubscribe all released monitor. ConnectionId={0}", connectionId);
            }
        }

        /// <summary>
        /// Publish message to message bus
        /// </summary>
        /// <param name="topic">The topic, without filters</param>
        /// <param name="message">Message to publish. This will typically be a JSON string.</param>
        /// <returns></returns>
        public Task Publish(string topic, string message)
        {
            return Publish(topic, message, null);
        }

        /// <summary>
        /// Publish message to message bus
        /// </summary>
        /// <param name="topic">The topic, without filters</param>
        /// <param name="message">Message to publish. This will typically be a JSON string.</param>
        /// <param name="properties">The properties.</param>
        /// <returns></returns>
        public Task Publish(string topic, string message, IDictionary<string, object> properties)
        {
            if (TraceStrategy.ShouldTraceMessage(message))
            {
                Trace.TraceInformation("Receiving message ({0}): {1}", topic, message);
            }

            return MessageBus.Publish(topic, message, properties);
        }

        /// <summary>
        /// Disconnects all clients.
        /// </summary>
        /// <param name="retry">if set to <c>true</c> [retry].</param>
        public void DisconnectAll(bool retry = true)
        {
            //Disconnect all server with retry true as default
            Clients.All.serverOrderedDisconnect(retry);
        }

        #endregion

        #region Helpers


        private void MessageBusCallback(string topic, string filter, string value)
        {
            _counters.NumberDispatchedToSignalRTotal.Increment();
            _counters.NumberProcessedMessagesPerSecond.Increment();
            if (TraceStrategy.ShouldTraceMessage(value))
            {
                Trace.TraceInformation("Sending message ({0}): {1}", topic, value);
            }

            Dispatch(topic, filter, value);
        }

        /// <summary>
        /// Dispatch message to all clients who are interested in the topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="data">The data.</param>
        private void Dispatch(string topic, string filter, string data)
        {
            try
            {
                //find the tag
                //get connection id for the tag
                if (_unblockGroupSend)
                {
                    Task.Run(
                        () =>
                            Clients.Group(new SubscriptionIdentifier(topic, filter).Selector)
                                .onmessage(topic, filter, data));
                }
                else
                {
                    Clients.Group(new SubscriptionIdentifier(topic, filter).Selector).onmessage(topic, filter, data);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex,
                                 string.Format(
                                     "Unexpected error while dispatching message. Topic={0}, Data={1}",
                                     topic,
                                     data));
            }
        }

        // todo: we are not really using this the standard way right now because we are sending directly to a connection. 
        // subscription from the client side is only to assign a callback. filters are ignored (we don't need them). Is this okay, or do we
        // need to inject filters when subscribing to /session/* topics?
        private void DispatchConnection(string connectionId, string topic, string data)
        {
            try
            {
                Clients.Client(connectionId).onmessage(topic, "", data);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex,
                                 string.Format(
                                     "Unexpected error while dispatching message. Topic: {0}, Data: {1}",
                                     topic,
                                     data));
            }
        }

        private void DispatchConnections(IEnumerable<string> connectionIds, string topic, string data)
        {
            foreach (string connectionId in connectionIds)
            {
                DispatchConnection(connectionId, topic, data);
            }
        }

        #endregion
    }
}