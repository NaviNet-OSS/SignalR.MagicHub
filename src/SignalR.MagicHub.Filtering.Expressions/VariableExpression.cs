using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR.MagicHub.Messaging.Filters;

namespace SignalR.MagicHub.Filtering.Expressions
{
    /// <summary>
    /// Variable expression class
    /// </summary>
    public class VariableExpression : IFilterExpression
    {
        private string FieldName { get; set; }
        
        /// <summary>
        /// Variable expression constructor
        /// </summary>
        /// <param name="fieldName"></param>
        public VariableExpression(string fieldName)
        {
            FieldName = fieldName;
        }

        /// <summary>
        /// Evaluates message context against filtering expression
        /// </summary>
        /// <param name="messageContext">Key value pair store with message context</param>
        /// <returns>Evaluation result task</returns>
        public Task<IComparable> EvaluateAsync(IReadOnlyDictionary<string, object> messageContext)
        {
            object actual;
            messageContext.TryGetValue(FieldName, out actual);
            return Task.FromResult(actual as IComparable ?? NullEvaluationResult.Value);
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
            var otherExpression = obj as VariableExpression;
            return otherExpression != null && FieldName.Equals(otherExpression.FieldName);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return FieldName.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return FieldName;
        }
    }
}
