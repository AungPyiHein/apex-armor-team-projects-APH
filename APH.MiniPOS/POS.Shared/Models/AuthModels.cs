namespace POS.Shared.Models;

public class LoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class AuthResponse
{
    public string Username { get; set; } = null!;
    public string Role { get; set; } = null!;
    public Guid? UserId { get; set; }
    public Guid? MerchantId { get; set; }
    public Guid? BranchId { get; set; }
}
