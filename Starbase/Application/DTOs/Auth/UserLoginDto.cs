namespace Application.DTOs.Auth;

public class UserLoginDto
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}
