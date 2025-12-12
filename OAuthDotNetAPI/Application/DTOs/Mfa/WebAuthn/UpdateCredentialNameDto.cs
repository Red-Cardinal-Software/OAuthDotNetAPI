using System.ComponentModel.DataAnnotations;
using Application.DTOs.Validation;

namespace Application.DTOs.Mfa.WebAuthn;

/// <summary>
/// DTO for updating a credential name.
/// </summary>
public class UpdateCredentialNameDto : IValidatableDto
{
    /// <summary>
    /// The new name for the credential.
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;
}
