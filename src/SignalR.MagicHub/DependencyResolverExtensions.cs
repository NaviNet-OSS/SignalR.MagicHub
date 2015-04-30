
using System;
using System.Threading;
using Microsoft.AspNet.SignalR;
using SignalR.MagicHub.Performance;

namespace SignalR.MagicHub
{
    /// <summary>
    /// Extensions class for <see cref="IDependencyResolver"/>
    /// </summary>
    public static class DependencyResolverExtensions
    {
        /// <summary>
        /// Initializes the magic hub performance counters.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        /// <param name="instanceName">Name of this process instance. (For performance counter purposes)</param>
        /// <param name="hostShutdownToken">The host shutdown token to be used to release counters when process shuts down</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// resolver
        /// or
        /// instanceName
        /// </exception>
        public static IDependencyResolver InitializeMagicHubPerformanceCounters(this IDependencyResolver resolver,
    string instanceName, CancellationToken hostShutdownToken)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            if (string.IsNullOrEmpty(instanceName))
            {
                throw new ArgumentNullException("instanceName");
            }

            var counters = resolver.Resolve<IMagicHubPerformanceCounterManager>();
            if (counters != null)
            {
                counters.Initialize(instanceName, hostShutdownToken);
            }

            return resolver;
        }
    }
}