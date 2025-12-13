using Application.DTOs.Auth;
using Application.DTOs.Organization;

namespace Application.DTOs.Users;

public class AppUserDto
{
    /// <summary>
    /// Gets the unique identifier for the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets the username used for login and identification.
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// Gets the user's first name.
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// Gets the user's last name.
    /// </summary>
    public string LastName { get; set; } = null!;

    /// <summary>
    /// Gets a value indicating whether the user must reset their password at the next login.
    /// </summary>
    public bool ForceResetPassword { get; set; }

    /// <summary>
    /// Gets a value indicating whether the user is currently active.
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Gets the timestamp of the user's last login, if any.
    /// </summary>
    public DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// Gets the collection of roles assigned to this user.
    /// </summary>
    public ICollection<RoleDto> Roles { get; set; } = new List<RoleDto>();

    /// <summary>
    /// Gets the unique identifier of the user's organization.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Gets the organization the user belongs to, if loaded.
    /// </summary>
    public BasicOrganizationDto? Organization { get; set; }
}
