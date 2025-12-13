using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security;

public static class PasswordHashUtility
{
    /// <summary>
    /// Converts a given password into a normalized SHA-256 hash string.
    /// </summary>
    /// <param name="password">The password to be hashed. The password is trimmed and converted to lowercase before hashing.</param>
    /// <returns>A hexadecimal string that represents the SHA-256 hash of the normalized password.</returns>
    public static string HashCommonPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password.Trim().ToLowerInvariant());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
