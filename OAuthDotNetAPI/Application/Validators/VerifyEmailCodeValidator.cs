using Application.DTOs.Mfa.EmailMfa;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Validator for VerifyEmailCodeDto to ensure email MFA code verification requests are valid.
/// Enforces strict validation for verification codes and challenge IDs.
/// </summary>
public class VerifyEmailCodeValidator : AbstractValidator<VerifyEmailCodeDto>
{
    public VerifyEmailCodeValidator()
    {
        RuleFor(x => x.ChallengeId)
            .NotEmpty()
            .WithMessage("Challenge ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Challenge ID cannot be empty GUID");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Verification code is required")
            .Length(8)
            .WithMessage("Verification code must be exactly 8 characters")
            .Matches(@"^\d{8}$")
            .WithMessage("Verification code must contain only 8 digits")
            .Must(BeValidVerificationCode)
            .WithMessage("Invalid verification code format");
    }

    /// <summary>
    /// Additional validation to ensure the verification code meets security requirements.
    /// </summary>
    private static bool BeValidVerificationCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 8)
            return false;

        // Ensure it's all digits
        if (!code.All(char.IsDigit))
            return false;

        // Optional: Add additional security checks (e.g., not all same digit)
        if (code.All(c => c == code[0]))
            return false; // Reject codes like "00000000", "11111111", etc.

        return true;
    }
}