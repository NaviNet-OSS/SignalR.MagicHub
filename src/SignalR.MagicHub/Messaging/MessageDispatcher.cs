using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tracing;
using SignalR.MagicHub.Messaging.Filters;

namespace SignalR.MagicHub.Messaging
{
    /// <summary>
    /// Tracks topic subscriptions and invokes callbacks for callbacks matching messages
    /// </summary>
    public class MessageDispatcher : IMessageDispatcher
    {
        private readonly IMagicHubFilteringService _filteringService;
        private readonly ITraceManager _traceManager;

        private readonly ConcurrentDictionary<SubscriptionIdentifier, MessageBusCallbackDelegate> _callbacks =
            new ConcurrentDictionary<SubscriptionIdentifier, MessageBusCallbackDelegate>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageDispatcher"/> class.
        /// </summary>
        /// <param name="filteringService">The filtering service.</param>
        /// <param name="traceManager">The trace manager.</param>
        public MessageDispatcher(IMagicHubFilteringService filteringService, ITraceManager traceManager)
        {
            _filteringService = filteringService;
            _traceManager = traceManager;
        }

        /// <summary>
        /// Gets the trace source
        /// </summary>
        /// <value>
        /// The trace.
        /// </value>
        protected TraceSource Trace
        {
            get { return _traceManager[AppConstants.SignalRMagicHub]; }
        }

        /// <summary>
        /// Informs the message dispatcher of a new subscription
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="callback">The callback.</param>
        public void Subscribe(SubscriptionIdentifier subscription, MessageBusCallbackDelegate callback)
        {
            _callbacks[subscription] = callback;
        }

        /// <summary>
        /// Informs the message dispatcher of a closed subscription
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        public void Unsubscribe(SubscriptionIdentifier subscription)
        {
            MessageBusCallbackDelegate temp;
            _callbacks.TryRemove(subscription, out temp);
        }

        /// <summary>
        /// Dispatches the message to all matching subscriptions.
        /// </summary>
        /// <param name="msg">The message</param>
        /// <returns>
        /// Task which completes when all subscriptions have been dispatched
        /// </returns>
        public async Task DispatchMessage(IMagicHubMessage msg)
        {
            try
            {
                var callbacks = await _filteringService.Filter(msg.Context, _callbacks);
                //Trace.TraceVerbose("Invoking {0} callbacks.", callbacks.Count());
                var tasks = callbacks.Select(item => Task.Run(() => item.Value(item.Key.Topic, item.Key.Filter, msg.Message)));
                //callbacks.AsParallel().ForEach(item => item.Value(item.Key.Topic, item.Key.Filter, msg.Message));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex, "Error dispatching message");
                throw;
            }

        }


    }
}