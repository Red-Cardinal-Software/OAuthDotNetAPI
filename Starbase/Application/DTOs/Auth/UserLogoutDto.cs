namespace Application.DTOs.Auth;

public class UserLogoutDto
{
    public required string Username { get; set; }
    public required string RefreshToken { get; set; }
}
