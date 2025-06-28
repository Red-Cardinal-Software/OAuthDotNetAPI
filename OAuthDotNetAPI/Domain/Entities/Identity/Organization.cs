using Domain.Exceptions;

namespace Domain.Entities.Identity;

/// <summary>
/// Represents an organization entity with basic information and user membership management.
/// </summary>
public class Organization
{
    /// <summary>
    /// Gets the unique identifier of the organization.
    /// </summary>
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Gets the name of the organization.
    /// </summary>
    public string Name { get; private set; }
    
    /// <summary>
    /// Gets a value indicating whether the organization is currently active.
    /// </summary>
    public bool Active { get; private set; }
    
    /// <summary>
    /// Gets the collection of users associated with this organization.
    /// </summary>
    public ICollection<AppUser> Users { get; private set; }

    public ICollection<Role> Roles { get; private set; }

    
    /// <summary>
    /// Initializes a new instance of the <see cref="Organization"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the organization.</param>
    /// <exception cref="InvalidOrganizationNameException">Thrown when the organization name is null or whitespace.</exception>
    public Organization(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOrganizationNameException(name);

        Id = Guid.NewGuid();
        Name = name;
        Active = true;
        Users = [];
        Roles = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Organization"/> class for use by EF Core.
    /// </summary>
    public Organization()
    {
        Users = [];
        Roles = [];
    }

    /// <summary>
    /// Marks the organization as inactive.
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Thrown when the organization is already inactive.</exception>
    public void Deactivate()
    {
        if (!Active)
            throw new InvalidStateTransitionException("Organization is already deactivated.");
        
        Active = false;
    }

    /// <summary>
    /// Marks the organization as active.
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Thrown when the organization is already active.</exception>
    public void Activate()
    {
        if (Active)
            throw new InvalidStateTransitionException("Organization is already active.");
        
        Active = true;   
    }
    
    /// <summary>
    /// Adds a user to the organization.
    /// </summary>
    /// <param name="user">The user to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when the user is null.</exception>
    /// <exception cref="DuplicateUserException">Thrown when the user already exists in the organization.</exception>
    public void AddUser(AppUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        
        if (Users.Contains(user))
            throw new DuplicateUserException(user.Username);
        
        Users.Add(user);
    }

    /// <summary>
    /// Removes a user from the organization.
    /// </summary>
    /// <param name="user">The user to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown when the user is null.</exception>
    /// <exception cref="InvalidStateTransitionException">Thrown when the user is not part of the organization.</exception>
    public void RemoveUser(AppUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        
        if (!Users.Contains(user))
            throw new InvalidStateTransitionException("User is not a member of this organization.");
        
        Users.Remove(user);
    }
    
    /// <summary>
    /// Renames the organization.
    /// </summary>
    /// <param name="newName">The new name for the organization</param>
    /// <exception cref="InvalidOrganizationNameException">Thrown when the name is blank or null</exception>
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new InvalidOrganizationNameException(newName);

        Name = newName;
    }
}