namespace MiniPos.Frontend.Pages.Merchants;

public class Branch
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public Merchant? Merchant { get; set; }
}

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
