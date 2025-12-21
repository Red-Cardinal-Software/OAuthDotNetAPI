using System.ComponentModel.DataAnnotations;
using Application.DTOs.Validation;

namespace Application.DTOs.Mfa;

/// <summary>
/// DTO for sending a push challenge request.
/// </summary>
public class SendPushChallengeDto : IValidatableDto
{
    /// <summary>
    /// Gets or sets the device to send the challenge to.
    /// </summary>
    [Required(ErrorMessage = "Device ID is required")]
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    [Required(ErrorMessage = "Session ID is required")]
    [StringLength(256, ErrorMessage = "Session ID must not exceed 256 characters")]
    public string SessionId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the location information if available.
    /// </summary>
    [StringLength(256, ErrorMessage = "Location must not exceed 256 characters")]
    public string? Location { get; set; }
}