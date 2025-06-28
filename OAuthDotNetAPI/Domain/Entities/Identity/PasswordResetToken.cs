namespace Domain.Entities.Identity;

/// <summary>
/// Represents a password reset token issued to a user for resetting their password.
/// </summary>
public class PasswordResetToken
{
    /// <summary>
    /// Gets the unique identifier of the reset token.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the ID of the user associated with this token.
    /// </summary>
    public Guid AppUserId { get; private set; }

    /// <summary>
    /// Gets the user associated with this token.
    /// </summary>
    public AppUser AppUser { get; private set; } = null!;

    /// <summary>
    /// Gets the UTC expiration time of the token.
    /// </summary>
    public DateTime Expiration { get; private set; }

    /// <summary>
    /// Gets the IP address from which the token was created.
    /// </summary>
    public string CreatedByIp { get; private set; } = null!;

    /// <summary>
    /// Gets the IP address from which the token was claimed, if any.
    /// </summary>
    public string? ClaimedByIp { get; private set; }

    /// <summary>
    /// Gets the UTC time the token was claimed, if any.
    /// </summary>
    public DateTime? ClaimedDate { get; private set; }

    /// <summary>
    /// EF Core constructor.
    /// </summary>
    public PasswordResetToken() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordResetToken"/> class.
    /// </summary>
    /// <param name="appUser">The user to associate with the token.</param>
    /// <param name="expiration">The expiration time of the token.</param>
    /// <param name="createdByIp">The IP address from which the request originated.</param>
    /// <exception cref="ArgumentNullException">Thrown when user or IP is null.</exception>
    public PasswordResetToken(AppUser appUser, DateTime expiration, string createdByIp)
    {
        ArgumentNullException.ThrowIfNull(appUser);
        ArgumentNullException.ThrowIfNull(createdByIp);

        Id = Guid.NewGuid();
        AppUserId = appUser.Id;
        AppUser = appUser;
        Expiration = expiration;
        CreatedByIp = createdByIp;
    }

    /// <summary>
    /// Marks the token as claimed.
    /// </summary>
    /// <param name="newHashedPassword">The hashed new password</param>
    /// <param name="claimedByIp">The IP address from which the token was claimed.</param>
    /// <exception cref="ArgumentNullException">Thrown when the IP is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the token has already been claimed</exception>
    public void Claim(string newHashedPassword, string claimedByIp)
    {
        ArgumentNullException.ThrowIfNull(claimedByIp);
        if(IsClaimed())
            throw new InvalidOperationException("Token already claimed.");
        AppUser.ChangePassword(newHashedPassword);
        ClaimedDate = DateTime.UtcNow;
        ClaimedByIp = claimedByIp;
    }

    /// <summary>
    /// Marks the redundant token as claimed
    /// </summary>
    /// <param name="claimedByIp">The IP address from which the token was claimed.</param>
    /// <exception cref="ArgumentNullException">Thrown when the IP is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the token has already been claimed</exception>
    public void ClaimRedundantToken(string claimedByIp)
    {
        ArgumentNullException.ThrowIfNull(claimedByIp);
        if(IsClaimed())
            throw new InvalidOperationException("Token already claimed.");
        ClaimedDate = DateTime.UtcNow;
        ClaimedByIp = claimedByIp;
    }

    /// <summary>
    /// Indicates whether the token has been claimed.
    /// </summary>
    public bool IsClaimed() => ClaimedDate.HasValue;

    /// <summary>
    /// Indicates whether the token has expired.
    /// </summary>
    public bool IsExpired() => DateTime.UtcNow > Expiration;
}
