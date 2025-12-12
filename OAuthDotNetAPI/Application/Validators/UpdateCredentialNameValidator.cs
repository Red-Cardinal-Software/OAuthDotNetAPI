using Application.DTOs.Mfa.WebAuthn;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Validator for UpdateCredentialNameDto to ensure credential name updates are valid.
/// Enforces security requirements for credential naming.
/// </summary>
public class UpdateCredentialNameValidator : AbstractValidator<UpdateCredentialNameDto>
{
    private static readonly string[] ForbiddenNames =
    {
        "admin", "administrator", "system", "test", "default", "null", "undefined",
        "password", "secret", "key", "token", "credential"
    };

    public UpdateCredentialNameValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Credential name is required")
            .Length(1, 100)
            .WithMessage("Credential name must be between 1 and 100 characters")
            .Must(NotContainInvalidCharacters)
            .WithMessage("Credential name contains invalid characters")
            .Must(NotBeForbiddenName)
            .WithMessage("Credential name is not allowed")
            .Must(NotContainOnlyWhitespace)
            .WithMessage("Credential name cannot contain only whitespace");
    }

    /// <summary>
    /// Ensures the credential name doesn't contain potentially dangerous characters.
    /// </summary>
    private static bool NotContainInvalidCharacters(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Disallow potentially dangerous characters for security
        var invalidChars = new[] { '<', '>', '"', '\'', '&', '\0', '\r', '\n', '\t' };
        return !name.Any(c => invalidChars.Contains(c));
    }

    /// <summary>
    /// Prevents use of common system or security-related names.
    /// </summary>
    private static bool NotBeForbiddenName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return !ForbiddenNames.Contains(name.ToLowerInvariant().Trim());
    }

    /// <summary>
    /// Ensures the name contains more than just whitespace.
    /// </summary>
    private static bool NotContainOnlyWhitespace(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.Trim().Length > 0;
    }
}