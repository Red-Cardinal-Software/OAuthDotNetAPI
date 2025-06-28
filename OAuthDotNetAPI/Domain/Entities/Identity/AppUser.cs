using Domain.Exceptions;

namespace Domain.Entities.Identity;

/// <summary>
/// Represents a user in the system, associated with an organization and assigned one or more roles.
/// </summary>
public class AppUser
{
    /// <summary>
    /// Gets the unique identifier for the user.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the username used for login and identification.
    /// </summary>
    public string Username { get; private set; } = null!;

    /// <summary>
    /// Gets the hashed password for the user.
    /// </summary>
    public HashedPassword Password { get; private set; } = null!;

    /// <summary>
    /// Gets the user's first name.
    /// </summary>
    public string FirstName { get; private set; } = null!;

    /// <summary>
    /// Gets the user's last name.
    /// </summary>
    public string LastName { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether the user must reset their password at the next login.
    /// </summary>
    public bool ForceResetPassword { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user is currently active.
    /// </summary>
    public bool Active { get; private set; } = true;

    /// <summary>
    /// Gets the timestamp of the user's last login, if any.
    /// </summary>
    public DateTime? LastLoginTime { get; private set; }

    /// <summary>
    /// Gets the collection of roles assigned to this user.
    /// </summary>
    public ICollection<Role> Roles { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the user's organization.
    /// </summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>
    /// Gets the organization the user belongs to, if loaded.
    /// </summary>
    public Organization? Organization { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppUser"/> class with required fields.
    /// </summary>
    /// <param name="username">The login username.</param>
    /// <param name="hashedPassword">The already-hashed password string.</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    /// <param name="organizationId">The identifier of the organization the user belongs to.</param>
    /// <param name="forceResetPassword">Optional flag to require password reset at the next login.</param>
    /// <exception cref="InvalidUsernameException">Thrown when the username is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when required fields are null or whitespace.</exception>
    public AppUser(string username, string hashedPassword, string firstName, string lastName, Guid organizationId, bool? forceResetPassword = true)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidUsernameException(username);
        
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentNullException(nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentNullException(nameof(lastName));

        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new ArgumentNullException(nameof(hashedPassword));
        
        Id = Guid.NewGuid();
        Username = username;
        Password = new HashedPassword(hashedPassword);
        FirstName = firstName;
        LastName = lastName;
        OrganizationId = organizationId;
        ForceResetPassword = forceResetPassword ?? true;
        Active = true;
        LastLoginTime = null;
        Roles = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppUser"/> class for use by EF Core.
    /// </summary>
    public AppUser()
    {
        Roles = [];
    }

    /// <summary>
    /// Marks the user as inactive.
    /// </summary>
    public void Deactivate() => Active = false;

    /// <summary>
    /// Marks the user as active.
    /// </summary>
    public void Activate() => Active = true;

    /// <summary>
    /// Changes the user's first name.
    /// </summary>
    /// <param name="newFirstName">The new first name.</param>
    /// <exception cref="ArgumentNullException">Thrown if the name is null or whitespace.</exception>
    public void ChangeFirstName(string newFirstName)
    {
        if (string.IsNullOrWhiteSpace(newFirstName))
            throw new ArgumentNullException(nameof(newFirstName));
        FirstName = newFirstName;
    }

    /// <summary>
    /// Changes the user's last name.
    /// </summary>
    /// <param name="newLastName">The new last name.</param>
    /// <exception cref="ArgumentNullException">Thrown if the name is null or whitespace.</exception>
    public void ChangeLastName(string newLastName)
    {
        if (string.IsNullOrWhiteSpace(newLastName))
            throw new ArgumentNullException(nameof(newLastName));
        LastName = newLastName;
    }

    /// <summary>
    /// Flags the user to be required to reset their password at the next login.
    /// </summary>
    public void ForceResetPasswordAtNextLogin() => ForceResetPassword = true;

    /// <summary>
    /// Changes the user's password to a new hashed value.
    /// </summary>
    /// <param name="newHashedPassword">The new hashed password.</param>
    /// <exception cref="ArgumentNullException">Thrown if the password is null or whitespace.</exception>
    public void ChangePassword(string newHashedPassword)
    {
        if (string.IsNullOrWhiteSpace(newHashedPassword))
            throw new ArgumentNullException(nameof(newHashedPassword));
        Password = new HashedPassword(newHashedPassword);
        ForceResetPassword = false;
    }

    /// <summary>
    /// Records the current time as the user's last login time.
    /// </summary>
    public void LoggedIn() => LastLoginTime = DateTime.UtcNow;

    /// <summary>
    /// Adds a role to the user's role list.
    /// </summary>
    /// <param name="role">The role to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if the role is null.</exception>
    /// <exception cref="DuplicateRoleException">Thrown if the role already exists.</exception>
    public void AddRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        if (Roles.Contains(role))
            throw new DuplicateRoleException(role.Name);
        Roles.Add(role);
    }

    /// <summary>
    /// Removes a role from the user's role list.
    /// </summary>
    /// <param name="role">The role to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown if the role is null.</exception>
    /// <exception cref="InvalidStateTransitionException">Thrown if the role is not assigned to the user.</exception>
    public void RemoveRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        if (!Roles.Contains(role))
            throw new InvalidStateTransitionException("User is not a member of this role.");
        Roles.Remove(role);
    }

    /// <summary>
    /// Assigns the user to a new organization.
    /// </summary>
    /// <param name="newOrg">The organization to assign.</param>
    /// <exception cref="ArgumentNullException">Thrown if the organization is null.</exception>
    public void ChangeOrganization(Organization newOrg)
    {
        ArgumentNullException.ThrowIfNull(newOrg);
        
        if (OrganizationId == newOrg.Id)
            return;
        
        Organization = newOrg;
        OrganizationId = newOrg.Id;
    }
}
