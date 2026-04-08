namespace MiniPos.Frontend.Pages.Branches;

public class Merchant
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? ContactEmail { get; set; }
}

public class BranchCreateRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? MerchantId { get; set; }
}

public class Branch
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public DateTime? CreatedAt { get; set; }
    public decimal TodayOrderPrice { get; set; }
    public int TodayOrderCount { get; set; }
    public Merchant? Merchant { get; set; }
}

public class BranchUpdateRequest
{
    public string MerchantId { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
}

public class UserAssignBranchRequest
{
    public Guid BranchId { get; set; }
}

public class User
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? TotalSales { get; set; }
    public Merchant? Merchant { get; set; }
    public Branch? Branch { get; set; }
    public List<Branch> Branches { get; set; } = new();
    public DateTime? JoinedDate { get; set; }
}