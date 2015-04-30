using System.Threading.Tasks;

namespace SignalR.MagicHub.Messaging
{
    /// <summary>
    /// Message dispatcher interface
    /// </summary>
    public interface IMessageDispatcher
    {
        /// <summary>
        /// Informs the message dispatcher of a new subscription
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="callback">The callback.</param>
        void Subscribe(SubscriptionIdentifier subscription, MessageBusCallbackDelegate callback);
        /// <summary>
        /// Informs the message dispatcher of a closed subscription
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        void Unsubscribe(SubscriptionIdentifier subscription);

        /// <summary>
        /// Dispatches the message to all matching subscriptions.
        /// </summary>
        /// <param name="msg">The message</param>
        /// <returns>Task which completes when all subscriptions have been dispatched</returns>
        Task DispatchMessage(IMagicHubMessage msg);
    }
}