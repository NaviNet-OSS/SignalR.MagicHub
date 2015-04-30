
namespace SignalR.MagicHub.Messaging
{
    /// <summary>
    /// Defines a set of functionality associated with receiving subscriptions for topics and filters, and associating them with callbacks
    /// </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// Register topic subscription
        /// </summary>
        /// <param name="topic">The topic being subscribed to. No filter strings are passed with this. Should have nnt: qualifier if applicable</param>
        /// <param name="filter">The filter in SQL-92 format</param>
        /// <param name="callback">The callback from the message bus to be executed when a message comes in.</param>
        void TopicSubscribed(string topic, string filter, MessageBusCallbackDelegate callback);

        /// <summary>
        /// Unregister topic subscription.
        /// </summary>
        /// <param name="topic">The topic being unsubscribed from. No filter strings are passed with this. Should have nnt: qualifier if applicable</param>
        /// <param name="filter">The filter in SQL-92 format</param>
        void TopicUnsubscribed(string topic, string filter);
    }
}