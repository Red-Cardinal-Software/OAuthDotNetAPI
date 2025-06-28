using Application.DTOs.Auth;

namespace Application.DTOs.Users;

public class CreateNewUserDto
{
    /// <summary>
    /// Gets the username used for login and identification.
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// The password of the new user.
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// Gets the user's first name.
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// Gets the user's last name.
    /// </summary>
    public string LastName { get; set; } = null!;

    /// <summary>
    /// Gets the collection of roles assigned to this user.
    /// </summary>
    public ICollection<RoleDto> Roles { get; set; }

    /// <summary>
    /// Gets the unique identifier of the user's organization.
    /// </summary>
    public Guid OrganizationId { get; private set; }
}