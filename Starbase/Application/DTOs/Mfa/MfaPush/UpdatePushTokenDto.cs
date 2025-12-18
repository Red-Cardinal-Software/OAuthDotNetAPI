using System.ComponentModel.DataAnnotations;
using Application.DTOs.Validation;

namespace Application.DTOs.Mfa;

/// <summary>
/// DTO for updating a device's push token.
/// </summary>
public class UpdatePushTokenDto : IValidatableDto
{
    /// <summary>
    /// Gets or sets the new push token.
    /// </summary>
    [Required(ErrorMessage = "New token is required")]
    [StringLength(512, ErrorMessage = "Push token must not exceed 512 characters")]
    public string NewToken { get; set; } = null!;
}