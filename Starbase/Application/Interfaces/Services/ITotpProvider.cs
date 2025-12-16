namespace Application.Interfaces.Services;

/// <summary>
/// Service interface for Time-based One-Time Password (TOTP) operations.
/// Provides cryptographically secure TOTP generation and validation for MFA.
/// </summary>
public interface ITotpProvider
{
    /// <summary>
    /// Generates a cryptographically secure Base32-encoded secret for TOTP.
    /// </summary>
    /// <param name="length">Length of the secret in bytes (default: 20)</param>
    /// <returns>Base32-encoded secret string</returns>
    string GenerateSecret(int length = 20);

    /// <summary>
    /// Generates the otpauth:// URI for QR code generation.
    /// </summary>
    /// <param name="accountName">The account identifier (username/email)</param>
    /// <param name="secret">The Base32-encoded secret</param>
    /// <param name="issuer">The service/app name</param>
    /// <param name="digits">Number of digits in the code (default: 6)</param>
    /// <param name="period">Validity period in seconds (default: 30)</param>
    /// <returns>Complete otpauth URI</returns>
    string GenerateUri(string accountName, string secret, string? issuer = null, int digits = 6, int period = 30);

    /// <summary>
    /// Generates a QR code image as Base64-encoded PNG.
    /// </summary>
    /// <param name="uri">The otpauth URI</param>
    /// <param name="size">QR code size in pixels (default: 200)</param>
    /// <returns>Base64-encoded PNG image</returns>
    Task<string> GenerateQrCodeAsync(string uri, int size = 200);

    /// <summary>
    /// Validates a TOTP code against a secret.
    /// </summary>
    /// <param name="secret">The Base32-encoded secret</param>
    /// <param name="code">The 6-digit code to validate</param>
    /// <param name="window">Number of time periods to check (default: 1)</param>
    /// <param name="digits">Expected number of digits (default: 6)</param>
    /// <param name="period">Time period in seconds (default: 30)</param>
    /// <returns>True if the code is valid</returns>
    bool ValidateCode(string secret, string code, int window = 1, int digits = 6, int period = 30);

    /// <summary>
    /// Generates the current TOTP code for a secret (for testing purposes).
    /// </summary>
    /// <param name="secret">The Base32-encoded secret</param>
    /// <param name="digits">Number of digits (default: 6)</param>
    /// <param name="period">Time period in seconds (default: 30)</param>
    /// <returns>Current TOTP code</returns>
    string GenerateCode(string secret, int digits = 6, int period = 30);

    /// <summary>
    /// Formats a secret for easier manual entry by users.
    /// </summary>
    /// <param name="secret">The raw secret</param>
    /// <returns>Formatted secret with spaces (e.g., "ABCD EFGH IJKL")</returns>
    string FormatSecretForDisplay(string secret);

    /// <summary>
    /// Gets the time remaining until the current TOTP code expires.
    /// </summary>
    /// <param name="period">TOTP period in seconds (default: 30)</param>
    /// <returns>Seconds remaining until next code</returns>
    int GetTimeRemaining(int period = 30);
}