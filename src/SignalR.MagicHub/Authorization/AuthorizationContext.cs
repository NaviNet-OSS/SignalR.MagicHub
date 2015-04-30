using System.Security.Principal;

namespace SignalR.MagicHub.Authorization
{
    /// <summary>
    /// Represents the context of an authorization request for an ACL resource
    /// </summary>
    public class AuthorizationContext
    {
        /// <summary>
        /// Gets the action being requested.
        /// </summary>
        public string Action { get; private set; }

        /// <summary>
        /// Gets the ACL identifier of the resource being requested
        /// </summary>
        /// <value>
        /// The resource.
        /// </value>
        public string Resource { get; private set; }

        /// <summary>
        /// Gets the value representing the identity of the requester.
        /// </summary>
        /// <value>
        /// The requester.
        /// </value>
        public IPrincipal Requester { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationContext"/> class.
        /// </summary>
        /// <param name="requester">The requester of the action.</param>
        /// <param name="resource">The resource identifier</param>
        /// <param name="permissionType">Type of action requested</param>
        public AuthorizationContext(IPrincipal requester, string resource, string permissionType)
        {
            Action = permissionType;
            Requester = requester;
            Resource = resource;
        }
    }
}
