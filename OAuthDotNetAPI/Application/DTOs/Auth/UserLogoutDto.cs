namespace Application.DTOs.Auth;

public class UserLogoutDto
{
    public string Username { get; set; }
    public string RefreshToken { get; set; }
}