using System.Threading.Tasks;

namespace SignalR.MagicHub.Messaging.Filters
{
    /// <summary>
    /// Represents a factory for generating an expression tree of filters.
    /// </summary>
    public interface IFilterExpressionFactory
    {
        /// <summary>
        /// Asynchronously gets an expression tree representing the <paramref name="filterString"/>.
        /// </summary>
        /// <param name="filterString">The filter string.</param>
        /// <returns>A task containing the expression tree representing the passed <paramref name="filterString"/></returns>
        Task<IFilterExpression> GetExpressionAsync(string filterString);
    }
}
