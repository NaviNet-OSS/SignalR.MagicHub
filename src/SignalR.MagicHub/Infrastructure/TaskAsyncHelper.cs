using System;
using System.Threading.Tasks;

namespace SignalR.MagicHub.Infrastructure
{
    /// <summary>
    /// Provides convenience functionality for tasks
    /// </summary>
    public static class TaskAsyncHelper
    {
        private static readonly Task _emptyTask = Task.FromResult<object>(null);
        private static readonly Task<bool> _trueTask = Task.FromResult(true);

        /// <summary>
        /// Gets a value which represents an empty completed task.
        /// </summary>
        public static Task Empty
        {
            get { return _emptyTask; }
        }

        /// <summary>
        /// Gets a value which represents a completed task with a result of True
        /// </summary>
        public static Task<bool> True
        {
            get { return _trueTask; }
        }


        /// <summary>
        /// Gets a value represents an error task
        /// </summary>
        public static Task FromError(Exception e)
        {
            return FromError<object>(e);
        }

        /// <summary>
        /// Gets a value represents an error task
        /// </summary>
        public static Task<T> FromError<T>(Exception e)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetUnwrappedException<T>(e);
            return tcs.Task;
        }


        /// <summary>
        /// Sets an exception on a task completion source.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tcs">The TCS.</param>
        /// <param name="e">The e.</param>
        public static void SetUnwrappedException<T>(this TaskCompletionSource<T> tcs, Exception e)
        {
            var aggregateException = e as AggregateException;
            if (aggregateException != null)
            {
                tcs.SetException(aggregateException.InnerExceptions);
            }
            else
            {
                tcs.SetException(e);
            }
        }
    }
}