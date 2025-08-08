using Domain.Exceptions;

namespace Domain.Entities.Identity;

/// <summary>
/// Represents a role that can be assigned to users and associated with specific privileges.
/// </summary>
public class Role : IEquatable<Role>
{
    /// <summary>
    /// Gets the unique identifier for the role.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the name of the role.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// The ID of the Organization that carries this role
    /// </summary>
    public Guid? OrganizationId { get; private set; }

    /// <summary>
    /// The Organization that carries this role
    /// </summary>
    public Organization? Organization { get; private set; }

    /// <summary>
    /// Gets the collection of users assigned to this role.
    /// </summary>
    public ICollection<AppUser> Users { get; private set; }

    /// <summary>
    /// Gets the collection of privileges associated with this role.
    /// </summary>
    public ICollection<Privilege> Privileges { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the role.</param>
    /// <param name="organizationId">The organization this role is being added to</param>
    /// <exception cref="ArgumentNullException">Thrown when the name is null or whitespace.</exception>
    public Role(string name, Guid? organizationId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        Id = Guid.NewGuid();
        Name = name.Trim();
        OrganizationId = organizationId;
        Users = [];
        Privileges = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class for use by EF Core.
    /// </summary>
    public Role()
    {
        Users = [];
        Privileges = [];
    }

    /// <summary>
    /// Adds a privilege to the role.
    /// </summary>
    /// <param name="privilege">The privilege to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when the privilege is null.</exception>
    /// <exception cref="DuplicatePrivilegeException">Thrown when the privilege already exists in the role.</exception>
    public void AddPrivilege(Privilege privilege)
    {
        ArgumentNullException.ThrowIfNull(privilege);

        if (Privileges.Contains(privilege))
            throw new DuplicatePrivilegeException(privilege.Name);

        Privileges.Add(privilege);
    }

    /// <summary>
    /// Removes a privilege from the role.
    /// </summary>
    /// <param name="privilege">The privilege to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown when the privilege is null.</exception>
    /// <exception cref="InvalidStateTransitionException">Thrown when the privilege does not exist in the role.</exception>
    public void RemovePrivilege(Privilege privilege)
    {
        ArgumentNullException.ThrowIfNull(privilege);

        if (!Privileges.Contains(privilege))
            throw new InvalidStateTransitionException("Privilege not set on role.");

        Privileges.Remove(privilege);
    }

    /// <summary>
    /// Renames a role
    /// </summary>
    /// <param name="newName">New name for the role</param>
    /// <exception cref="ArgumentNullException">Thrown when the new name is null or whitespace</exception>
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentNullException(nameof(newName));
        Name = newName.Trim();
    }


    /// <summary>
    /// Checks if the role is equal to another role.
    /// </summary>
    /// <param name="other">The other Role to compare to.</param>
    /// <returns>Whether the object is equal</returns>
    public bool Equals(Role? other)
    {
        return other is not null && Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the role is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare to</param>
    /// <returns>Whether the object is equal</returns>
    public override bool Equals(object? obj) => Equals(obj as Role);

    /// <summary>
    /// Overrides the default GetHashCode implementation.
    /// </summary>
    /// <returns>Hashcode of the role</returns>
    public override int GetHashCode() => Name.ToLowerInvariant().GetHashCode();

}
