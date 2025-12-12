using Domain.Exceptions;

namespace Domain.Entities.Security;

/// <summary>
/// Represents an active push notification authentication challenge.
/// </summary>
public class MfaPushChallenge
{
    /// <summary>
    /// Gets the unique identifier for this challenge.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the user ID this challenge is for.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the device ID that should respond to this challenge.
    /// </summary>
    public Guid DeviceId { get; private set; }

    /// <summary>
    /// Gets the associated push device.
    /// </summary>
    public MfaPushDevice? Device { get; private set; }

    /// <summary>
    /// Gets the unique challenge code.
    /// </summary>
    public string ChallengeCode { get; private set; } = null!;

    /// <summary>
    /// Gets the session identifier this challenge is associated with.
    /// </summary>
    public string SessionId { get; private set; } = null!;

    /// <summary>
    /// Gets the IP address of the login attempt.
    /// </summary>
    public string IpAddress { get; private set; } = null!;

    /// <summary>
    /// Gets the user agent of the login attempt.
    /// </summary>
    public string UserAgent { get; private set; } = null!;

    /// <summary>
    /// Gets the location information if available.
    /// </summary>
    public string? Location { get; private set; }

    /// <summary>
    /// Gets the timestamp when this challenge was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when this challenge expires.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when this challenge was responded to.
    /// </summary>
    public DateTime? RespondedAt { get; private set; }

    /// <summary>
    /// Gets the current status of the challenge.
    /// </summary>
    public ChallengeStatus Status { get; private set; }

    /// <summary>
    /// Gets the response from the device if any.
    /// </summary>
    public ChallengeResponse? Response { get; private set; }

    /// <summary>
    /// Gets the signature of the response for verification.
    /// </summary>
    public string? ResponseSignature { get; private set; }

    /// <summary>
    /// Gets any additional context data for the challenge.
    /// </summary>
    public string? ContextData { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MfaPushChallenge"/> class.
    /// </summary>
    /// <param name="userId">The user this challenge is for.</param>
    /// <param name="deviceId">The device that should respond.</param>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="ipAddress">The IP address of the login attempt.</param>
    /// <param name="userAgent">The user agent of the login attempt.</param>
    /// <param name="expiryMinutes">Minutes until the challenge expires.</param>
    public MfaPushChallenge(
        Guid userId,
        Guid deviceId,
        string sessionId,
        string ipAddress,
        string userAgent,
        int expiryMinutes = 5)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentNullException(nameof(sessionId));
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentNullException(nameof(ipAddress));
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentNullException(nameof(userAgent));
        if (expiryMinutes <= 0 || expiryMinutes > 30)
            throw new ArgumentException("Expiry must be between 1 and 30 minutes", nameof(expiryMinutes));

        Id = Guid.NewGuid();
        UserId = userId;
        DeviceId = deviceId;
        SessionId = sessionId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        ChallengeCode = GenerateChallengeCode();
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.AddMinutes(expiryMinutes);
        Status = ChallengeStatus.Pending;
        Response = ChallengeResponse.None;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MfaPushChallenge"/> class for EF Core.
    /// </summary>
    private MfaPushChallenge()
    {
    }

    /// <summary>
    /// Sets the location information for the challenge.
    /// </summary>
    /// <param name="location">The location description.</param>
    public void SetLocation(string location)
    {
        if (!string.IsNullOrWhiteSpace(location))
            Location = location;
    }

    /// <summary>
    /// Sets additional context data for the challenge.
    /// </summary>
    /// <param name="contextData">The context data (JSON).</param>
    public void SetContextData(string contextData)
    {
        if (!string.IsNullOrWhiteSpace(contextData))
            ContextData = contextData;
    }

    /// <summary>
    /// Approves the challenge with a signed response.
    /// </summary>
    /// <param name="signature">The cryptographic signature of the response.</param>
    public void Approve(string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
            throw new ArgumentNullException(nameof(signature));
        
        ValidateCanRespond();

        Status = ChallengeStatus.Approved;
        Response = ChallengeResponse.Approved;
        ResponseSignature = signature;
        RespondedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Denies the challenge with a signed response.
    /// </summary>
    /// <param name="signature">The cryptographic signature of the response.</param>
    public void Deny(string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
            throw new ArgumentNullException(nameof(signature));
        
        ValidateCanRespond();

        Status = ChallengeStatus.Denied;
        Response = ChallengeResponse.Denied;
        ResponseSignature = signature;
        RespondedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the challenge as expired.
    /// </summary>
    public void MarkExpired()
    {
        if (Status != ChallengeStatus.Pending)
            throw new InvalidStateTransitionException("Only pending challenges can be expired");

        Status = ChallengeStatus.Expired;
    }

    /// <summary>
    /// Marks the challenge as consumed after successful authentication.
    /// </summary>
    public void MarkConsumed()
    {
        if (Status != ChallengeStatus.Approved)
            throw new InvalidStateTransitionException("Only approved challenges can be consumed");

        Status = ChallengeStatus.Consumed;
    }

    /// <summary>
    /// Validates that the challenge can be responded to.
    /// </summary>
    private void ValidateCanRespond()
    {
        if (Status != ChallengeStatus.Pending)
            throw new InvalidStateTransitionException($"Challenge is already {Status}");

        if (DateTime.UtcNow > ExpiresAt)
            throw new InvalidStateTransitionException("Challenge has expired");
    }

    /// <summary>
    /// Generates a unique challenge code.
    /// </summary>
    private static string GenerateChallengeCode()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .Substring(0, 16);
    }
}

/// <summary>
/// The status of a push challenge.
/// </summary>
public enum ChallengeStatus
{
    Pending,
    Approved,
    Denied,
    Expired,
    Consumed
}

/// <summary>
/// The response to a push challenge.
/// </summary>
public enum ChallengeResponse
{
    None,
    Approved,
    Denied
}
