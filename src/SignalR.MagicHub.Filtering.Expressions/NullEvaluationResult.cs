using System;

namespace SignalR.MagicHub.Filtering.Expressions
{
    public class NullEvaluationResult : IComparable
    {
        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj"/> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="obj"/>. Greater than zero This instance follows <paramref name="obj"/> in the sort order. 
        /// </returns>
        /// <param name="obj">An object to compare with this instance. </param><exception cref="T:System.ArgumentException"><paramref name="obj"/> is not the same type as this instance. </exception><filterpriority>2</filterpriority>
        /// <remarks>
        /// By definition, any object compares greater than (or follows) null, and two null references compare equal to each other.
        /// NullEvaluationResult is our null value for evaluation result.
        /// If it is compared to anything else (expect for another NullEvaluationResult or null), it is never equals to that object.
        /// </remarks>
        public int CompareTo(object obj)
        {
            if (obj == null || obj is NullEvaluationResult) 
                return 0;
            return int.MinValue;
        }

        private NullEvaluationResult()
        {
        }

        public static readonly NullEvaluationResult Value = new NullEvaluationResult();
    }
}