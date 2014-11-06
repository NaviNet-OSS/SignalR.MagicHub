using System.Security.Principal;

namespace SignalR.MagicHub.Authorization
{
    public class AuthorizationContext
    {
        public string Action { get; private set; }

        public string Resource { get; private set; }

        public IPrincipal Requester { get; private set; }

        public AuthorizationContext(IPrincipal requester, string resource, string permissionType)
        {
            Action = permissionType;
            Requester = requester;
            Resource = resource;
        }
    }
}
