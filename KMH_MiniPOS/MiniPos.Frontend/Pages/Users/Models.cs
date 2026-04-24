namespace MiniPos.Frontend.Pages.Users;

public class Merchant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int BranchCount { get; set; }
    public int ProductCount { get; set; }
    public int EmployeeCount { get; set; }
}


public class Branch
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public virtual Merchant? Merchant { get; set; }
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