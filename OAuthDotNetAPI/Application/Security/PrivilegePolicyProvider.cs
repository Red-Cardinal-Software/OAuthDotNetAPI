using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Application.Security;

/// <summary>
/// Provides a custom authorization policy provider that enables dynamic generation of policies based on privilege requirements.
/// </summary>
/// <remarks>
/// This class allows defining custom authorization policies dynamically by interpreting policy names with a specific prefix and extracting privilege strings from them.
/// If a policy name does not match the expected pattern, the default authorization policy provider is used as a fallback.
/// </remarks>
public class PrivilegePolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private const string PolicyPrefix = RequirePrivilegeAttribute.Prefix;

    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var privileges = policyName.Substring(PolicyPrefix.Length + 1).Split(';', StringSplitOptions.RemoveEmptyEntries);

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PrivilegeRequirement(privileges))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
