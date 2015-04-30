using System;
using System.Runtime.Caching;
using System.Threading.Tasks;
using LazyCache;
using SignalR.MagicHub.Messaging.Filters;

namespace SignalR.MagicHub.Filtering.Parsing
{
    public class CachingFilterExpressionFactory : IFilterExpressionFactory
    {

        private readonly IFilterExpressionFactory _innerFactory;
        private readonly CachingService _cache;

        public CachingFilterExpressionFactory(IFilterExpressionFactory innerFactory, ObjectCache objectCache)
        {
            _cache = new CachingService(objectCache);
            _innerFactory = innerFactory;
            DefaultPolicy = new CacheItemPolicy {SlidingExpiration = TimeSpan.FromHours(2)};
        }
        

        public CacheItemPolicy DefaultPolicy { get; set; }
        public Task<IFilterExpression> GetExpressionAsync(string filterString)
        {
            return _cache.GetOrAdd(filterString, () => _innerFactory.GetExpressionAsync(filterString),DefaultPolicy);
        }
    }
}
