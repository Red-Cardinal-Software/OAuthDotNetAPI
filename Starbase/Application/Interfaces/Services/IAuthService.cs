using System.Security.Claims;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.Models;

namespace Application.Interfaces.Services;

/// <summary>
/// Represents the contract for authentication-related services.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Performs the login process for a user by validating credentials and generating authentication tokens.
    /// </summary>
    /// <param name="username">The username provided by the user attempting to log in.</param>
    /// <param name="password">The password provided by the user attempting to log in.</param>
    /// <param name="ipAddress">The IP address of the device making the login request.</param>
    /// <returns>A service response containing a <see cref="JwtResponseDto"/> object with an access token and a refresh token.</returns>
    public Task<ServiceResponse<JwtResponseDto>> Login(string username, string password, string ipAddress);

    /// <summary>
    /// Retrieves a new JWT token and refresh token for a user using the provided refresh token.
    /// </summary>
    /// <param name="username">The username of the user requesting a token refresh.</param>
    /// <param name="token">The refresh token used to validate the user's session.</param>
    /// <param name="ipAddress">The IP address of the device making the token refresh request.</param>
    /// <returns>A service response containing a new <see cref="JwtResponseDto"/> object with an updated access token and refresh token.</returns>
    public Task<ServiceResponse<JwtResponseDto>> Refresh(string username, string token, string ipAddress);

    /// <summary>
    /// Performs the logout process by invalidating the refresh token associated with the specified user.
    /// </summary>
    /// <param name="username">The username of the user requesting to log out.</param>
    /// <param name="refreshToken">The refresh token to be invalidated during the logout process.</param>
    /// <returns>A service response indicating whether the logout process was successfully completed.</returns>
    public Task<ServiceResponse<bool>> Logout(string username, string refreshToken);

    /// <summary>
    /// Initiates the password reset workflow by generating a password reset email for the specified user.
    /// This method ensures enhanced security by always returning a successful response regardless of email existence.
    /// </summary>
    /// <param name="email">The email address of the user requesting the password reset.</param>
    /// <param name="ipAddress">The IP address from which the password reset request originates.</param>
    /// <returns>A service response containing a boolean value indicating whether the operation was processed successfully.</returns>
    public Task<ServiceResponse<bool>> RequestPasswordReset(string email, string ipAddress);

    /// <summary>
    /// Applies a password reset request using the provided token and new password.
    /// This method validates the reset token and updates the user's password if valid.
    /// </summary>
    /// <param name="token">An object containing the reset token and the new password to be applied.</param>
    /// <param name="ipAddress">The IP address of the device initiating the password reset request.</param>
    /// <returns>A service response indicating whether the password reset was successful.</returns>
    public Task<ServiceResponse<bool>> ApplyPasswordReset(PasswordResetSubmissionDto token, string ipAddress);

    /// <summary>
    /// Forces a user's password to be reset by updating it with a new password provided during the process.
    /// </summary>
    /// <param name="user">The claims principal representing the authenticated user requesting the password reset.</param>
    /// <param name="newPassword">The new password to replace the old one for the user.</param>
    /// <returns>A service response indicating whether the password reset operation was successful or not.</returns>
    public Task<ServiceResponse<bool>> ForcePasswordReset(ClaimsPrincipal user, string newPassword);

    /// <summary>
    /// Generates a new JWT token for a specified user, typically used when token regeneration is required after certain operations or settings changes.
    /// </summary>
    /// <param name="user">The claims principal representing the user for whom the token is being generated.</param>
    /// <returns>An instance of <see cref="JwtResponseDto"/> containing the generated JWT token and a new refresh token.</returns>
    public Task<JwtResponseDto> GenerateJwtToken(ClaimsPrincipal user);

    /// <summary>
    /// Completes the MFA verification process and issues authentication tokens.
    /// </summary>
    /// <param name="completeMfaDto">The MFA completion information including challenge token and verification code.</param>
    /// <param name="ipAddress">The IP address of the device completing MFA verification.</param>
    /// <returns>A service response containing JWT tokens upon successful MFA verification.</returns>
    public Task<ServiceResponse<JwtResponseDto>> CompleteMfaAuthentication(CompleteMfaDto completeMfaDto, string ipAddress);
}
