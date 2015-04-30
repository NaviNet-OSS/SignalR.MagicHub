using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR.MagicHub.Messaging.Filters;

namespace SignalR.MagicHub.Filtering.Expressions
{
    /// <summary>
    /// Logical expression class
    /// </summary>
    /// <example>OfficeNID > 10</example>
    /// <remarks>This expression consists of left and right expression and operation</remarks>
    public class LogicalExpression : IFilterExpression
    {
        public IFilterExpression LeftExpression { get; set; }
        public IFilterExpression RightExpression { get; set; }
        public FilterOperator Operation { get; set; }

        /// <summary>
        /// Logical expression constructor
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="operation"></param>
        public LogicalExpression(IFilterExpression left, IFilterExpression right, FilterOperator operation)
        {
            LeftExpression = left;
            RightExpression = right;
            Operation = operation;
        }

        /// <summary>
        /// Evaluates filter expression agains message context
        /// </summary>
        /// <param name="messageContext">Key value pair store with message context</param>
        /// <returns>Evaluation result task</returns>
        public async Task<IComparable> EvaluateAsync(IReadOnlyDictionary<string, object> messageContext)
        {
            // When no actual filter was created as expression we think that evaluation passed successfully, because there were nothing to compare to 
            if (Operation == 0 && (LeftExpression == null || RightExpression == null)) return true;

            var left = LeftExpression.EvaluateAsync(messageContext);
            var right = RightExpression.EvaluateAsync(messageContext);
              
            await Task.WhenAll(left, right);
            
            if(object.ReferenceEquals(left.Result, NullEvaluationResult.Value))
                return false;

            //actual operation between left and right
            switch (Operation)
            {
                case FilterOperator.And:
                {
                    return (bool)left.Result && (bool)right.Result; 
                }                    
                case FilterOperator.Or:
                {
                    return (bool)left.Result || (bool)right.Result;
                }
                case FilterOperator.EqualTo:
                {
                    return left.Result.Equals(right.Result);
                }
                case FilterOperator.NotEqualTo:
                {
                    return !left.Result.Equals(right.Result);
                }
                case FilterOperator.GreaterThan:
                {
                    return left.Result.CompareTo(right.Result) == 1;
                }
                case FilterOperator.GreaterThanOrEqualTo:
                {
                    return left.Result.CompareTo(right.Result) >= 0;   
                }
                case FilterOperator.LessThanOrEqualTo:
                {
                    return left.Result.CompareTo(right.Result) <= 0;  
                }
                case FilterOperator.LessThan:
                {
                    return left.Result.CompareTo(right.Result) == -1;  
                }
            }
            return false;
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
            var otherExpression = obj as LogicalExpression;
            return otherExpression != null
                   && Operation.Equals(otherExpression.Operation)
                   && SafeEquals(LeftExpression, otherExpression.LeftExpression)
                   && SafeEquals(RightExpression, otherExpression.RightExpression);
        }

        private static bool SafeEquals(IFilterExpression first, IFilterExpression second)
        {
            return (first == null && second == null) 
                || (first != null && first.Equals(second));
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("({0} {1} {2})", LeftExpression, Operation, RightExpression);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}