using Application.Common.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Application.Security;

/// <summary>
/// An attribute that enforces a requirement for the active state of the user.
/// </summary>
/// <remarks>
/// This attribute is implemented as an authorization filter to ensure that only active
/// users can access the decorated endpoints. If the user is not active, the filter
/// interrupts the request and returns an HTTP 401 Unauthorized response.
/// </remarks>
/// <example>
/// Use this attribute to enhance security by restricting access to resources or endpoints
/// unless the user's account satisfies the active state requirement.
/// </example>
public class RequireActiveUserAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!RoleUtility.IsUserActive(user))
        {
            context.Result = new UnauthorizedResult();
        }
    }
}