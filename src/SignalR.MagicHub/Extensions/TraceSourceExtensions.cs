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
                                  : string.Format("Error occurred: {0}\n", optionalMessage);
            
            trace.TraceError("{3}Exception Type: {0}\nException Message: {1}\nStack:\n{2}", ex.GetType().FullName,
                             ex.Message, ex.StackTrace, optionalMessage);
            return trace;
        }
    }
}