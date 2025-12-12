using Application.DTOs.Mfa;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Validator for SendPushChallengeDto to ensure push challenge requests are valid.
/// Enforces security requirements for push notification challenges.
/// </summary>
public class SendPushChallengeValidator : AbstractValidator<SendPushChallengeDto>
{
    public SendPushChallengeValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .WithMessage("Device ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Device ID cannot be empty GUID");

        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required")
            .Length(10, 128)
            .WithMessage("Session ID must be between 10 and 128 characters")
            .Must(BeValidSessionId)
            .WithMessage("Session ID format is invalid");

        RuleFor(x => x.Location)
            .Length(0, 100)
            .WithMessage("Location must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Location))
            .Must(NotContainInvalidCharacters)
            .WithMessage("Location contains invalid characters")
            .When(x => !string.IsNullOrEmpty(x.Location));
    }

    /// <summary>
    /// Validates session ID format for security.
    /// </summary>
    private static bool BeValidSessionId(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        // Session ID should be alphanumeric with optional hyphens and underscores
        return sessionId.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
    }

    /// <summary>
    /// Ensures location doesn't contain potentially dangerous characters.
    /// </summary>
    private static bool NotContainInvalidCharacters(string location)
    {
        if (string.IsNullOrEmpty(location))
            return true;

        // Disallow potentially dangerous characters
        var invalidChars = new[] { '<', '>', '"', '\'', '&', '\0', '\r', '\n', '\t' };
        return !location.Any(c => invalidChars.Contains(c));
    }
}