using Microsoft.AspNetCore.Authorization;

namespace Application.Security;

/// <summary>
/// Represents an authorization requirement based on user privileges.
/// </summary>
/// <remarks>
/// This class is used to specify a list of required privileges for an authorization policy.
/// It implements the <see cref="IAuthorizationRequirement"/> interface, which is a marker
/// interface used by ASP.NET Core's authorization infrastructure.
/// </remarks>
/// <remarks>
/// Instances of this class are typically used in conjunction with a custom authorization handler,
/// such as <c>PrivilegeAuthorizationHandler</c>, to determine whether a user has sufficient
/// privileges to access a specific resource or perform a specific operation.
/// </remarks>
public class PrivilegeRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the collection of privileges required to satisfy the authorization requirement.
    /// </summary>
    /// <remarks>
    /// This property contains an immutable collection of strings that represent the
    /// specific privileges necessary for a user to meet the corresponding
    /// <see cref="PrivilegeRequirement"/>. These privileges are typically compared
    /// with the user's assigned privileges within an authorization handler.
    /// </remarks>
    public IReadOnlyCollection<string> RequiredPrivileges { get; }

    /// <summary>
    /// Represents an authorization requirement based on user-defined privileges.
    /// </summary>
    /// <remarks>
    /// This class is used to define a set of required privileges that a user must have to meet the authorization condition.
    /// The requirement works in conjunction with ASP.NET Core's authorization infrastructure, particularly with
    /// custom authorization handlers designed to verify user privileges.
    /// </remarks>
    /// <remarks>
    /// The <see cref="PrivilegeRequirement"/> is typically used in authorization policies and enables more granular
    /// access control by associating a specific set of privileges with a policy.
    /// </remarks>
    public PrivilegeRequirement(IEnumerable<string> requiredPrivileges)
    {
        RequiredPrivileges = requiredPrivileges.ToArray();
    }
}
