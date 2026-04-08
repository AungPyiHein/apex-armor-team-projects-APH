namespace POS.Frontend.Models.Sales;

public class CustomerResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string MerchantName { get; set; } = string.Empty;
}

public class CreateCustomerRequest
{
    public Guid MerchantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}
