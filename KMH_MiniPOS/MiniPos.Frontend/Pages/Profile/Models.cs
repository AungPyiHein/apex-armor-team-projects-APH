namespace MiniPos.Frontend.Pages.Profile;

public class ProfileModel
{
    public Guid Id { get; set; }
    public string? Role { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalSales { get; set; }
    public ProfileMerchantModel? Merchant { get; set; }
    public ProfileBranchModel? Branch { get; set; }
}

public class ProfileMerchantModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? ContactEmail { get; set; }
}

public class ProfileBranchModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
}

public class UpdateProfileResponse
{
    public string? Message { get;set; }
}