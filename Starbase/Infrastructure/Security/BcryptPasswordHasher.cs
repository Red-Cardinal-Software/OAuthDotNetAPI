using Application.Interfaces.Security;

namespace Infrastructure.Security;

/// <summary>
/// Provides functionality for hashing and verifying passwords using the BCrypt algorithm.
/// </summary>
/// <remarks>
/// This class serves as an implementation of the <see cref="IPasswordHasher"/> interface, leveraging the
/// BCrypt library for secure password management. Password hashing and verification are performed
/// via the BCrypt.Net.BCrypt utility methods.
/// </remarks>
public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hashedPassword) => BCrypt.Net.BCrypt.Verify(password, hashedPassword);
}
