using Domain.Exceptions;

namespace Domain.Entities.Security;

/// <summary>
/// Represents a device registered for push notification multi-factor authentication.
/// </summary>
public class MfaPushDevice
{
    /// <summary>
    /// Gets the unique identifier for this push device registration.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the MFA method ID this device belongs to.
    /// </summary>
    public Guid MfaMethodId { get; private set; }

    /// <summary>
    /// Gets the associated MFA method.
    /// </summary>
    public MfaMethod? MfaMethod { get; private set; }

    /// <summary>
    /// Gets the user ID who owns this device.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the device identifier (platform-specific token or ID).
    /// </summary>
    public string DeviceId { get; private set; } = null!;

    /// <summary>
    /// Gets the friendly name of the device.
    /// </summary>
    public string DeviceName { get; private set; } = null!;

    /// <summary>
    /// Gets the platform type (iOS, Android, etc.).
    /// </summary>
    public string Platform { get; private set; } = null!;

    /// <summary>
    /// Gets the push token for sending notifications.
    /// </summary>
    public string PushToken { get; private set; } = null!;

    /// <summary>
    /// Gets the public key for verifying signed responses from this device.
    /// </summary>
    public string PublicKey { get; private set; } = null!;

    /// <summary>
    /// Gets the timestamp when this device was registered.
    /// </summary>
    public DateTime RegisteredAt { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last successful push authentication.
    /// </summary>
    public DateTime? LastUsedAt { get; private set; }

    /// <summary>
    /// Gets whether this device is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the device trust level for risk assessment.
    /// </summary>
    public int TrustScore { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MfaPushDevice"/> class.
    /// </summary>
    /// <param name="mfaMethodId">The MFA method this device belongs to.</param>
    /// <param name="userId">The user who owns this device.</param>
    /// <param name="deviceId">The unique device identifier.</param>
    /// <param name="deviceName">The friendly name of the device.</param>
    /// <param name="platform">The platform type.</param>
    /// <param name="pushToken">The push notification token.</param>
    /// <param name="publicKey">The public key for response verification.</param>
    public MfaPushDevice(
        Guid mfaMethodId,
        Guid userId,
        string deviceId,
        string deviceName,
        string platform,
        string pushToken,
        string publicKey)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentNullException(nameof(deviceId));
        if (string.IsNullOrWhiteSpace(deviceName))
            throw new ArgumentNullException(nameof(deviceName));
        if (string.IsNullOrWhiteSpace(platform))
            throw new ArgumentNullException(nameof(platform));
        if (string.IsNullOrWhiteSpace(pushToken))
            throw new ArgumentNullException(nameof(pushToken));
        if (string.IsNullOrWhiteSpace(publicKey))
            throw new ArgumentNullException(nameof(publicKey));

        Id = Guid.NewGuid();
        MfaMethodId = mfaMethodId;
        UserId = userId;
        DeviceId = deviceId;
        DeviceName = deviceName;
        Platform = platform;
        PushToken = pushToken;
        PublicKey = publicKey;
        RegisteredAt = DateTime.UtcNow;
        IsActive = true;
        TrustScore = 50; // Start with medium trust
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MfaPushDevice"/> class for EF Core.
    /// </summary>
    private MfaPushDevice()
    {
    }

    /// <summary>
    /// Updates the push token when it changes.
    /// </summary>
    /// <param name="newToken">The new push token.</param>
    public void UpdatePushToken(string newToken)
    {
        if (string.IsNullOrWhiteSpace(newToken))
            throw new ArgumentNullException(nameof(newToken));

        PushToken = newToken;
    }

    /// <summary>
    /// Records a successful authentication from this device.
    /// </summary>
    public void RecordSuccessfulUse()
    {
        LastUsedAt = DateTime.UtcNow;
        
        // Increase trust score on successful use, max 100
        if (TrustScore < 100)
            TrustScore = Math.Min(100, TrustScore + 5);
    }

    /// <summary>
    /// Records a suspicious activity from this device.
    /// </summary>
    public void RecordSuspiciousActivity()
    {
        // Decrease trust score on suspicious activity, min 0
        TrustScore = Math.Max(0, TrustScore - 10);
        
        // Auto-disable if trust score too low and device is still active
        if (TrustScore < 20 && IsActive)
            Deactivate();
    }

    /// <summary>
    /// Deactivates this device.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            throw new InvalidStateTransitionException("Device is already inactive");

        IsActive = false;
    }

    /// <summary>
    /// Reactivates this device.
    /// </summary>
    public void Reactivate()
    {
        if (IsActive)
            throw new InvalidStateTransitionException("Device is already active");

        IsActive = true;
        TrustScore = 50; // Reset to medium trust on reactivation
    }

    /// <summary>
    /// Updates the device name.
    /// </summary>
    /// <param name="newName">The new device name.</param>
    public void UpdateDeviceName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentNullException(nameof(newName));

        DeviceName = newName;
    }
}
