using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Security;

/// <summary>
/// Represents a WebAuthn credential registered for a user's MFA method.
/// Stores public key and metadata for FIDO2/WebAuthn authentication.
/// </summary>
public class WebAuthnCredential
{
    /// <summary>
    /// Unique identifier for the credential.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The MFA method this credential belongs to.
    /// </summary>
    public Guid MfaMethodId { get; private set; }

    /// <summary>
    /// Navigation property to the parent MFA method.
    /// </summary>
    public MfaMethod MfaMethod { get; private set; } = null!;

    /// <summary>
    /// The user who owns this credential.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Base64URL-encoded credential ID from the authenticator.
    /// </summary>
    public string CredentialId { get; private set; }

    /// <summary>
    /// Base64URL-encoded public key for verification.
    /// </summary>
    public string PublicKey { get; private set; }

    /// <summary>
    /// Counter value from the authenticator (for cloned device detection).
    /// </summary>
    public uint SignCount { get; private set; }

    /// <summary>
    /// Type of authenticator (platform, cross-platform).
    /// </summary>
    public AuthenticatorType AuthenticatorType { get; private set; }

    /// <summary>
    /// Transport methods supported by the authenticator.
    /// </summary>
    public AuthenticatorTransport[] Transports { get; private set; }

    /// <summary>
    /// Whether the authenticator supports user verification.
    /// </summary>
    public bool SupportsUserVerification { get; private set; }

    /// <summary>
    /// User-friendly name for this credential.
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// Attestation type used during registration.
    /// </summary>
    public string? AttestationType { get; private set; }

    /// <summary>
    /// AAGUID (Authenticator Attestation GUID) if available.
    /// </summary>
    public string? Aaguid { get; private set; }

    /// <summary>
    /// When this credential was registered.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// When this credential was last used successfully.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; private set; }

    /// <summary>
    /// Whether this credential is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// IP address where the credential was registered.
    /// </summary>
    public string? RegistrationIpAddress { get; private set; }

    /// <summary>
    /// User agent used during registration.
    /// </summary>
    public string? RegistrationUserAgent { get; private set; }

    /// <summary>
    /// Private constructor for Entity Framework Core.
    /// </summary>
    private WebAuthnCredential()
    {
        CredentialId = string.Empty;
        PublicKey = string.Empty;
        Transports = Array.Empty<AuthenticatorTransport>();
    }

    /// <summary>
    /// Creates a new WebAuthn credential.
    /// </summary>
    /// <param name="mfaMethodId">The MFA method ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="credentialId">Base64URL-encoded credential ID</param>
    /// <param name="publicKey">Base64URL-encoded public key</param>
    /// <param name="signCount">Initial sign count</param>
    /// <param name="authenticatorType">Type of authenticator</param>
    /// <param name="transports">Supported transport methods</param>
    /// <param name="supportsUserVerification">Whether user verification is supported</param>
    /// <param name="name">User-friendly name</param>
    /// <param name="attestationType">Attestation type</param>
    /// <param name="aaguid">Authenticator AAGUID</param>
    /// <param name="ipAddress">Registration IP address</param>
    /// <param name="userAgent">Registration user agent</param>
    /// <returns>A new WebAuthn credential</returns>
    public static WebAuthnCredential Create(
        Guid mfaMethodId,
        Guid userId,
        string credentialId,
        string publicKey,
        uint signCount,
        AuthenticatorType authenticatorType,
        AuthenticatorTransport[] transports,
        bool supportsUserVerification,
        string? name = null,
        string? attestationType = null,
        string? aaguid = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (mfaMethodId == Guid.Empty)
            throw new ArgumentException("MFA method ID cannot be empty", nameof(mfaMethodId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(credentialId))
            throw new ArgumentException("Credential ID cannot be empty", nameof(credentialId));
        if (string.IsNullOrWhiteSpace(publicKey))
            throw new ArgumentException("Public key cannot be empty", nameof(publicKey));

        return new WebAuthnCredential
        {
            Id = Guid.NewGuid(),
            MfaMethodId = mfaMethodId,
            UserId = userId,
            CredentialId = credentialId,
            PublicKey = publicKey,
            SignCount = signCount,
            AuthenticatorType = authenticatorType,
            Transports = transports ?? Array.Empty<AuthenticatorTransport>(),
            SupportsUserVerification = supportsUserVerification,
            Name = name,
            AttestationType = attestationType,
            Aaguid = aaguid,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true,
            RegistrationIpAddress = ipAddress,
            RegistrationUserAgent = userAgent
        };
    }

    /// <summary>
    /// Updates the sign count after successful authentication.
    /// </summary>
    /// <param name="newSignCount">The new sign count from the authenticator</param>
    /// <returns>True if the counter is valid (greater than or equal to stored count)</returns>
    public bool UpdateSignCount(uint newSignCount)
    {
        // Sign count should always increase (or stay the same for some authenticators)
        if (newSignCount < SignCount)
        {
            // This could indicate a cloned authenticator
            return false;
        }

        SignCount = newSignCount;
        LastUsedAt = DateTimeOffset.UtcNow;
        return true;
    }

    /// <summary>
    /// Deactivates this credential.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reactivates this credential.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Updates the user-friendly name for this credential.
    /// </summary>
    /// <param name="name">The new name</param>
    public void UpdateName(string? name)
    {
        Name = name?.Trim();
    }

    /// <summary>
    /// Records usage of this credential.
    /// </summary>
    public void RecordUsage()
    {
        LastUsedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Checks if this credential can be used for authentication.
    /// </summary>
    /// <returns>True if the credential is active and valid</returns>
    public bool CanAuthenticate()
    {
        return IsActive && !string.IsNullOrWhiteSpace(CredentialId) && !string.IsNullOrWhiteSpace(PublicKey);
    }
}

/// <summary>
/// Types of WebAuthn authenticators.
/// </summary>
public enum AuthenticatorType
{
    /// <summary>
    /// Platform authenticator (built into the device).
    /// </summary>
    Platform = 0,

    /// <summary>
    /// Cross-platform authenticator (external security key).
    /// </summary>
    CrossPlatform = 1
}

/// <summary>
/// Transport methods for WebAuthn authenticators.
/// </summary>
public enum AuthenticatorTransport
{
    /// <summary>
    /// USB transport.
    /// </summary>
    Usb = 0,

    /// <summary>
    /// Near Field Communication (NFC).
    /// </summary>
    Nfc = 1,

    /// <summary>
    /// Bluetooth Low Energy (BLE).
    /// </summary>
    Ble = 2,

    /// <summary>
    /// Platform-specific transport (built-in).
    /// </summary>
    Internal = 3,

    /// <summary>
    /// Hybrid transport (QR code + proximity).
    /// </summary>
    Hybrid = 4
}
