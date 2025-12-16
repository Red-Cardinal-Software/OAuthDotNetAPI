namespace Application.DTOs.Mfa.EmailMfa;

/// <summary>
/// Response DTO for successful email code sending.
/// </summary>
public class EmailCodeSentDto
{
    /// <summary>
    /// Indicates if the email was sent successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// When the code expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Number of attempts remaining to verify the code.
    /// </summary>
    public int AttemptsRemaining { get; set; }

    /// <summary>
    /// Optional message with additional information.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// The email address where the code was sent (masked for security).
    /// </summary>
    public string? MaskedEmail { get; set; }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    public static EmailCodeSentDto Successful(DateTimeOffset expiresAt, int attemptsRemaining, string? maskedEmail)
    {
        return new EmailCodeSentDto
        {
            Success = true,
            ExpiresAt = expiresAt,
            AttemptsRemaining = attemptsRemaining,
            MaskedEmail = maskedEmail,
            Message = "Verification code sent successfully"
        };
    }

    /// <summary>
    /// Creates a failed response.
    /// </summary>
    public static EmailCodeSentDto Failed(string message)
    {
        return new EmailCodeSentDto
        {
            Success = false,
            Message = message
        };
    }
}