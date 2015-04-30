using System;
using System.Diagnostics;

namespace SignalR.MagicHub
{
    /// <summary>
    /// Extensions class for trace source
    /// </summary>
    public static class TraceSourceExtensions
    {
        /// <summary>
        /// Traces error.
        /// </summary>
        /// <param name="trace">The trace.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="optionalMessage">The optional message.</param>
        /// <returns></returns>
        public static TraceSource TraceError(this TraceSource trace, Exception ex, string optionalMessage = null)
        {
            optionalMessage = optionalMessage == null
                                  ? string.Empty
                                  : string.Format("ErrorOccurred=\"{0}\" ", optionalMessage);
            
            trace.TraceError("{3} ExceptionType=\"{0}\" ExceptionMessage=\"{1}\" StackTrace={2}", ex.GetType().FullName,
                             ex.Message, ex.StackTrace, optionalMessage);
            return trace;
        }
    }
}