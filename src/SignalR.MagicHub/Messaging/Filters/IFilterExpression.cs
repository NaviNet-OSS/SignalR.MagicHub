using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.MagicHub.Messaging.Filters
{
    /// <summary>
    /// Default interface for filter expressions
    /// </summary>
    public interface IFilterExpression
    {
        /// <summary>
        /// Evaluates message context against filtering expression
        /// </summary>
        /// <param name="messageContext">Key value pair store with message context</param>
        /// <returns>Evaluation result task</returns>
        Task<IComparable> EvaluateAsync(IReadOnlyDictionary<string, object> messageContext);
    }
}
