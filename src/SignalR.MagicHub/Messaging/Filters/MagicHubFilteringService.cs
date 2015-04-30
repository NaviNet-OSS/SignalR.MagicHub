using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tracing;

namespace SignalR.MagicHub.Messaging.Filters
{
    /// <summary>
    /// MagicHub filtering service implementation of <see cref="IMagicHubFilteringService"/>.
    /// </summary>
    public class MagicHubFilteringService : IMagicHubFilteringService
    {
        private readonly IFilterExpressionFactory _filterExpressionFactory;
        private readonly TraceSource _trace;

        /// <summary>
        /// Initializes a new instance of the <see cref="MagicHubFilteringService"/> class.
        /// </summary>
        /// <param name="filterExpressionFactory">The filter expression factory.</param>
        /// <param name="traceManager">The trace manager.</param>
        public MagicHubFilteringService(IFilterExpressionFactory filterExpressionFactory, ITraceManager traceManager)
        {
            _filterExpressionFactory = filterExpressionFactory;
            _trace = traceManager[AppConstants.SignalRMagicHub];
        }

        /// <summary>
        /// Filters subscriptions that match Subscription indentifier filter based on message context
        /// </summary>
        /// <param name="context">Message context</param>
        /// <param name="subscriptions">List of subscriptions to filter</param>
        /// <returns></returns>
        public async Task<IEnumerable<KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>>> Filter(IReadOnlyDictionary<string, object> context,
            IEnumerable<KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>> subscriptions)
        {
            var matched = await WhereAsync(subscriptions, p => Task.Run(() => SelectDelegate(p, context)));

            return matched;
        }

        private Task<IFilterExpression> GetFilterExpression(SubscriptionIdentifier subscriptionIdentifier)
        {
            return _filterExpressionFactory.GetExpressionAsync(subscriptionIdentifier.Selector);
        }

        private async Task<bool> SelectDelegate(
            KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate> pair, IReadOnlyDictionary<string, object> context)
        {
            try
            {
                var filter = await GetFilterExpression(pair.Key);
                return (bool) await filter.EvaluateAsync(context);
            }
            catch (Exception ex)
            {
                _trace.TraceError(ex, "Error occurred while attempting to evaluate subscription: " + pair.Key.Selector);
            }

            return false;
        }

        private static async Task<IEnumerable<T>> WhereAsync<T>(IEnumerable<T> items, Func<T, Task<bool>> predicate)
        {
            var itemTaskList = items.Select(item => new { Item = item, PredTask = predicate.Invoke(item) }).ToList();

            await Task.WhenAll(itemTaskList.Select(x => x.PredTask));
            return itemTaskList.Where(x => x.PredTask.Result).Select(x => x.Item);
        }
    }

}