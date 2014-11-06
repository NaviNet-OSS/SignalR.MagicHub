using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.MagicHub.Infrastructure
{
    public interface IMessageHub
    {
        /// <summary>
        ///     Subscribe topic to message bus
        /// </summary>
        /// <param name="connectionId">The SignalR connectionID associated with this subscription.</param>
        /// <param name="topic">The topic, without filter, with nnt: qualifier, if applicable.</param>
        /// <returns></returns>
        Task Subscribe(string connectionId, string topic);
        /// <summary>
        ///     Subscribe topic to message bus
        /// </summary>
        /// <param name="connectionId">The SignalR connectionID associated with this subscription.</param>
        /// <param name="topic">The topic, without filter, with nnt: qualifier, if applicable.</param>
        /// <param name="filter">The filter, in SQL-92 format.</param>
        /// <returns></returns>
        Task Subscribe(string connectionId, string topic, string filter);
        /// <summary>
        ///     Unsubscribe topic
        /// </summary>
        /// <param name="connectionId">The SignalR connectionID associated with this subscription.</param>
        /// <param name="topic">The topic, without filter, with nnt: qualifier, if applicable.</param>
        /// <returns></returns>
        Task Unsubscribe(string connectionId, string topic);
        /// <summary>
        ///     Unsubscribe topic
        /// </summary>
        /// <param name="connectionId">The SignalR connectionID associated with this subscription.</param>
        /// <param name="topic">The topic, without filter, with nnt: qualifier, if applicable.</param>
        /// <param name="filter">The filter, in SQL-92 format.</param>
        /// <returns></returns>
        Task Unsubscribe(string connectionId, string topic, string filter);

        /// <summary>
        ///     Unsubscribe all topics for connection
        /// </summary>
        /// <param name="connectionId">SignallR connection for which to unsubscribe</param>
        void Unsubscribe(string connectionId);

        /// <summary>
        ///     Publish message to message bus
        /// </summary>
        /// <param name="topic">The topic, without filters</param>
        /// <param name="message">Message to publish. This will typically be a JSON string.</param>
        /// <returns></returns>
        Task Publish(string topic, string message);

        /// <summary>
        /// Publish message to message bus
        /// </summary>
        /// <param name="topic">The topic, without filters</param>
        /// <param name="message">Message to publish. This will typically be a JSON string.</param>
        /// <param name="properties">The properties.</param>
        /// <returns></returns>
        Task Publish(string topic, string message, IDictionary<string, object> properties);
    }

}
