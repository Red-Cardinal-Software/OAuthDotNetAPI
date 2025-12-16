using Application.Common.Constants;
using Application.Common.Factories;
using Application.Interfaces.Repositories;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Application.Logging;
using Application.Models;
using Domain.Entities.Identity;
using FluentValidation;

namespace Application.Services.PasswordReset;

public class PasswordResetService(
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IPasswordHasher passwordHasher,
    IAppUserRepository appUserRepository,
    LogContextHelper<PasswordResetService> logger,
    IValidator<string> passwordValidator)
    : IPasswordResetService
{
    public async Task<ServiceResponse<bool>> ResetPasswordWithTokenAsync(string token, string password, string claimedByIpAddress)
    {
        var parsedTokenId = TryParseToken(token);
        if (parsedTokenId is null)
        {
            logger.Critical(new StructuredLogBuilder()
                .SetAction(PasswordResetActions.ResetPassword)
                .SetStatus(LogStatuses.Failure)
                .SetEntity(nameof(PasswordResetToken))
                .SetDetail(ServiceResponseConstants.TokenNotFound));
            return ServiceResponseFactory.Error<bool>(ServiceResponseConstants.TokenNotFound);
        }

        var tokenEntity = await passwordResetTokenRepository.GetPasswordResetTokenAsync(parsedTokenId.Value);
        if (tokenEntity is null)
        {
            logger.Critical(new StructuredLogBuilder()
                .SetAction(PasswordResetActions.ResetPassword)
                .SetStatus(LogStatuses.Failure)
                .SetEntity(nameof(PasswordResetToken))
                .SetDetail(ServiceResponseConstants.InvalidPasswordResetToken));
            return ServiceResponseFactory.Error<bool>(ServiceResponseConstants.InvalidPasswordResetToken);
        }

        if (tokenEntity.IsClaimed())
        {
            logger.Critical(new StructuredLogBuilder()
                .SetType(LogTypes.Security.Threat)
                .SetAction(PasswordResetActions.ResetPassword)
                .SetStatus(LogStatuses.Failure)
                .SetEntity(nameof(PasswordResetToken))
                .SetDetail(ServiceResponseConstants.EmailPasswordResetSent));
            return ServiceResponseFactory.Error<bool>(ServiceResponseConstants.InvalidPasswordResetToken);
        }

        var validationResult = await passwordValidator.ValidateAsync(password);
        if (!validationResult.IsValid)
        {
            logger.Warning(new StructuredLogBuilder()
                .SetAction(PasswordResetActions.ResetPassword)
                .SetStatus(LogStatuses.Failure)
                .SetEntity(nameof(PasswordResetToken))
                .SetDetail(validationResult.Errors.First().ErrorMessage));
            return ServiceResponseFactory.Error<bool>(validationResult.Errors.First().ErrorMessage);
        }

        tokenEntity.Claim(passwordHasher.Hash(password), claimedByIpAddress);
        var unclaimedTokens = await passwordResetTokenRepository.GetAllUnclaimedResetTokensForUserAsync(tokenEntity.AppUserId);

        foreach (var unclaimedToken in unclaimedTokens)
        {
            unclaimedToken.ClaimRedundantToken(claimedByIpAddress);
        }

        logger.Info(new StructuredLogBuilder()
            .SetAction(PasswordResetActions.ResetPassword)
            .SetStatus(LogStatuses.Success)
            .SetEntity(nameof(PasswordResetToken)));

        return ServiceResponseFactory.Success(true);
    }

    public async Task<ServiceResponse<bool>> ForcePasswordResetAsync(Guid userId, string newPassword)
    {
        var thisUser = await appUserRepository.GetUserByIdAsync(userId);

        if (thisUser is null)
        {
            logger.Critical(new StructuredLogBuilder()
                .SetType(LogTypes.Security.Threat)
                .SetAction(PasswordResetActions.ResetPassword)
                .SetStatus(LogStatuses.Failure)
                .SetEntity(nameof(PasswordResetToken))
                .SetDetail(ServiceResponseConstants.UserNotFound));
            return ServiceResponseFactory.Error<bool>(ServiceResponseConstants.UserNotFound);
        }

        if (!thisUser.ForceResetPassword)
        {
            logger.Critical(new StructuredLogBuilder()
                .SetType(LogTypes.Security.Threat)
                .SetAction(PasswordResetActions.ResetPassword)
                .SetStatus(LogStatuses.Failure)
                .SetEntity(nameof(PasswordResetToken))
                .SetDetail(ServiceResponseConstants.UserNotRequiredToResetPassword));
            return ServiceResponseFactory.Error<bool>(ServiceResponseConstants.UserUnauthorized);
        }

        var validationResult = await passwordValidator.ValidateAsync(newPassword);

        if (!validationResult.IsValid)
        {
            logger.Warning(new StructuredLogBuilder()
                .SetAction(PasswordResetActions.ResetPassword)
                .SetStatus(LogStatuses.Failure)
                .SetEntity(nameof(PasswordResetToken))
                .SetDetail(validationResult.Errors.First().ErrorMessage));
            return ServiceResponseFactory.Error<bool>(validationResult.Errors.First().ErrorMessage);
        }

        if (passwordHasher.Verify(newPassword, thisUser.Password))
        {
            logger.Warning(new StructuredLogBuilder()
                .SetAction(PasswordResetActions.ResetPassword)
                .SetStatus(LogStatuses.Failure)
                .SetEntity(nameof(PasswordResetToken))
                .SetDetail(ServiceResponseConstants.PasswordMustBeDifferentFromCurrent));
            return ServiceResponseFactory.Error<bool>(ServiceResponseConstants.PasswordMustBeDifferentFromCurrent);
        }

        var hashedPassword = new HashedPassword(passwordHasher.Hash(newPassword));

        thisUser.ChangePassword(hashedPassword);

        logger.Info(new StructuredLogBuilder()
            .SetAction(PasswordResetActions.ResetPassword)
            .SetStatus(LogStatuses.Success)
            .SetEntity(nameof(PasswordResetToken))
        );

        return ServiceResponseFactory.Success(true);
    }

    /// <summary>
    /// Attempts to parse the provided token into a Guid.
    /// </summary>
    /// <param name="token">The token string to parse.</param>
    /// <returns>The parsed Guid if the token is valid; otherwise, null.</returns>
    private static Guid? TryParseToken(string token) => Guid.TryParse(token, out var guid) ? guid : null;
}
