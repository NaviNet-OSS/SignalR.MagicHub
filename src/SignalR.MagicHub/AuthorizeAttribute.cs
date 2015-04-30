using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using SignalR.MagicHub.Authorization;

namespace SignalR.MagicHub
{
    /// <summary>
    /// Instructs SignalR that a hub action requires authorization and provides an in interface to
    /// integrate with authorization frameworks
    /// </summary>
    public class AuthorizeAttribute : Microsoft.AspNet.SignalR.AuthorizeAttribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether or non full anonymous mode is enabled. 
        /// </summary>
        /// <value>
        /// if <c>true</c>, then all authorization checks will be suspended. Useful for debugging or
        /// running locally where authorization may not be available.
        /// </value>
        public static bool IsAnonymousEnabled { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class which loads an 
        /// instance of <see cref="IAuthorize"/> from SignalR's default dependency resolver.
        /// </summary>
        public AuthorizeAttribute()
            : this(GlobalHost.DependencyResolver.Resolve<IAuthorize>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class.
        /// </summary>
        /// <param name="authorizer">The authorization framework integration interface</param>
        public AuthorizeAttribute(IAuthorize authorizer)
        {
            WhiteList = new string[0];
            Blacklist = new string[0];

            _authorizer = authorizer;
        }



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

        private readonly IAuthorize _authorizer = null;

        /// <summary>
        /// Determines whether client is authorized to connect to <see cref="T:Microsoft.AspNet.SignalR.Hubs.IHub" />.
        /// </summary>
        /// <param name="hubDescriptor">Description of the hub client is attempting to connect to.</param>
        /// <param name="request">The (re)connect request from the client.</param>
        /// <returns>
        /// true if the caller is authorized to connect to the hub; otherwise, false.
        /// </returns>
        public override bool AuthorizeHubConnection(HubDescriptor hubDescriptor, IRequest request)
        {
            return IsAnonymousEnabled || base.AuthorizeHubConnection(hubDescriptor, request);
        }

        /// <summary>
        /// Determines whether client is authorized to invoke the <see cref="T:Microsoft.AspNet.SignalR.Hubs.IHub" /> method.
        /// </summary>
        /// <param name="hubIncomingInvokerContext">An <see cref="T:Microsoft.AspNet.SignalR.Hubs.IHubIncomingInvokerContext" /> providing details regarding the <see cref="T:Microsoft.AspNet.SignalR.Hubs.IHub" /> method invocation.</param>
        /// <param name="appliesToMethod">Indicates whether the interface instance is an attribute applied directly to a method.</param>
        /// <returns>
        /// true if the caller is authorized to invoke the <see cref="T:Microsoft.AspNet.SignalR.Hubs.IHub" /> method; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Gets the resource keys out of the method invocation metadata for the hub method being called.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">No resource key found on target method. A method that requires authorization must have a resource key.</exception>
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
