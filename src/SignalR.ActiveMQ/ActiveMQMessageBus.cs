using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using SignalR.MagicHub.Infrastructure;
using SignalR.MagicHub.Messaging;

namespace SignalR.ActiveMQ
{
    /// <summary>
    ///     Implementation of IMessagebus for communicating with ActiveMQ
    /// </summary>
    public class ActiveMqMessageBus : IMessageBus, IDisposable
    {
        #region Private Members

        private readonly string _connectionUri;

        private readonly IMessageReceiver _receiver;
        private IConnection _connection;

        private IConnectionFactory _connectionFactory;

        private bool _isDisposed;

        private ISession _session;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ActiveMqMessageBus" /> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="connectionUri">The connection URI.</param>
        /// <param name="receiver">The receiver which handles subscriptions and message receiving</param>
        public ActiveMqMessageBus(ISession session, IConnectionFactory connectionFactory, string connectionUri,
                                  IMessageReceiver receiver)
        {
            _session = session;
            _connectionFactory = connectionFactory;
            _connectionUri = connectionUri;
            _receiver = receiver;
            OutboundTopic = "AMQ.SignalRMagicHub.Notifications.Json.Ext.T"; // outbound == inbound by default
        }

        public ActiveMqMessageBus(string connectionUri, IMessageReceiver receiver)
            : this(null, null, connectionUri, receiver)
        {
        }

        #endregion

        #region IDisposable members

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _session.Dispose();

                if (_connection != null)
                {
                    _connection.Dispose();
                }
                _isDisposed = true;
            }
        }

        #endregion

        #region IMessageBus Members

        public Task<long> Publish(string key, string value)
        {
            return Publish(key, value, null);
        }

        public Task<long> Publish(string key, string value, IDictionary<string, object> properties)
        {
            try
            {
                Send(key, value, properties);
            }
            catch (IOException)
            {
                //Try again
                ResetConnection();
                Send(key, value, properties);
            }
            //return Task<long>.Factory.StartNew(() =>
            return TaskAsyncHelper.FromResult<long>(0);
        }

        public Task Subscribe(string topic, MessageBusCallbackDelegate callback)
        {
            return Subscribe(topic, null, callback);
        }

        public Task Subscribe(string topic, string filter, MessageBusCallbackDelegate callback)
        {
            try
            {
                _receiver.TopicSubscribed(topic, filter, callback);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError(ex);
            }


            return TaskAsyncHelper.Empty;
        }

        public Task Unsubscribe(string key)
        {
            return Unsubscribe(key, null);
        }

        public Task Unsubscribe(string topic, string filter)
        {
            try
            {
                _receiver.TopicUnsubscribed(topic, filter);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError(ex);
            }

            return TaskAsyncHelper.Empty;
        }

        #endregion

        #region Private Methods

        private ISession GetSession()
        {
            if (_session != null)
            {
                return _session;
            }

            if (_connectionFactory == null)
            {
                _connectionFactory = new ConnectionFactory(_connectionUri);
            }
            if (_connection == null)
            {
                _connection = _connectionFactory.CreateConnection();
                //TODO set client id
                _connection.Start();
            }
            if (!_connection.IsStarted)
            {
                try
                {
                    _connection.Start();
                }
                catch (Exception)
                {
                    //try again
                    _connection = _connectionFactory.CreateConnection();
                    //TODO set client id
                    _connection.Start();
                }
            }

            _session = _connection.CreateSession();
            return _session;
        }

        private void ResetConnection()
        {
            //TODO: DurableClient set client id
            if (_connection != null)
            {
                _connection.Dispose();
            }
            if (_session != null)
            {
                _session.Dispose();
            }
            _connection = null;
            _session = null;
        }

        private void Send(string key, string value, IEnumerable<KeyValuePair<string, object>> properties = null)
        {
            ISession session = GetSession();

            //TODO: Filter by the key; 
            ITopic destination = session.GetTopic(OutboundTopic);

            //IDestination destination = session.GetTopic(key);
            using (IMessageProducer producer = session.CreateProducer(destination))
            {
                ITextMessage message = producer.CreateTextMessage(value);

                message.Properties["Topic"] = key;
                if (properties != null)
                {
                    foreach (var pair in properties)
                    {
                        message.Properties[pair.Key] = pair.Value;
                    }
                }

                producer.Send(message);
            }
        }

        #endregion

        public string OutboundTopic { get; set; }
    }
}