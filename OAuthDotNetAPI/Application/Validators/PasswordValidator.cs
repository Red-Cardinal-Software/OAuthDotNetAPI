using Application.Common.Constants;
using Application.Interfaces.Security;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace Application.Validators;

/// <summary>
/// Represents a validator used to validate password strings according to defined rules.
/// </summary>
/// <remarks>
/// This validator ensures that:
/// - The password is not empty.
/// - The password meets the minimum and maximum length requirements, which are configurable.
/// - The password is not present in a blacklist of common or insecure passwords.
/// </remarks>
/// <param name="configuration">
/// Provides access to application configuration settings, such as password length constraints.
/// </param>
/// <param name="blacklistedPasswordRepository">
/// An interface used to check if a given password is blacklisted.
/// </param>
public class PasswordValidator : AbstractValidator<string>
{
    public PasswordValidator(IConfiguration configuration, IBlacklistedPasswordRepository blacklistedPasswordRepository)
    {
        var minPasswordLength = GetMinimumPasswordLength(configuration);
        var maxPasswordLength = GetMaximumPasswordLength(configuration);
        RuleFor(p => p)
            .NotEmpty()
            .WithMessage(ServiceResponseConstants.PasswordMustNotBeEmpty)
            .MinimumLength(minPasswordLength)
            .WithMessage(ServiceResponseConstants.PasswordDoesNotMeetMinimumLengthRequirements)
            .MaximumLength(maxPasswordLength)
            .WithMessage(ServiceResponseConstants.PasswordExceedsMaximumLengthRequirements)
            .MustAsync(async (password, _) => !await blacklistedPasswordRepository.IsPasswordBlacklistedAsync(password))
            .WithMessage(ServiceResponseConstants.PasswordIsBlacklisted);
    }
    
    private static int GetMinimumPasswordLength(IConfiguration configuration)
    {
        if (!int.TryParse(configuration["PasswordMinimumLength"], out var passwordMinimumLength))
        {
            passwordMinimumLength = SystemDefaults.DefaultPasswordMinimumLength;
        }

        return passwordMinimumLength;
    }
    
    private static int GetMaximumPasswordLength(IConfiguration configuration)
    {
        if (!int.TryParse(configuration["PasswordMaximumLength"], out var passwordMaximumLength))
        {
            passwordMaximumLength = SystemDefaults.DefaultPasswordMaximumLength;
        }

        return passwordMaximumLength;
    }
}