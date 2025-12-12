using Application.DTOs.Mfa;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Validator for UpdatePushTokenDto to ensure push token updates are valid.
/// Enforces security requirements for push notification tokens.
/// </summary>
public class UpdatePushTokenValidator : AbstractValidator<UpdatePushTokenDto>
{
    public UpdatePushTokenValidator()
    {
        RuleFor(x => x.NewToken)
            .NotEmpty()
            .WithMessage("Push token is required")
            .Length(10, 4096)
            .WithMessage("Push token must be between 10 and 4096 characters")
            .Must(BeValidPushToken)
            .WithMessage("Push token format is invalid")
            .Must(NotContainSensitiveData)
            .WithMessage("Push token appears to contain sensitive information");
    }

    /// <summary>
    /// Validates push token format based on common push notification service patterns.
    /// </summary>
    private static bool BeValidPushToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        // Push tokens are typically alphanumeric with some special characters
        // Allow base64-like characters plus some common push service characters
        var allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_.:";
        return token.All(c => allowedChars.Contains(c));
    }

    /// <summary>
    /// Basic check to ensure the token doesn't contain obvious sensitive patterns.
    /// </summary>
    private static bool NotContainSensitiveData(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var lowerToken = token.ToLowerInvariant();

        // Check for obvious sensitive patterns that shouldn't be in push tokens
        var sensitivePatterns = new[]
        {
            "password", "secret", "key", "private", "credential", "bearer",
            "authorization", "session", "cookie", "jwt", "token="
        };

        return !sensitivePatterns.Any(pattern => lowerToken.Contains(pattern));
    }
}