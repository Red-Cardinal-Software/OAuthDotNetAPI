namespace Domain.Entities.Identity;

/// <summary>
/// Represents a hashed password value object.
/// </summary>
/// <remarks>
/// This class enforces basic validation on hashed password strings and provides an implicit
/// conversion to string for convenience in comparisons and serialization.
/// </remarks>
public class HashedPassword
{
    /// <summary>
    /// Gets the hashed password string value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashedPassword"/> class.
    /// </summary>
    /// <param name="value">The hashed password string.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the hashed password is null, whitespace, or too short to be valid.
    /// </exception>
    public HashedPassword(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 20)
            throw new ArgumentException("Invalid hashed password.");
        Value = value;
    }

    /// <summary>
    /// Implicitly converts a <see cref="HashedPassword"/> instance to a string.
    /// </summary>
    /// <param name="hp">The <see cref="HashedPassword"/> instance.</param>
    public static implicit operator string(HashedPassword hp) => hp.Value;
}
