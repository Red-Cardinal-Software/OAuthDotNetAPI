namespace Application.Interfaces.Security;

/// <summary>
/// Provides methods for hashing passwords and verifying their integrity.
/// </summary>
/// <remarks>
/// The <see cref="IPasswordHasher"/> interface defines the contract for password hashing and verification.
/// Implementations of this interface are responsible for securely hashing user passwords and
/// verifying password hashes to ensure integrity and protection against unauthorized access.
/// Examples include BCrypt and Argon2. Recommend using battle-tested libraries.
/// </remarks>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes the provided password using a secure hashing algorithm.
    /// </summary>
    /// <param name="password">The password to be hashed.</param>
    /// <returns>A hashed representation of the provided password.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies whether the provided password matches the hashed password.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="hashedPassword">The hashed representation of the password to compare against.</param>
    /// <returns>True if the password matches the hashed password; otherwise, false.</returns>
    bool Verify(string password, string hashedPassword);
}