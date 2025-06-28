using System.Security.Claims;

namespace TestUtils.Utilities;

public static class ClaimsPrincipalFactory
{
    public static ClaimsPrincipal CreateClaim(Guid orgId, Guid userId, string username = "user")
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("Organization", orgId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username)
        }, "mock");
        return new ClaimsPrincipal(identity);
    }
}