using Microsoft.AspNet.SignalR.Hubs;

namespace SignalR.MagicHub.Authorization
{
    public interface IAuthorize
    {
        bool HasClaim(AuthorizationContext context);
    }
}
