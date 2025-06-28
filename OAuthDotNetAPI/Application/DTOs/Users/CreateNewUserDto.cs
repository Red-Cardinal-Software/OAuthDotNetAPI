using Application.DTOs.Auth;

namespace Application.DTOs.Users;

public class CreateNewUserDto
{
    /// <summary>
    /// Gets the username used for login and identification.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// The password of the new user.
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// Gets the user's first name.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// Gets the user's last name.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Gets the collection of roles assigned to this user.
    /// </summary>
    public ICollection<RoleDto> Roles { get; set; } = [];

    /// <summary>
    /// Gets the unique identifier of the user's organization.
    /// </summary>
    public Guid OrganizationId { get; private set; }
}