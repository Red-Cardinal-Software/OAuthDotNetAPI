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
    public string NewToken { get; set; } = null!;
}