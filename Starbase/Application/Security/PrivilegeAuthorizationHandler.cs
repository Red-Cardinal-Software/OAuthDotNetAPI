using Microsoft.AspNetCore.Authorization;

namespace Application.Security;

/// <summary>
/// A custom authorization handler that evaluates privilege-based requirements
/// and determines whether a user meets the required privileges specified
/// in the associated <see cref="PrivilegeRequirement"/>.
/// </summary>
/// <remarks>
/// This class inherits from the <see cref="AuthorizationHandler{TRequirement}"/> base class
/// and implements the logic to check if the logged-in user possesses any of the required
/// privileges defined in the provided <see cref="PrivilegeRequirement"/>.
/// </remarks>
/// <remarks>
/// The user's privileges are expected to be provided as claims with the type "priv".
/// </remarks>
/// <remarks>
/// If a user's privileges match any of the required privileges listed in the requirement,
/// the authorization request will succeed.
/// </remarks>
public class PrivilegeAuthorizationHandler : AuthorizationHandler<PrivilegeRequirement>
{
    /// <summary>
    /// Handles an authorization requirement by evaluating if the user's privileges satisfy
    /// the specified requirement.
    /// </summary>
    /// <param name="context">
    /// The authorization context, which contains information about the current user
    /// and their claims.
    /// </param>
    /// <param name="requirement">
    /// The privilege requirement that specifies the necessary privileges to fulfill the
    /// authorization request.
    /// </param>
    /// <returns>
    /// A completed <see cref="Task"/> after evaluating the authorization requirement. The
    /// requirement is marked as succeeded if the user's privileges match any of the required
    /// privileges.
    /// </returns>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PrivilegeRequirement requirement)
    {
        var userPrivileges = context.User.FindAll("priv").Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (requirement.RequiredPrivileges.Any(r => userPrivileges.Contains(r)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
