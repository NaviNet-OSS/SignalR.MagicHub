using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Newtonsoft.Json;
using SignalR.MagicHub.Infrastructure;

namespace SignalR.MagicHub.Messaging
{
    /// <summary>
    /// In process messagebus for simple publish and subscribe.
    /// </summary>
    /// <remarks>
    /// Uses SignalR Inprocess messagebus. Susbcribes to signalr inprocess using the key SignalR.MagicHub. 
    /// Topics and it's callback are stored internally. Publish publishes to SignalR.MagicHub with topic as the Source.
    /// Subscriber stores the callback in topics internally. 
    /// 
    /// When a message is received key would be SignalR.MagicHub and Source will contain the actual topic.
    /// </remarks>
    public class MessageBus : IMessageBus, ISubscriber
    {
        private const string SOURCE = AppConstants.SignalRMagicHub;
        private readonly Microsoft.AspNet.SignalR.Messaging.IMessageBus _messageBus;
        private readonly JsonSerializer _serializer;
        
        private readonly ConcurrentDictionary<string, MessageBusCallbackDelegate> _topics =
            new ConcurrentDictionary<string, MessageBusCallbackDelegate>();

        #region ctor

        public MessageBus()
            : this(GlobalHost.DependencyResolver.Resolve<Microsoft.AspNet.SignalR.Messaging.IMessageBus>(),
                   GlobalHost.DependencyResolver.Resolve<JsonSerializer>())
        {
        }

        public MessageBus(Microsoft.AspNet.SignalR.Messaging.IMessageBus messageBus, JsonSerializer serializer)
        {
            _messageBus = messageBus;
            _serializer = serializer;
            Identity = SOURCE;
            SubscribeToMessageBus();
        }

        #endregion

        #region IMessageBus

        /// <summary>
        /// Publish message to inprocess message bus.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Task<long> Publish(string key, string value)
        {
            return Publish(key, value, null);
        }

        /// <summary>
        /// Publish message to inprocess message bus.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Task<long> Publish(string key, string value, IDictionary<string, object> properties)
        {
            //switch the source and keys as EventKeys is copy at the time of initialization
            var message = new Message(key, SOURCE, _serializer.Stringify(value));
            _messageBus.Publish(message);

            return TaskAsyncHelper.FromResult<long>(1);
        }

        /// <summary>
        /// Subscribe to inprocess message bus. 
        /// </summary>
        /// <param name="topic</param>
        /// <param name="selector"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        /// <remarks>
        /// Stores the callback and uses the callback when SignalR calls the main callback.
        /// </remarks>
        public virtual Task Subscribe(string topic, MessageBusCallbackDelegate callback)
        {
            return Subscribe(topic, null, callback);
        }

        /// <summary>
        /// Subscribe to inprocess message bus. 
        /// </summary>
        /// <param name="topic</param>
        /// <param name="filter"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        /// <remarks>
        /// Stores the callback and uses the callback when SignalR calls the main callback.
        /// </remarks>
        public virtual Task Subscribe(string topic, string filter, MessageBusCallbackDelegate callback)
        {
            _topics.TryAdd(topic, callback);

            //return emtpy task
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Unsubscribe(string key)
        {
            return Unsubscribe(key, null);
        }

        public virtual Task Unsubscribe(string key, string filter)
        {
            MessageBusCallbackDelegate temp;
            _topics.TryRemove(key, out temp);

            return TaskAsyncHelper.Empty;
        }

        #endregion

        #region ISubscriber

        public IList<string> EventKeys
        {
            get { return new List<string> {SOURCE}; }
        }

        public Action<TextWriter> WriteCursor { get; set; }
        public string Identity { get; private set; }
        public Subscription Subscription { get; set; }

#pragma warning disable 067
        public event Action<ISubscriber, string> EventKeyAdded;
        public event Action<ISubscriber, string> EventKeyRemoved;
#pragma warning restore 067
        #endregion

        #region Helpers

        private void SubscribeToMessageBus()
        {
            _messageBus.Subscribe(this, null, MessageResultCallBack, 10, null);
        }

        /// <summary>
        /// Uses topics callback when matched on the key.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private Task<bool> MessageResultCallBack(MessageResult result, object state)
        {
            //return emtpy task
            result.Messages.Enumerate(message => message.Key.Equals(SOURCE), (s, message) =>
                {
                    MessageBusCallbackDelegate callback;
                    if (_topics.TryGetValue(message.Source, out callback))
                    {
                        callback(message.Source, null, _serializer.Parse<string>(message.Value, message.Encoding));
                    }
                }, state);

            return TaskAsyncHelper.True;
        }

        #endregion
    }
}
