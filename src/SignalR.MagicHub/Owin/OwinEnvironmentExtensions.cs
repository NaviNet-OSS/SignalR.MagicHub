using System;
using System.Collections.Generic;
using System.Threading;

namespace SignalR.MagicHub.Owin
{
    internal static class OwinEnvironmentExtensions
    {
        internal static CancellationToken GetShutdownToken(this IDictionary<string, object> env)
        {
            object value;
            return env.TryGetValue(OwinConstants.HostOnAppDisposing, out value)
                   && value is CancellationToken
                ? (CancellationToken) value
                : default(CancellationToken);
        }

        internal static string GetAppInstanceName(this IDictionary<string, object> environment)
        {
            object value;
            if (environment.TryGetValue(OwinConstants.HostAppNameKey, out value))
            {
                var stringVal = value as string;

                if (!String.IsNullOrEmpty(stringVal))
                {
                    return stringVal;
                }
            }

            return null;
        }
    }
}