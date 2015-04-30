using System;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;

namespace SignalR.MagicHub.Infrastructure
{
    /// <summary>
    /// Implementation of a set of algorithms for logic associated with tracing.
    /// </summary>
    public class TraceStrategy
    {

        /// <summary>
        /// Inspects the given message and returns true if this message should be traced. 
        /// True if message contains tracing_enabled: true flag.
        /// </summary>
        /// <param name="jsonMessage">The json message.</param>
        /// <returns>true if the message should be traced.</returns>
        /// 
        public virtual bool ShouldTraceMessage(string jsonMessage)
        {
            try
            {
                var json = JsonConvert.DeserializeObject<dynamic>(jsonMessage);

                return json != null && json.tracing_enabled != null && json.tracing_enabled.Value == true;
            }
            catch (Exception)
            {
                // Message not in JSON format
                return false;
            }
        }
    }
}