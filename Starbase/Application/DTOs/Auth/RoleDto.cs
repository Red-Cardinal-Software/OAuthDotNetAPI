namespace Application.DTOs.Auth;

public class RoleDto
{
    /// <summary>
    /// Gets the unique identifier for the role.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets the name of the role.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets the collection of privileges associated with this role.
    /// </summary>
    public required ICollection<PrivilegeDto> Privileges { get; set; }
}
