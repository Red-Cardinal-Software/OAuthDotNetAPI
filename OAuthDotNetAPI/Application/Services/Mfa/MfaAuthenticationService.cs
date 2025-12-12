using Application.Common.Configuration;
using Application.Common.Constants;
using Application.Common.Services;
using Application.DTOs.Auth;
using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Mfa;

/// <summary>
/// Service implementation for MFA authentication operations during login flow.
/// Handles challenge creation, verification, and security controls for the authentication process.
/// </summary>
public class MfaAuthenticationService(
    IMfaMethodRepository mfaMethodRepository,
    IMfaChallengeRepository mfaChallengeRepository,
    MfaRecoveryCodeService mfaRecoveryCodeService,
    ITotpProvider totpProvider,
    IMfaEmailService mfaEmailService,
    IWebAuthnService webAuthnService,
    IUnitOfWork unitOfWork,
    IOptions<MfaOptions> mfaOptions,
    ILogger<MfaAuthenticationService> logger) 
    : BaseAppService(unitOfWork), IMfaAuthenticationService
{

    #region Challenge Management

    /// <summary>
    /// Creates an MFA challenge for a user during login.
    /// </summary>
    public async Task<MfaChallengeDto> CreateChallengeAsync(Guid userId, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default) => await RunWithCommitAsync(async () =>
    {
        // Check rate limiting
        if (!await CanCreateChallengeAsync(userId, cancellationToken))
        {
            throw new InvalidOperationException("Too many MFA challenges. Please wait before requesting another.");
        }

        // Get user's enabled MFA methods
        var enabledMethods = await mfaMethodRepository.GetEnabledByUserIdAsync(userId, cancellationToken);
        if (enabledMethods.Count == 0)
        {
            throw new InvalidOperationException("User has no enabled MFA methods");
        }

        // Get default method or first enabled method
        var defaultMethod = enabledMethods.FirstOrDefault(m => m.IsDefault) ?? enabledMethods.First();
        
        // Create challenge
        var challenge = MfaChallenge.Create(
            userId, 
            defaultMethod.Type, 
            defaultMethod.Id,
            ipAddress, 
            userAgent);

        await mfaChallengeRepository.AddAsync(challenge, cancellationToken);

        // If the default method is email, send the email code immediately
        if (defaultMethod.Type == MfaType.Email)
        {
            await SendEmailCodeForChallengeAsync(challenge.Id, userId, defaultMethod, ipAddress, cancellationToken);
        }

        // Map available methods to DTOs
        var availableMethods = enabledMethods.Select(MapToAvailableMethodDto).ToArray();

        logger.LogInformation("MFA challenge created for user {UserId}, challenge {ChallengeId}", 
            userId, challenge.Id);

        return new MfaChallengeDto
        {
            ChallengeToken = challenge.ChallengeToken,
            AvailableMethods = availableMethods,
            ExpiresAt = challenge.ExpiresAt,
            AttemptsRemaining = challenge.GetRemainingAttempts(),
            Instructions = GetInstructionsForMfaType(defaultMethod.Type)
        };
    });

    /// <summary>
    /// Verifies an MFA challenge with the provided code.
    /// </summary>
    public async Task<MfaVerificationResult> VerifyMfaAsync(CompleteMfaDto completeMfaDto, CancellationToken cancellationToken = default) => await RunWithCommitAsync(async () =>
    {
        // Get and validate challenge
        var challenge = await mfaChallengeRepository.GetByChallengeTokenAsync(completeMfaDto.ChallengeToken, cancellationToken);
        if (challenge == null)
        {
            return MfaVerificationResult.Failure("Invalid or expired challenge token");
        }

        if (!challenge.IsValid())
        {
            return MfaVerificationResult.Failure("Challenge has expired or been exhausted");
        }

        // Record attempt
        var canContinue = challenge.RecordFailedAttempt(); // Optimistically record as failed, will update if successful

        if (!canContinue)
        {
            logger.LogWarning("MFA challenge {ChallengeId} exhausted for user {UserId}", 
                challenge.Id, challenge.UserId);
            
            return MfaVerificationResult.Failure("Maximum verification attempts exceeded", 0, true);
        }

        // Determine which MFA method to use
        var methodToUse = completeMfaDto.MfaMethodId.HasValue
            ? await mfaMethodRepository.GetByIdAsync(completeMfaDto.MfaMethodId.Value, cancellationToken)
            : challenge.MfaMethodId.HasValue
                ? await mfaMethodRepository.GetByIdAsync(challenge.MfaMethodId.Value, cancellationToken)
                : await mfaMethodRepository.GetDefaultByUserIdAsync(challenge.UserId, cancellationToken);

        if (methodToUse == null || methodToUse.UserId != challenge.UserId || !methodToUse.IsEnabled)
        {
            return MfaVerificationResult.Failure("Invalid MFA method", challenge.GetRemainingAttempts());
        }

        // Verify the code
        var verificationResult = await VerifyCodeForMethod(methodToUse, completeMfaDto.Code, completeMfaDto.IsRecoveryCode, cancellationToken);
        
        if (verificationResult.IsValid)
        {
            // Mark challenge as completed
            challenge.Complete();

            // Record method usage
            methodToUse.RecordUsage();

            // Invalidate other challenges for this user
            await InvalidateUserChallengesAsync(challenge.UserId, cancellationToken);

            logger.LogInformation("MFA verification successful for user {UserId}, method {MethodId}", 
                challenge.UserId, methodToUse.Id);

            return MfaVerificationResult.Success(
                challenge.UserId, 
                methodToUse.Id, 
                completeMfaDto.IsRecoveryCode);
        }

        logger.LogWarning("MFA verification failed for user {UserId}, method {MethodId}", 
            challenge.UserId, methodToUse.Id);

        return MfaVerificationResult.Failure(
            verificationResult.ErrorMessage ?? "Invalid verification code", 
            challenge.GetRemainingAttempts());
    });

    /// <summary>
    /// Invalidates all active challenges for a user.
    /// </summary>
    public async Task<int> InvalidateUserChallengesAsync(Guid userId, CancellationToken cancellationToken = default) => await RunWithCommitAsync(async () =>
    {
        var invalidatedCount = await mfaChallengeRepository.InvalidateAllUserChallengesAsync(userId, cancellationToken);
        
        if (invalidatedCount > 0)
        {
            logger.LogInformation("Invalidated {Count} MFA challenges for user {UserId}", 
                invalidatedCount, userId);
        }

        return invalidatedCount;
    });

    #endregion

    #region Validation

    /// <summary>
    /// Checks if a user requires MFA for authentication.
    /// </summary>
    public async Task<bool> RequiresMfaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await mfaMethodRepository.UserHasEnabledMfaAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Gets the default MFA method for a user.
    /// </summary>
    public async Task<MfaMethod?> GetDefaultMfaMethodAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await mfaMethodRepository.GetDefaultByUserIdAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Validates that an MFA challenge is still active and usable.
    /// </summary>
    public async Task<bool> IsChallengeValidAsync(string challengeToken, CancellationToken cancellationToken = default)
    {
        var challenge = await mfaChallengeRepository.GetByChallengeTokenAsync(challengeToken, cancellationToken);
        return challenge?.IsValid() == true;
    }

    #endregion

    #region Rate Limiting

    /// <summary>
    /// Checks if a user can create new MFA challenges based on rate limiting.
    /// </summary>
    public async Task<bool> CanCreateChallengeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Check active challenge count
        var activeChallenges = await GetActiveChallengeCountAsync(userId, cancellationToken);
        var maxActiveChallenges = GetMfaConfiguration().MaxActiveChallenges;
        
        if (activeChallenges >= maxActiveChallenges)
        {
            return false;
        }

        // Check recent challenge creation rate
        var rateLimitWindow = TimeSpan.FromMinutes(GetMfaConfiguration().RateLimitWindowMinutes);
        var recentChallenges = await mfaChallengeRepository.GetChallengeCountSinceAsync(
            userId, 
            DateTimeOffset.UtcNow.Subtract(rateLimitWindow), 
            cancellationToken);
        
        return recentChallenges < GetMfaConfiguration().MaxChallengesPerWindow;
    }

    /// <summary>
    /// Gets the number of active challenges for a user.
    /// </summary>
    public async Task<int> GetActiveChallengeCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await mfaChallengeRepository.GetActiveChallengeCountAsync(userId, cancellationToken);
    }

    #endregion

    #region Administrative

    /// <summary>
    /// Cleans up expired MFA challenges.
    /// </summary>
    public async Task<int> CleanupExpiredChallengesAsync(DateTimeOffset? expiredBefore = null, CancellationToken cancellationToken = default) => await RunWithCommitAsync(async () =>
    {
        var cutoffTime = expiredBefore ?? DateTimeOffset.UtcNow.AddHours(-1); // Default cleanup after 1 hour
        var cleanedUpCount = await mfaChallengeRepository.DeleteExpiredChallengesAsync(cutoffTime, cancellationToken);
        
        if (cleanedUpCount > 0)
        {
            logger.LogInformation("Cleaned up {Count} expired MFA challenges", cleanedUpCount);
        }

        return cleanedUpCount;
    });

    #endregion

    #region Private Methods

    /// <summary>
    /// Verifies a code against a specific MFA method.
    /// </summary>
    private async Task<MfaVerificationResult> VerifyCodeForMethod(MfaMethod method, string code, bool isRecoveryCode, CancellationToken cancellationToken)
    {
        if (isRecoveryCode)
        {
            // Get unused recovery codes for this method
            var unusedCodes = method.GetUnusedRecoveryCodes();
            
            // Try to validate the code against each unused recovery code
            foreach (var recoveryCode in unusedCodes)
            {
                if (mfaRecoveryCodeService.ValidateAndUseRecoveryCode(recoveryCode, code))
                {
                    return MfaVerificationResult.Success(method.UserId, method.Id, true);
                }
            }
            
            return MfaVerificationResult.Failure("Invalid recovery code");
        }

        // Verify based on method type
        return method.Type switch
        {
            MfaType.Totp => VerifyTotpCode(method, code),
            MfaType.Email => await VerifyEmailCode(method, code, cancellationToken),
            MfaType.WebAuthn => await VerifyWebAuthnAssertion(method, code, cancellationToken),
            _ => MfaVerificationResult.Failure("Unsupported MFA method")
        };
    }

    /// <summary>
    /// Verifies a TOTP code.
    /// </summary>
    private MfaVerificationResult VerifyTotpCode(MfaMethod method, string code)
    {
        if (string.IsNullOrWhiteSpace(method.Secret))
        {
            return MfaVerificationResult.Failure("Method configuration error");
        }

        var isValid = totpProvider.ValidateCode(method.Secret, code);
        return isValid 
            ? MfaVerificationResult.Success(method.UserId, method.Id)
            : MfaVerificationResult.Failure("Invalid authenticator code");
    }

    /// <summary>
    /// Verifies an email code by delegating to the email MFA service.
    /// </summary>
    private async Task<MfaVerificationResult> VerifyEmailCode(MfaMethod method, string code, CancellationToken cancellationToken)
    {
        // Get active challenges and find one for this method type
        var activeChallenges = await mfaChallengeRepository.GetActiveByUserIdAsync(method.UserId, cancellationToken);
        var emailChallenge = activeChallenges.FirstOrDefault(c => c.Type == MfaType.Email);
        
        if (emailChallenge == null)
        {
            return MfaVerificationResult.Failure("No active email challenge found");
        }

        // Verify the email code using the email service
        var verificationResult = await mfaEmailService.VerifyCodeAsync(emailChallenge.Id, code, cancellationToken);
        
        return verificationResult.Success 
            ? MfaVerificationResult.Success(method.UserId, method.Id)
            : MfaVerificationResult.Failure(verificationResult.ErrorMessage ?? "Invalid email verification code", verificationResult.RemainingAttempts);
    }

    /// <summary>
    /// Verifies a WebAuthn assertion by delegating to the WebAuthn service.
    /// </summary>
    private async Task<MfaVerificationResult> VerifyWebAuthnAssertion(MfaMethod method, string assertionData, CancellationToken cancellationToken)
    {
        try
        {
            // Parse the assertion data (in real implementation, this would come from the client)
            // For now, we'll assume the assertionData contains the necessary information
            
            // Get active challenges and find one for WebAuthn
            var activeChallenges = await mfaChallengeRepository.GetActiveByUserIdAsync(method.UserId, cancellationToken);
            var webAuthnChallenge = activeChallenges.FirstOrDefault(c => c.Type == MfaType.WebAuthn);
            
            if (webAuthnChallenge == null)
            {
                return MfaVerificationResult.Failure("No active WebAuthn challenge found");
            }

            // In a real implementation, you would extract these from the client's assertion response
            // For demo purposes, we'll simulate the verification
            var simulatedCredentialId = "simulated-credential-id";
            var simulatedAssertionResponse = new WebAuthnAssertionResponse
            {
                Id = simulatedCredentialId,
                RawId = simulatedCredentialId,
                Response = new WebAuthnAuthenticatorAssertionResponse
                {
                    AuthenticatorData = "simulated-authenticator-data",
                    ClientDataJSON = "simulated-client-data",
                    Signature = "simulated-signature"
                }
            };

            // Use the WebAuthn service to verify the assertion
            var verificationResult = await webAuthnService.CompleteAuthenticationAsync(
                simulatedCredentialId,
                webAuthnChallenge.ChallengeToken,
                simulatedAssertionResponse,
                cancellationToken);

            return verificationResult.Success 
                ? MfaVerificationResult.Success(method.UserId, method.Id)
                : MfaVerificationResult.Failure(verificationResult.ErrorMessage ?? "Invalid WebAuthn assertion");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying WebAuthn assertion for method {MethodId}", method.Id);
            return MfaVerificationResult.Failure("WebAuthn verification failed");
        }
    }

    /// <summary>
    /// Maps an MFA method to an available method DTO.
    /// </summary>
    private static AvailableMfaMethodDto MapToAvailableMethodDto(MfaMethod method)
    {
        return new AvailableMfaMethodDto
        {
            Id = method.Id,
            Type = method.Type,
            Name = method.Name ?? "Unknown Method",
            IsDefault = method.IsDefault,
            DisplayInfo = GetDisplayInfoForMethod(method),
            Instructions = GetInstructionsForMfaType(method.Type)
        };
    }

    /// <summary>
    /// Gets display information for an MFA method.
    /// </summary>
    private static string GetDisplayInfoForMethod(MfaMethod method)
    {
        return method.Type switch
        {
            MfaType.Totp => "Authenticator App",
            MfaType.Email => "Email Verification", // Could extract email from metadata
            MfaType.WebAuthn => "Security Key", // Could extract device name from metadata
            MfaType.Push => "Push Notification", // Could extract device name from metadata
            _ => method.Type.ToString()
        };
    }

    /// <summary>
    /// Gets instructions for using a specific MFA type.
    /// </summary>
    private static string GetInstructionsForMfaType(MfaType mfaType)
    {
        return mfaType switch
        {
            MfaType.Totp => "Enter the 6-digit code from your authenticator app",
            MfaType.Email => "Check your email for a verification code",
            MfaType.WebAuthn => "Use your security key or device biometric",
            MfaType.Push => "Approve the notification on your device",
            _ => "Enter your verification code"
        };
    }

    /// <summary>
    /// Gets MFA configuration from appsettings.
    /// </summary>
    private MfaAuthenticationConfiguration GetMfaConfiguration()
    {
        var options = mfaOptions.Value;
        return new MfaAuthenticationConfiguration
        {
            MaxActiveChallenges = options.MaxActiveChallenges,
            MaxChallengesPerWindow = options.MaxChallengesPerWindow,
            RateLimitWindowMinutes = options.RateLimitWindowMinutes,
            ChallengeExpiryMinutes = options.ChallengeExpiryMinutes
        };
    }

    /// <summary>
    /// Sends an email verification code for a challenge.
    /// </summary>
    private async Task SendEmailCodeForChallengeAsync(Guid challengeId, Guid userId, MfaMethod emailMethod, string? ipAddress, CancellationToken cancellationToken)
    {
        // Extract email address from method metadata or user info
        var emailAddress = ExtractEmailFromMfaMethod(emailMethod);
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            logger.LogError("Cannot send email MFA code - no email address found for method {MethodId}", emailMethod.Id);
            return;
        }

        // Send the email code
        var result = await mfaEmailService.SendCodeAsync(challengeId, userId, emailAddress, ipAddress, cancellationToken);
        if (!result.Success)
        {
            logger.LogWarning("Failed to send email MFA code for challenge {ChallengeId}: {Error}", 
                challengeId, result.ErrorMessage);
        }
        else
        {
            logger.LogInformation("Email MFA code sent for challenge {ChallengeId}", challengeId);
        }
    }

    /// <summary>
    /// Extracts email address from MFA method metadata or returns fallback.
    /// </summary>
    private static string? ExtractEmailFromMfaMethod(MfaMethod method)
    {
        // For now, assume email is stored in the method's metadata or secret field
        // In a real implementation, you might want to:
        // 1. Store email in a separate field in MfaMethod
        // 2. Extract from JSON metadata
        // 3. Look up user email from user repository
        return method.Metadata; // Placeholder - should contain email address
    }

    /// <summary>
    /// Configuration class for MFA authentication settings.
    /// </summary>
    private class MfaAuthenticationConfiguration
    {
        public int MaxActiveChallenges { get; init; }
        public int MaxChallengesPerWindow { get; init; }
        public int RateLimitWindowMinutes { get; init; }
        public int ChallengeExpiryMinutes { get; init; }
    }

    #endregion
}
