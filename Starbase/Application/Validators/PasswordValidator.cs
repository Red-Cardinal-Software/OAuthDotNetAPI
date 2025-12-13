using Application.Common.Configuration;
using Application.Common.Constants;
using Application.Interfaces.Security;
using FluentValidation;
using Microsoft.Extensions.Options;

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
/// <param name="appOptions">
/// Provides access to application configuration options, such as password length constraints.
/// </param>
/// <param name="blacklistedPasswordRepository">
/// An interface used to check if a given password is blacklisted.
/// </param>
public class PasswordValidator : AbstractValidator<string>
{
    public PasswordValidator(IOptions<AppOptions> appOptions, IBlacklistedPasswordRepository blacklistedPasswordRepository)
    {
        var options = appOptions.Value;
        var minPasswordLength = options.PasswordMinimumLength;
        var maxPasswordLength = options.PasswordMaximumLength;
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
}
