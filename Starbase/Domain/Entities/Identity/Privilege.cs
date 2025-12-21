using Domain.Attributes;

namespace Domain.Entities.Identity;

/// <summary>
/// Represents a system privilege that can be assigned to roles to grant specific access rights.
/// </summary>
[Audited]
public class Privilege : IEquatable<Privilege>
{
    /// <summary>
    /// Gets the unique identifier for the privilege.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the name of the privilege.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// A friendly description to show in UI or audit logs.
    /// </summary>
    public string Description { get; private set; } = null!;

    /// <summary>
    /// Indicates whether the privilege is a system-defined privilege that should not be editable or deletable by organizations
    /// </summary>
    public bool IsSystemDefault { get; private set; }

    /// <summary>
    /// Whether this privilege should be included by default for users with the Admin role.
    /// </summary>
    public bool IsAdminDefault { get; private set; }

    /// <summary>
    /// Whether this privilege should be included by default for users with the basic User role.
    /// </summary>
    public bool IsUserDefault { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Privilege"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the privilege.</param>
    /// <param name="description">User Friendly description of the privilege</param>
    /// <param name="isSystemDefault">System-defined privilege that should not be editable or deletable by organizations</param>
    /// <param name="isAdminDefault">Whether this privilege should be included by default for users with the Admin role.</param>
    /// <param name="isUserDefault">Whether this privilege should be included by default for users with the basic User role.</param>
    /// <exception cref="ArgumentNullException">Thrown when the name is null or whitespace.</exception>
    public Privilege(string name, string description, bool isSystemDefault, bool isAdminDefault, bool isUserDefault)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentNullException(nameof(description));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description;
        IsSystemDefault = isSystemDefault;
        IsAdminDefault = isAdminDefault;
        IsUserDefault = isUserDefault;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Privilege"/> class for EF Core.
    /// </summary>
    public Privilege() { }

    /// <summary>
    /// Renames the privilege.
    /// </summary>
    /// <param name="newName">The new name for the privilege.</param>
    /// <exception cref="ArgumentNullException">Thrown when the new name is null or whitespace.</exception>
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentNullException(nameof(newName));

        Name = newName.Trim();
    }

    /// <inheritdoc />
    public bool Equals(Privilege? other)
    {
        return other is not null && Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Privilege);

    /// <inheritdoc />
    public override int GetHashCode() => Name.ToLowerInvariant().GetHashCode();
}
