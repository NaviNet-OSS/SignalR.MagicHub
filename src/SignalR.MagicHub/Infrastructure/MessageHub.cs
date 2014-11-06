using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;
using SignalR.MagicHub.MessageContracts;
using SignalR.MagicHub.Messaging;
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

        private readonly ConcurrentDictionary<string, uint> _subscriptionsToSelector =
            new ConcurrentDictionary<string, uint>();

        private readonly ITraceManager _traceManager;

        private readonly TraceStrategy _traceStrategy = new TraceStrategy();
        private readonly ISessionMappings _sessionMappings;

        #region ctor

        public MessageHub(IDependencyResolver resolver, IConnectionManager connectionManager)
            : this(
                resolver.Resolve<IMessageBus>() ?? new MessageBus(), 
                connectionManager,
                resolver.Resolve<ITraceManager>(), 
                resolver.Resolve<ISessionValidatorService>(),
                resolver.Resolve<ISessionMappings>())
        {
        }

        public MessageHub(IMessageBus messageBus, IConnectionManager connectionManager, ITraceManager traceManager, ISessionValidatorService sessionValidatorService, ISessionMappings sessionMappings)
        {
            var context = connectionManager.GetHubContext<TopicBroker>();
            _messageBus = messageBus;
            Clients = context.Clients;
            Groups = context.Groups;
            _traceManager = traceManager;
            _sessionMappings = sessionMappings;

            if (sessionValidatorService != null)
            {
                sessionValidatorService.SessionExpired += SessionValidatorServiceOnSessionExpired;
                sessionValidatorService.SessionKeptAlive += SessionValidatorServiceOnSessionKeptAlive;
                sessionValidatorService.SessionExpiring += SessionValidatorServiceSessionExpiring;
            }
        }

        private void SessionValidatorServiceSessionExpiring(object sender, SessionStateEventArgs e)
        {
            var msg = new SessionExpiringMessage {ExpiresAt = e.ExpiresAt};
            DispatchConnections(_sessionMappings.GetConnectionIds(e.SessionState.SessionKey), TopicBroker.TopicSessionExpiring, msg.ToString());
            msg.User = e.SessionState;
            // todo: AMQ notify
            MessageBus.Publish(TopicBroker.TopicSessionExpiring, msg.ToString());
        }

        private void SessionValidatorServiceOnSessionKeptAlive(object sender, SessionStateEventArgs e)
        {
            var msg = new SessionKeptAliveMessage { Status = e.OperationSuccess ? "succeeded" : "failed"};
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

        protected IHubConnectionContext Clients { get; private set; }

        protected IGroupManager Groups { get; private set; }

        protected IMessageBus MessageBus
        {
            get { return _messageBus; }
        }

        protected TraceSource Trace
        {
            get { return _traceManager[AppConstants.SignalRMagicHub]; }
        }

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
        public Task Subscribe(string connectionId, string topic)
        {
            return Subscribe(connectionId, topic, null);
        }

        /// <summary>
        ///     Subscribe topic to message bus
        /// </summary>
        /// <param name="connectionId">The SignalR connectionID associated with this subscription.</param>
        /// <param name="topic">The topic, without filter, with nnt: qualifier, if applicable.</param>
        /// <param name="filter">The filter, in SQL-92 format.</param>
        /// <returns></returns>
        public Task Subscribe(string connectionId, string topic, string filter)
        {
            Trace.TraceVerbose("Subscribe called: " + string.Join("; ", topic, filter));
            var subscription = new SubscriptionIdentifier(topic, filter);

            Task task =
                IncrementSelectorSubscriptions(subscription, MessageBusCallback).ContinueWith(t =>
                    {
                        if (t.Exception == null)
                        {
                            //succeded in susbcription
                            lock (_subscribedSelectorsForConnection)
                            {
                                List<string> subscribedSelectors;
                                if (
                                    !_subscribedSelectorsForConnection.TryGetValue(connectionId, out subscribedSelectors))
                                {
                                    subscribedSelectors = new List<string>();
                                    _subscribedSelectorsForConnection[connectionId] = subscribedSelectors;
                                }
                                if (subscribedSelectors.All(x => x != subscription.Selector))
                                {
                                    subscribedSelectors.Add(subscription.Selector);
                                    Groups.Add(connectionId, subscription.Selector);
                                }
                                
                                Trace.TraceVerbose("Subscribed: " + string.Join("; ", topic, filter));
                            }
                        }
                        else
                        {
                            t.Exception.Handle(e =>
                                {
                                    Trace.TraceError(e);
                                    return false;
                                });
                        }
                    });

            return task;
        }

        /// <summary>
        ///     Unsubscribe topic
        /// </summary>
        /// <param name="connectionId">The SignalR connectionID associated with this subscription.</param>
        /// <param name="topic">The topic, without filter, with nnt: qualifier, if applicable.</param>
        /// <returns></returns>
        public Task Unsubscribe(string connectionId, string topic)
        {
            return Unsubscribe(connectionId, topic, null);
        }

        /// <summary>
        ///     Unsubscribe topic
        /// </summary>
        /// <param name="connectionId">The SignalR connectionID associated with this subscription.</param>
        /// <param name="topic">The topic, without filter, with nnt: qualifier, if applicable.</param>
        /// <param name="filter">The filter, in SQL-92 format.</param>
        /// <returns></returns>
        public Task Unsubscribe(string connectionId, string topic, string filter)
        {
            var messageBusUnsubscribeTasks = new List<Task>();
            lock (_subscribedSelectorsForConnection)
            {
                List<string> subscribedSelectors;
                if (_subscribedSelectorsForConnection.TryGetValue(connectionId, out subscribedSelectors))
                {
                    var subscription = new SubscriptionIdentifier(topic, filter);
                    if (subscribedSelectors.Remove(subscription.Selector))
                    {
                        messageBusUnsubscribeTasks.Add(DecrementSelectorSubscriptions(subscription));
                    }
                    Groups.Remove(connectionId, subscription.Selector);
                    // if it's the last topic, clean up resource
                    if (subscribedSelectors.Count == 0)
                    {
                        _subscribedSelectorsForConnection.TryRemove(connectionId, out subscribedSelectors);
                    }
                }
            }
            return Task.WhenAll(messageBusUnsubscribeTasks).ContinueWith((t) =>
                {
                    if (t.Exception != null)
                    {
                        t.Exception.Handle((e) =>
                            {
                                Trace.TraceError(e);
                                return false;
                            });
                    }
                });
        }

        /// <summary>
        ///     Unsubscribe all topics for connection
        /// </summary>
        /// <param name="connectionId">SignallR connection for which to unsubscribe</param>
        public void Unsubscribe(string connectionId)
        {
            try
            {
                lock (_subscribedSelectorsForConnection)
                {
                    List<string> selectorsToUnsubscribe;
                    if (_subscribedSelectorsForConnection.TryRemove(connectionId, out selectorsToUnsubscribe))
                    {
                        foreach (string selector in selectorsToUnsubscribe)
                        {
                            Groups.Remove(connectionId, selector);
                            DecrementSelectorSubscriptions(new SubscriptionIdentifier(selector));
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                //eat the exception as unsubscribe for connectionid need not be propogated as client is already disconnected.
                Trace.TraceError(exception);
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

        private Task IncrementSelectorSubscriptions(SubscriptionIdentifier subscription,
                                                    MessageBusCallbackDelegate callback)
        {
            uint newValue = _subscriptionsToSelector.AddOrUpdate(subscription.Selector, 1, (t, value) => value + 1);
            if (newValue == 1)
            {
                Task task = MessageBus.Subscribe(subscription.Topic, subscription.Filter, callback)
                                      .ContinueWith(t =>
                                          {
                                              //failed in susbcription
                                              if (t.Exception != null)
                                              {
                                                  t.Exception.Handle(e =>
                                                      {
                                                          _subscriptionsToSelector.AddOrUpdate(subscription.Selector, 0,
                                                                                               (tpk, value) => value - 1);
                                                          return false;
                                                      });
                                              }
                                          }, TaskContinuationOptions.OnlyOnFaulted);
                return task;
            }
            return TaskAsyncHelper.Empty;
        }

        private Task DecrementSelectorSubscriptions(SubscriptionIdentifier subscription)
        {
            uint newValue = _subscriptionsToSelector.AddOrUpdate(subscription.Selector, 0, (t, value) => value - 1);
            if (newValue == 0)
            {
                try
                {
                    return MessageBus.Unsubscribe(subscription.Topic, subscription.Filter);
                }
                catch (Exception ex)
                {
                    return TaskAsyncHelper.FromError(ex);
                }
            }
            return TaskAsyncHelper.Empty;
        }

        private void MessageBusCallback(string topic, string filter, string value)
        {
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
                Clients.Group(new SubscriptionIdentifier(topic, filter).Selector).onmessage(topic, filter, data);
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