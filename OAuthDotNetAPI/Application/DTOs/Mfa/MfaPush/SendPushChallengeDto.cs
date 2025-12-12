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
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    public string SessionId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the location information if available.
    /// </summary>
    public string? Location { get; set; }
}