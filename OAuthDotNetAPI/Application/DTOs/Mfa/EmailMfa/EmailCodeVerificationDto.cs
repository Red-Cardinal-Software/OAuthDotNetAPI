namespace Application.DTOs.Mfa.EmailMfa;

/// <summary>
/// Response DTO for email code verification attempt.
/// </summary>
public class EmailCodeVerificationDto
{
    /// <summary>
    /// Indicates if the verification was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if verification failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Number of attempts remaining if verification failed.
    /// </summary>
    public int? AttemptsRemaining { get; set; }

    /// <summary>
    /// Creates a successful verification response.
    /// </summary>
    public static EmailCodeVerificationDto Successful()
    {
        return new EmailCodeVerificationDto
        {
            Success = true
        };
    }

    /// <summary>
    /// Creates a failed verification response.
    /// </summary>
    public static EmailCodeVerificationDto Failed(string error, int? attemptsRemaining = null)
    {
        return new EmailCodeVerificationDto
        {
            Success = false,
            Error = error,
            AttemptsRemaining = attemptsRemaining
        };
    }
}