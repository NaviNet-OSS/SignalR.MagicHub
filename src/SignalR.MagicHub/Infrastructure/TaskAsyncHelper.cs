using System;
using System.Threading.Tasks;

namespace SignalR.MagicHub.Infrastructure
{
    public static class TaskAsyncHelper
    {
        private static readonly Task _emptyTask = FromResult<object>(null);
        private static readonly Task<bool> _trueTask = FromResult(true);

        public static Task Empty
        {
            get { return _emptyTask; }
        }

        public static Task<bool> True
        {
            get { return _trueTask; }
        }

        public static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }

        public static Task FromError(Exception e)
        {
            return FromError<object>(e);
        }

        public static Task<T> FromError<T>(Exception e)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetUnwrappedException<T>(e);
            return tcs.Task;
        }

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