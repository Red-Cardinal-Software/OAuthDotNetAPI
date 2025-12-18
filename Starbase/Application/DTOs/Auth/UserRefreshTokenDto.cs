using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth;

public class UserRefreshTokenDto
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(256, ErrorMessage = "Username must not exceed 256 characters")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "Refresh token is required")]
    [StringLength(512, ErrorMessage = "Refresh token must not exceed 512 characters")]
    public required string RefreshToken { get; set; }
}