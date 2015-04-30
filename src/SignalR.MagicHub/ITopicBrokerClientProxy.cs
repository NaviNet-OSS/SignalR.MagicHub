
using SignalR.MagicHub.Infrastructure;

namespace SignalR.MagicHub
{
    /// <summary>
    /// Interface for a dynamic proxy for a SignalR client for MagicHub and Topicbroker
    /// </summary>
    /// <see cref="TopicBroker"/>
    /// <see cref="MessageHub"/>
    public interface ITopicBrokerClientProxy
    {
        /// <summary>
        /// Invokes the "onmessage" function on a client
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="data">The data of the message</param>
        void onmessage(string topic, string filter, string data);

        /// <summary>
        /// Invokes "serverOrderedDisconnect" on a client, informing it that the server requests it be disconnected
        /// </summary>
        /// <param name="shouldRetry">if set to <c>true</c> the client should try to reconnect.</param>
        void serverOrderedDisconnect(bool shouldRetry);
    }
}