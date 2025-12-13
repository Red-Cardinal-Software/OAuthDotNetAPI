using Application.Common.Email;
using Application.Common.Utilities;
using Application.DTOs.Mfa.EmailMfa;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Application.Services.Mfa;

/// <summary>
/// Service for handling email-based MFA authentication flows.
/// Provides high-level operations that combine business logic with email MFA operations.
/// </summary>
public class MfaEmailAuthenticationService(IMfaEmailService emailMfaService, ILogger<MfaEmailAuthenticationService> logger) : IMfaEmailAuthenticationService
{
    /// <summary>
    /// Sends an email MFA verification code to the user's email address.
    /// </summary>
    public async Task<EmailCodeSentDto> SendCodeAsync(ClaimsPrincipal user, SendEmailCodeDto request, string? ipAddress)
    {
        var userId = RoleUtility.GetUserIdFromClaims(user);

        logger.LogInformation("Sending email MFA code for user {UserId}", userId);

        // Use provided email or get from user's claims/profile
        var emailAddress = request.EmailAddress;
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            emailAddress = user.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                logger.LogWarning("No email address provided and no email found in user profile for user {UserId}", userId);
                throw new InvalidOperationException("No email address provided and no email found in user profile");
            }
        }

        var result = await emailMfaService.SendCodeAsync(
            request.ChallengeId,
            userId,
            emailAddress,
            ipAddress);

        if (!result.Success)
        {
            logger.LogWarning("Failed to send email MFA code for user {UserId}: {Error}", userId, result.ErrorMessage);
            throw new InvalidOperationException(result.ErrorMessage);
        }

        logger.LogInformation("Email MFA code sent successfully for user {UserId}", userId);

        // Mask email for security
        var maskedEmail = EmailMaskingUtility.MaskEmail(emailAddress);

        return EmailCodeSentDto.Successful(
            result.ExpiresAt!.Value,
            result.RemainingAttempts,
            maskedEmail);
    }

    /// <summary>
    /// Verifies an email MFA code.
    /// </summary>
    public async Task<EmailCodeVerificationDto> VerifyCodeAsync(VerifyEmailCodeDto request)
    {
        logger.LogInformation("Verifying email MFA code for challenge {ChallengeId}", request.ChallengeId);

        var result = await emailMfaService.VerifyCodeAsync(
            request.ChallengeId,
            request.Code);

        if (!result.Success)
        {
            logger.LogWarning("Failed to verify email MFA code for challenge {ChallengeId}: {Error}", request.ChallengeId, result.ErrorMessage);
            throw new InvalidOperationException(result.ErrorMessage);
        }

        logger.LogInformation("Email MFA code verified successfully for challenge {ChallengeId}", request.ChallengeId);

        return EmailCodeVerificationDto.Successful();
    }

    /// <summary>
    /// Checks the rate limit status for a user.
    /// </summary>
    public async Task<object> CheckRateLimitAsync(ClaimsPrincipal user)
    {
        var userId = RoleUtility.GetUserIdFromClaims(user);

        logger.LogDebug("Checking email MFA rate limit for user {UserId}", userId);

        var rateLimitResult = await emailMfaService.CheckRateLimitAsync(userId);

        return new
        {
            isAllowed = rateLimitResult.IsAllowed,
            codesUsed = rateLimitResult.CodesUsed,
            maxCodes = rateLimitResult.MaxCodesAllowed,
            resetTime = rateLimitResult.WindowResetTime
        };
    }
}
