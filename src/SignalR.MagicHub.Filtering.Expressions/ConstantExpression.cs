using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR.MagicHub.Messaging.Filters;

namespace SignalR.MagicHub.Filtering.Expressions
{
    /// <summary>
    /// Constant expression class 
    /// </summary>
    public class ConstantExpression : IFilterExpression
    {
        private IComparable ObjectValue { get; set; }
        
        /// <summary>
        /// Constant expression constructor
        /// </summary>
        /// <param name="value"></param>
        public ConstantExpression(IComparable value)
        {
            ObjectValue = value;
        }

        /// <summary>
        /// Evaluates message context against filtering expression
        /// </summary>
        /// <param name="messageContext">Key value pair store with message context</param>
        /// <returns>Evaluation result task</returns>
        public Task<IComparable> EvaluateAsync(IReadOnlyDictionary<string, object> messageContext)
        {
            return Task.FromResult(ObjectValue);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            var otherExpression = obj as ConstantExpression;
            return otherExpression != null && otherExpression.ObjectValue.Equals(ObjectValue);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return ObjectValue.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            if (ObjectValue is string)
            {
                return string.Format("'{0}'", ObjectValue);
            }
            return ObjectValue.ToString();
        }
    }
}
