
namespace SignalR.MagicHub.Authorization
{
    /// <summary>
    /// Represents a class which can provide simple ACL claim checks 
    /// </summary>
    public interface IAuthorize
    {
        /// <summary>
        /// Determines whether the specified context has claim for an authorization context.
        /// </summary>
        /// <param name="context">The context representing the claim being verified</param>
        /// <returns>true if the authorization context has a valid claim</returns>
        bool HasClaim(AuthorizationContext context);
    }
}
