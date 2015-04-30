using System;
using System.Threading;
using Microsoft.AspNet.SignalR;
using Owin;

namespace SignalR.MagicHub.Owin
{
    /// <summary>
    /// Extensions for owin app initialization
    /// </summary>
    public static class OwinExtensions
    {
        /// <summary>
        /// Initializes the performance counters in an Owin context.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="resolver">The resolver.</param>
        /// <returns></returns>
        public static IAppBuilder InitializeOwinPerformanceCounters(this IAppBuilder builder, IDependencyResolver resolver)
        {
            var env = builder.Properties;
            CancellationToken token = env.GetShutdownToken();

            // If we don't get a valid instance name then generate a random one
            string instanceName = env.GetAppInstanceName() ?? Guid.NewGuid().ToString();

            resolver.InitializeMagicHubPerformanceCounters(instanceName, token);

            return builder;
        }
    }
}