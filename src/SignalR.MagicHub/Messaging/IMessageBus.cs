using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.MagicHub.Messaging
{
    public delegate void MessageBusCallbackDelegate(string topic, string filter, string message);

    public interface IMessageBus
    {
        /// <summary>
        /// Publish message to message bus.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<long> Publish(string key, string value);

        /// <summary>
        /// Publish message to message bus.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        Task<long> Publish(string key, string value, IDictionary<string, object> properties);

        /// <summary>
        /// Subscribe message bus on key
        /// </summary>
        /// <param name="topic</param>
        /// <param name="callback"></param>
        /// <returns></returns>
        Task Subscribe(string topic, MessageBusCallbackDelegate callback);
        
        /// <summary>
        /// Subscribe message bus on key
        /// </summary>
        /// <param name="topic</param>
        /// <param name="callback"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task Subscribe(string topic, string filter, MessageBusCallbackDelegate callback);

        /// <summary>
        /// Unsubscribe from messagebus.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task Unsubscribe(string key);

        /// <summary>
        /// Unsubscribe from messagebus.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task Unsubscribe(string key, string filter);
    }
}