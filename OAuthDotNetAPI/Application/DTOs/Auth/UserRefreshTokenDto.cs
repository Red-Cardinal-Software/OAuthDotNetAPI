namespace Application.DTOs.Auth;

public class UserRefreshTokenDto
{
    public required string Username { get; set; }
    public required string RefreshToken { get; set; }
}