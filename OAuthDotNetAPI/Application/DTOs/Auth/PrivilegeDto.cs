namespace Application.DTOs.Auth;

public class PrivilegeDto
{
    /// <summary>
    /// Gets the unique identifier for the privilege.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets the name of the privilege.
    /// </summary>
    public string Name { get; set; }
}