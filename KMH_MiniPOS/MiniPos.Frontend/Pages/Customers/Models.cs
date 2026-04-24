namespace MiniPos.Frontend.Pages.Customers;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public Guid MerchantId { get; set; }
    public int OrderCount { get; set; }
}

public class RoyaltyDto
{
    public int CurrentPoint { get; set; }
    public int LifetimePoints { get; set; }
    public string Tier { get; set; } = string.Empty;
}

public class CustomerDetails
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid MerchantId { get; set; }
    public string MerchantName { get; set; } = null!;
    public List<OrderDto> Orders { get; set; } = [];
    public RoyaltyDto? Royalty { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerCreateRequest
{
    public Guid MerchantId { get; set; }
    public string Name { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class CustomerUpdateRequest
{
    public string Name { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class MerchantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
