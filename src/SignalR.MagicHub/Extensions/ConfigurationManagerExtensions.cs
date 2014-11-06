using Microsoft.AspNet.SignalR.Configuration;

namespace SignalR.MagicHub
{
    public static class ConfigurationManagerExtensions
    {
        public static IConfigurationManager AllowAnonymous(this IConfigurationManager configurationManager)
        {
            AuthorizeAttribute.IsAnonymousEnabled = true;
            return configurationManager;
        }
    }
}
