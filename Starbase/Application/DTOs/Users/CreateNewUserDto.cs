using System.ComponentModel.DataAnnotations;
using Application.DTOs.Auth;
using Application.DTOs.Validation;

namespace Application.DTOs.Users;

public class CreateNewUserDto : IValidatableDto
{
    /// <summary>
    /// Gets the username used for login and identification.
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(256, ErrorMessage = "Username must not exceed 256 characters")]
    [EmailAddress(ErrorMessage = "Username must be a valid email address")]
    public required string Username { get; set; }

    /// <summary>
    /// The password of the new user.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(128, ErrorMessage = "Password must not exceed 128 characters")]
    public required string Password { get; set; }

    /// <summary>
    /// Gets the user's first name.
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, ErrorMessage = "First name must not exceed 100 characters")]
    public required string FirstName { get; set; }

    /// <summary>
    /// Gets the user's last name.
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, ErrorMessage = "Last name must not exceed 100 characters")]
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