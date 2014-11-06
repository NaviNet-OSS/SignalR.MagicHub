using System;
using System.Diagnostics;

namespace SignalR.MagicHub
{
    public static class TraceSourceExtensions
    {
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