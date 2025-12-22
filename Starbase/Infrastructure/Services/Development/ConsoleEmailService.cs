using Application.DTOs.Email;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Development;

/// <summary>
/// Development email service that logs emails to the console instead of sending them.
/// Useful for local development and testing without requiring an email provider.
/// </summary>
/// <remarks>
/// This service is intended for development use only. In production, replace with
/// a real email provider implementation (SendGrid, Postmark, AWS SES, etc.).
///
/// All "sent" emails are logged with their full content, making it easy to:
/// - See verification codes during MFA testing
/// - Debug email templates
/// - Test email flows without external dependencies
/// </remarks>
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, RenderedEmail email)
    {
        LogEmail(to, email.Subject, email.Body, "General Email");
        return Task.CompletedTask;
    }

    public Task SendMfaVerificationCodeAsync(
        string to,
        string verificationCode,
        int expiresInMinutes,
        string appName,
        CancellationToken cancellationToken = default)
    {
        var subject = $"[{appName}] Your verification code";
        var body = $"""
            Your MFA verification code is: {verificationCode}

            This code will expire in {expiresInMinutes} minutes.

            If you did not request this code, please ignore this email.
            """;

        LogEmail(to, subject, body, "MFA Verification Code", new { Code = verificationCode, ExpiresInMinutes = expiresInMinutes });
        return Task.CompletedTask;
    }

    public Task SendMfaSetupVerificationCodeAsync(
        string to,
        string verificationCode,
        int expiresInMinutes,
        string appName,
        CancellationToken cancellationToken = default)
    {
        var subject = $"[{appName}] Verify your email for MFA setup";
        var body = $"""
            Your MFA setup verification code is: {verificationCode}

            This code will expire in {expiresInMinutes} minutes.

            If you did not initiate MFA setup, please secure your account immediately.
            """;

        LogEmail(to, subject, body, "MFA Setup Code", new { Code = verificationCode, ExpiresInMinutes = expiresInMinutes });
        return Task.CompletedTask;
    }

    public Task SendMfaSecurityNotificationAsync(
        string to,
        string eventType,
        string eventDetails,
        DateTimeOffset timestamp,
        string? ipAddress,
        string appName,
        CancellationToken cancellationToken = default)
    {
        var subject = $"[{appName}] Security Alert: {eventType}";
        var body = $"""
            Security Event: {eventType}

            Details: {eventDetails}
            Time: {timestamp:yyyy-MM-dd HH:mm:ss} UTC
            IP Address: {ipAddress ?? "Unknown"}

            If you did not perform this action, please secure your account immediately.
            """;

        LogEmail(to, subject, body, "Security Notification", new { EventType = eventType, IpAddress = ipAddress });
        return Task.CompletedTask;
    }

    private void LogEmail(string to, string subject, string body, string emailType, object? metadata = null)
    {
        _logger.LogInformation(
            """

            ╔══════════════════════════════════════════════════════════════════╗
            ║  DEVELOPMENT EMAIL - Not actually sent                           ║
            ╠══════════════════════════════════════════════════════════════════╣
            ║  Type: {EmailType,-56} ║
            ║  To: {To,-58} ║
            ║  Subject: {Subject,-52} ║
            ╠══════════════════════════════════════════════════════════════════╣
            {Body}
            ╚══════════════════════════════════════════════════════════════════╝

            """,
            emailType,
            to,
            subject.Length > 52 ? subject[..49] + "..." : subject,
            FormatBody(body));

        if (metadata != null)
        {
            _logger.LogDebug("Email metadata: {@Metadata}", metadata);
        }
    }

    private static string FormatBody(string body)
    {
        var lines = body.Split('\n');
        var formatted = lines.Select(line =>
        {
            var trimmed = line.TrimEnd();
            if (trimmed.Length > 66)
                trimmed = trimmed[..63] + "...";
            return $"║  {trimmed,-66} ║";
        });
        return string.Join("\n", formatted);
    }
}