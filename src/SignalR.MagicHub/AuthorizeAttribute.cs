using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using SignalR.MagicHub.Authorization;

namespace SignalR.MagicHub
{
    public class AuthorizeAttribute : Microsoft.AspNet.SignalR.AuthorizeAttribute
    {

        public AuthorizeAttribute()
            : this(GlobalHost.DependencyResolver.Resolve<IAuthorize>())
        {
        }

        public AuthorizeAttribute(IAuthorize authorizer)
        {
            WhiteList = new string[0];
            Blacklist = new string[0];

            _authorizer = authorizer;
        }


        public static bool IsAnonymousEnabled { get; set; }

        /// <summary>
        /// Gets or sets the value indicating what kind of permission a method requires. Only valid on methods. 
        /// This is a cue to the underlying authorization framework implementation indicating what kind of permission is required.
        /// </summary>
        public string PermissionType { get; set; }

        /// <summary>
        /// Gets or sets a value representing the indices of the resource keys for the method
        /// </summary>
        public int[] ResourceIndices { get; set; }

        /// <summary>
        /// Gets or sets the list of resource keys which are whitelisted for an action. (Method only)
        /// </summary>
        public string[] WhiteList { get; set; }

        /// <summary>
        /// Gets or sets the list of resource keys which are blacklisted for an action. (Method only)
        /// </summary>
        public string[] Blacklist { get; set; }

        private IAuthorize _authorizer = null;

        public override bool AuthorizeHubConnection(HubDescriptor hubDescriptor, IRequest request)
        {
            return IsAnonymousEnabled || base.AuthorizeHubConnection(hubDescriptor, request);
        }

        public override bool AuthorizeHubMethodInvocation(IHubIncomingInvokerContext hubIncomingInvokerContext, bool appliesToMethod)
        {
            if (IsAnonymousEnabled)
            {
                return true;
            }

            if (!base.AuthorizeHubMethodInvocation(hubIncomingInvokerContext, appliesToMethod))
            {
                return false;
            }

            if (appliesToMethod)
            {
                if (_authorizer == null)
                {
                    return true;
                }

                IPrincipal user = hubIncomingInvokerContext.Hub.Context.User;
                IEnumerable<string> keys = GetResourceKeys(hubIncomingInvokerContext);

                return !keys.Any((key) => Blacklist.Contains(key)) && keys.All((key) =>
                        WhiteList.Contains(key) || _authorizer.HasClaim(
                            new AuthorizationContext(user, key, PermissionType)));
            }

            return true;
        }

        private IEnumerable<string> GetResourceKeys(IHubIncomingInvokerContext context)
        {
            if ((ResourceIndices == null || ResourceIndices.Length == 0))
            {
                string firstStringArg = (string)context.Args.FirstOrDefault((x) => x is string);

                if (!string.IsNullOrWhiteSpace(firstStringArg))
                {
                    return new[] { firstStringArg };
                }
                else
                {
                    throw new InvalidOperationException("No resource key found on target method. A method that requires authorization must have a resource key.");
                }
            }

            return ResourceIndices.Select(index => (string)context.Args[index]);
        }
    }
}
