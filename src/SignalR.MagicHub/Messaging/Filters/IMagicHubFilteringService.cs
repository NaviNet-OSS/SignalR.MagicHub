using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.MagicHub.Messaging.Filters
{
    /// <summary>
    /// Represents a service which can filter a message context against a list of active filters.
    /// </summary>
    public interface IMagicHubFilteringService
    {
        /// <summary>
        /// Filters the specified <paramref name="context"/> against a set of subscriptions, returning the matching set.
        /// </summary>
        /// <param name="context">The context, containing properties that the subscriptions.</param>
        /// <param name="subscriptions">A collection of subscriptions and their callbacks</param>
        /// <returns>A filtered collection of subscriptions and callbacks which matches the <paramref name="context"/></returns>
        Task<IEnumerable<KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>>> Filter(IReadOnlyDictionary<string, object> context,
            IEnumerable<KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>> subscriptions);
    }
}