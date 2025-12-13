using Application.Common.Configuration;
using Application.Interfaces.Services;
using Microsoft.Extensions.Options;
using QRCoder;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security;

/// <summary>
/// Implementation of TOTP (Time-based One-Time Password) provider for MFA.
/// Uses RFC 6238 standard with configurable parameters for secure TOTP generation and validation.
/// </summary>
public class TotpProvider(IOptions<AppOptions> appOptions) : ITotpProvider
{
    private readonly string _defaultIssuer = appOptions.Value.AppName;
    private const string ValidChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"; // Base32 alphabet

    /// <summary>
    /// Generates a cryptographically secure Base32-encoded secret for TOTP.
    /// </summary>
    public string GenerateSecret(int length = 20)
    {
        if (length is < 16 or > 32)
            throw new ArgumentException("Secret length must be between 16 and 32 bytes", nameof(length));

        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        return ToBase32(bytes);
    }

    /// <summary>
    /// Generates the otpauth:// URI for QR code generation.
    /// </summary>
    public string GenerateUri(string accountName, string secret, string? issuer = null, int digits = 6, int period = 30)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name cannot be empty", nameof(accountName));

        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret cannot be empty", nameof(secret));

        issuer ??= _defaultIssuer;

        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedAccount = Uri.EscapeDataString(accountName);

        var uri = $"otpauth://totp/{encodedIssuer}:{encodedAccount}?secret={secret}&issuer={encodedIssuer}";

        if (digits != 6)
            uri += $"&digits={digits}";

        if (period != 30)
            uri += $"&period={period}";

        return uri;
    }

    /// <summary>
    /// Generates a QR code image as Base64-encoded PNG.
    /// </summary>
    public async Task<string> GenerateQrCodeAsync(string uri, int size = 200)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("URI cannot be empty", nameof(uri));

        return await Task.Run(() =>
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);

            var qrCodeBytes = qrCode.GetGraphic(size / 25); // Roughly 25 pixels per module
            return Convert.ToBase64String(qrCodeBytes);
        });
    }

    /// <summary>
    /// Validates a TOTP code against a secret.
    /// </summary>
    public bool ValidateCode(string secret, string code, int window = 1, int digits = 6, int period = 30)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return false;

        if (string.IsNullOrWhiteSpace(code) || code.Length != digits)
            return false;

        // Remove any spaces or formatting from the code
        var cleanCode = code.Replace(" ", "").Replace("-", "");

        if (!int.TryParse(cleanCode, out _))
            return false;

        try
        {
            var secretBytes = FromBase32(secret);
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Check the current time window and adjacent windows
            for (int i = -window; i <= window; i++)
            {
                var timeCounter = (currentTime / period) + i;
                var expectedCode = GenerateCodeForTimeCounter(secretBytes, timeCounter, digits);

                // Use constant-time comparison to prevent timing attacks
                var cleanCodeBytes = Encoding.UTF8.GetBytes(cleanCode);
                var expectedCodeBytes = Encoding.UTF8.GetBytes(expectedCode);

                if (CryptographicOperations.FixedTimeEquals(cleanCodeBytes, expectedCodeBytes))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates the current TOTP code for a secret (for testing purposes).
    /// </summary>
    public string GenerateCode(string secret, int digits = 6, int period = 30)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret cannot be empty", nameof(secret));

        var secretBytes = FromBase32(secret);
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timeCounter = currentTime / period;

        return GenerateCodeForTimeCounter(secretBytes, timeCounter, digits);
    }

    /// <summary>
    /// Formats a secret for easier manual entry by users.
    /// </summary>
    public string FormatSecretForDisplay(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return string.Empty;

        // Add space every 4 characters for readability
        var result = new StringBuilder();
        for (int i = 0; i < secret.Length; i++)
        {
            if (i > 0 && i % 4 == 0)
                result.Append(' ');
            result.Append(secret[i]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Gets the time remaining until the current TOTP code expires.
    /// </summary>
    public int GetTimeRemaining(int period = 30)
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return (int)(period - (currentTime % period));
    }

    /// <summary>
    /// Generates a TOTP code for a specific time counter.
    /// </summary>
    private static string GenerateCodeForTimeCounter(byte[] secret, long timeCounter, int digits)
    {
        // Convert counter to the byte array (big-endian)
        var counterBytes = BitConverter.GetBytes(timeCounter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        // HMAC-SHA1
        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);

        // Dynamic truncation
        var offset = hash[^1] & 0x0F;
        var truncatedHash = ((hash[offset] & 0x7F) << 24) |
                           ((hash[offset + 1] & 0xFF) << 16) |
                           ((hash[offset + 2] & 0xFF) << 8) |
                           (hash[offset + 3] & 0xFF);

        // Generate code
        var code = truncatedHash % (int)Math.Pow(10, digits);
        return code.ToString().PadLeft(digits, '0');
    }

    /// <summary>
    /// Converts a byte array to Base32 string.
    /// </summary>
    private static string ToBase32(byte[] bytes)
    {
        if (bytes.Length == 0)
            return string.Empty;

        var result = new StringBuilder();
        int buffer = 0;
        int bitsLeft = 0;

        foreach (byte b in bytes)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;

            while (bitsLeft >= 5)
            {
                int index = (buffer >> (bitsLeft - 5)) & 0x1F;
                result.Append(ValidChars[index]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
        {
            int index = (buffer << (5 - bitsLeft)) & 0x1F;
            result.Append(ValidChars[index]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts a Base32 string to the byte array.
    /// </summary>
    private static byte[] FromBase32(string base32)
    {
        if (string.IsNullOrWhiteSpace(base32))
            throw new ArgumentException("Base32 string cannot be empty", nameof(base32));

        // Remove any whitespace and convert to uppercase
        base32 = base32.Replace(" ", "").ToUpperInvariant();

        var result = new List<byte>();
        int buffer = 0;
        int bitsLeft = 0;

        foreach (char c in base32)
        {
            int index = ValidChars.IndexOf(c);
            if (index < 0)
                throw new ArgumentException($"Invalid Base32 character: {c}", nameof(base32));

            buffer = (buffer << 5) | index;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                result.Add((byte)((buffer >> (bitsLeft - 8)) & 0xFF));
                bitsLeft -= 8;
            }
        }

        return result.ToArray();
    }
}