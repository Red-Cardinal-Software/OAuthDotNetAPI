namespace Infrastructure.Security.Entities;

public class BlacklistedPassword
{
    public int Id { get; set; }
    public string HashedPassword { get; set; } = null!;
}