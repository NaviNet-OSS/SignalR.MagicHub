using Microsoft.AspNet.SignalR.Configuration;

namespace SignalR.MagicHub
{
    /// <summary>
    /// Extension method class for <see cref="IConfigurationManager"/>
    /// </summary>
    public static class ConfigurationManagerExtensions
    {
        /// <summary>
        /// Enables full-anonymous mode for hubs using <see cref="AuthorizeAttribute"/>
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <returns></returns>
        public static IConfigurationManager AllowAnonymous(this IConfigurationManager configurationManager)
        {
            AuthorizeAttribute.IsAnonymousEnabled = true;
            return configurationManager;
        }
    }
}
