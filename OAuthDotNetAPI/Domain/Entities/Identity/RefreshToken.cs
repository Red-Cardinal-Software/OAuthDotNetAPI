namespace Domain.Entities.Identity;

/// <summary>
/// Represents a refresh token used to re-authenticate a user without requiring credentials again.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Gets the unique identifier for this refresh token.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the logical token family ID for this refresh token chain.
    /// </summary>
    public Guid TokenFamily { get; private set; }

    /// <summary>
    /// Gets the ID of the user to whom this token belongs.
    /// </summary>
    public Guid AppUserId { get; private set; }

    /// <summary>
    /// Gets the user associated with this refresh token.
    /// </summary>
    public AppUser AppUser { get; private set; } = null!;

    /// <summary>
    /// Gets the UTC expiry date and time of the refresh token.
    /// </summary>
    public DateTime Expires { get; private set; }

    /// <summary>
    /// Gets the IP address from which the token was created.
    /// </summary>
    public string CreatedByIp { get; private set; } = null!;

    /// <summary>
    /// Gets the UTC date and time the token was created.
    /// </summary>
    public DateTime CreatedDate { get; private set; }

    /// <summary>
    /// Gets the ID of the token that replaced this token, if applicable.
    /// </summary>
    public string? ReplacedBy { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshToken"/> class.
    /// </summary>
    /// <param name="appUser">The user to associate with this token.</param>
    /// <param name="expires">The expiration time of the token.</param>
    /// <param name="createdByIp">The IP address where the token was created.</param>
    /// <param name="tokenFamily">The token family ID (used to track token rotation).</param>
    /// <param name="replacedBy">The token ID that replaced this one, if any.</param>
    /// <exception cref="ArgumentNullException">Thrown if any required argument is null.</exception>
    public RefreshToken(
        AppUser appUser,
        DateTime expires,
        string createdByIp,
        Guid? tokenFamily = null,
        string? replacedBy = null)
    {
        ArgumentNullException.ThrowIfNull(appUser);
        ArgumentNullException.ThrowIfNull(createdByIp);

        Id = Guid.NewGuid();
        TokenFamily = tokenFamily ?? Guid.NewGuid();
        AppUserId = appUser.Id;
        AppUser = appUser;
        Expires = expires;
        CreatedByIp = createdByIp;
        CreatedDate = DateTime.UtcNow;
        ReplacedBy = replacedBy;
    }

    /// <summary>
    /// EF Core parameterless constructor.
    /// </summary>
    public RefreshToken() { }

    /// <summary>
    /// Marks this token as replaced by another token.
    /// </summary>
    /// <param name="replacementId">The ID of the replacement token.</param>
    public void MarkReplaced(string replacementId)
    {
        if (string.IsNullOrWhiteSpace(replacementId))
            throw new ArgumentNullException(nameof(replacementId));

        ReplacedBy = replacementId;
    }

    /// <summary>
    /// Checks whether the token is currently expired.
    /// </summary>
    public bool IsExpired() => DateTime.UtcNow >= Expires;

    /// <summary>
    /// Checks if the token is valid
    /// </summary>
    /// <returns>If the token is valid</returns>
    public bool IsValid() => !IsExpired() && ReplacedBy is not null;
}
