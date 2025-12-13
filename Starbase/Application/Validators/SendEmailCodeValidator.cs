using Application.DTOs.Mfa.EmailMfa;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Validator for SendEmailCodeDto to ensure email MFA code sending requests are valid.
/// Provides comprehensive validation including email format and challenge ID validation.
/// </summary>
public class SendEmailCodeValidator : AbstractValidator<SendEmailCodeDto>
{
    public SendEmailCodeValidator()
    {
        RuleFor(x => x.ChallengeId)
            .NotEmpty()
            .WithMessage("Challenge ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Challenge ID cannot be empty GUID");

        RuleFor(x => x.EmailAddress)
            .EmailAddress()
            .WithMessage("Invalid email address format")
            .When(x => !string.IsNullOrWhiteSpace(x.EmailAddress))
            .Length(5, 254)
            .WithMessage("Email address must be between 5 and 254 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.EmailAddress));
    }
}