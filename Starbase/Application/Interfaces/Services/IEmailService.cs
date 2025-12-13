using Application.DTOs.Email;

namespace Application.Interfaces.Services;

/// <summary>
/// Defines the contract for an email service that handles sending emails in the application.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously to the specified recipient using a pre-rendered email object.
    /// </summary>
    /// <param name="to">The email address of the recipient.</param>
    /// <param name="email">An object containing the pre-rendered email subject, body content, and HTML format indicator.</param>
    /// <returns>A task representing the asynchronous operation of sending the email.</returns>
    Task SendEmailAsync(string to, RenderedEmail email);

    /// <summary>
    /// Sends an MFA verification code via email.
    /// </summary>
    /// <param name="to">The recipient's email address</param>
    /// <param name="verificationCode">The 6-digit verification code</param>
    /// <param name="expiresInMinutes">How many minutes until the code expires</param>
    /// <param name="appName">The name of the application</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SendMfaVerificationCodeAsync(string to, string verificationCode, int expiresInMinutes, string appName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an MFA setup verification code via email.
    /// </summary>
    /// <param name="to">The recipient's email address</param>
    /// <param name="verificationCode">The verification code for setup</param>
    /// <param name="expiresInMinutes">How many minutes until the code expires</param>
    /// <param name="appName">The name of the application</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SendMfaSetupVerificationCodeAsync(string to, string verificationCode, int expiresInMinutes, string appName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a security notification email for MFA-related events.
    /// </summary>
    /// <param name="to">The recipient's email address</param>
    /// <param name="eventType">The type of security event (e.g., "MFA Setup", "MFA Disabled")</param>
    /// <param name="eventDetails">Additional details about the event</param>
    /// <param name="timestamp">When the event occurred</param>
    /// <param name="ipAddress">IP address where the event originated</param>
    /// <param name="appName">The name of the application</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SendMfaSecurityNotificationAsync(string to, string eventType, string eventDetails, DateTimeOffset timestamp, string? ipAddress, string appName, CancellationToken cancellationToken = default);
}
