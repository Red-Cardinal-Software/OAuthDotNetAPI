using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Mfa;

/// <summary>
/// Request to register a device for push notifications.
/// </summary>
public class RegisterPushDeviceRequest
{
    /// <summary>
    /// Gets or sets the unique device identifier.
    /// </summary>
    [Required(ErrorMessage = "Device ID is required")]
    [StringLength(128, ErrorMessage = "Device ID must not exceed 128 characters")]
    public string DeviceId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the friendly name of the device.
    /// </summary>
    [Required(ErrorMessage = "Device name is required")]
    [StringLength(100, ErrorMessage = "Device name must not exceed 100 characters")]
    public string DeviceName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the platform (iOS, Android, etc.).
    /// </summary>
    [Required(ErrorMessage = "Platform is required")]
    [StringLength(50, ErrorMessage = "Platform must not exceed 50 characters")]
    public string Platform { get; set; } = null!;

    /// <summary>
    /// Gets or sets the push notification token.
    /// </summary>
    [Required(ErrorMessage = "Push token is required")]
    [StringLength(512, ErrorMessage = "Push token must not exceed 512 characters")]
    public string PushToken { get; set; } = null!;

    /// <summary>
    /// Gets or sets the public key for response verification.
    /// </summary>
    [Required(ErrorMessage = "Public key is required")]
    [StringLength(2048, ErrorMessage = "Public key must not exceed 2048 characters")]
    public string PublicKey { get; set; } = null!;
}

/// <summary>
/// DTO for a registered push device.
/// </summary>
public class MfaPushDeviceDto
{
    /// <summary>
    /// Gets or sets the device ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the device identifier.
    /// </summary>
    public string DeviceId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the friendly device name.
    /// </summary>
    public string DeviceName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the platform.
    /// </summary>
    public string Platform { get; set; } = null!;

    /// <summary>
    /// Gets or sets when the device was registered.
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Gets or sets when the device was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the device is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the trust score.
    /// </summary>
    public int TrustScore { get; set; }
}

/// <summary>
/// Session information for push challenges.
/// </summary>
public class PushChallengeSessionInfo
{
    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    public string SessionId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the IP address.
    /// </summary>
    public string IpAddress { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user agent.
    /// </summary>
    public string UserAgent { get; set; } = null!;

    /// <summary>
    /// Gets or sets the location if available.
    /// </summary>
    public string? Location { get; set; }
}

/// <summary>
/// DTO for a push challenge.
/// </summary>
public class MfaPushChallengeDto
{
    /// <summary>
    /// Gets or sets the challenge ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the challenge code.
    /// </summary>
    public string ChallengeCode { get; set; } = null!;

    /// <summary>
    /// Gets or sets when the challenge expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the device that should respond.
    /// </summary>
    public string DeviceName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the login location.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the browser/app info.
    /// </summary>
    public string BrowserInfo { get; set; } = null!;
}

/// <summary>
/// Status of a push challenge.
/// </summary>
public class MfaPushChallengeStatusDto
{
    /// <summary>
    /// Gets or sets the challenge ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the current status.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets whether the challenge is approved.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Gets or sets whether the challenge is denied.
    /// </summary>
    public bool IsDenied { get; set; }

    /// <summary>
    /// Gets or sets whether the challenge has expired.
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Gets or sets when the response was received.
    /// </summary>
    public DateTime? RespondedAt { get; set; }
}

/// <summary>
/// Response to a push challenge.
/// </summary>
public class PushChallengeResponse
{
    /// <summary>
    /// Gets or sets whether the challenge is approved.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Gets or sets the cryptographic signature.
    /// </summary>
    [Required(ErrorMessage = "Signature is required")]
    [StringLength(1024, ErrorMessage = "Signature must not exceed 1024 characters")]
    public string Signature { get; set; } = null!;

    /// <summary>
    /// Gets or sets the device ID responding.
    /// </summary>
    [Required(ErrorMessage = "Device ID is required")]
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Gets or sets any additional context.
    /// </summary>
    [StringLength(500, ErrorMessage = "Context must not exceed 500 characters")]
    public string? Context { get; set; }
}