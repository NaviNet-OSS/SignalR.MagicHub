using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalR.MagicHub.Infrastructure
{
    /// <summary>
    /// Interface which provides functionality to release a hub from IoC scope.
    /// </summary>
    public interface IHubReleaser
    {
        /// <summary>
        /// Releases the specified hub.
        /// </summary>
        /// <param name="hub">The hub.</param>
        /// <returns></returns>
        Task Release(IHub hub);
    }
}
