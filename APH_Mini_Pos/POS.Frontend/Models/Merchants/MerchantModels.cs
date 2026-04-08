using System;

namespace POS.Frontend.Models.Merchants;

public class MerchantResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public int CategoryCount { get; set; }
    public int ProductCount { get; set; }
}

public class CreateMerchantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
}

public class UpdateMerchantRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? ContactEmail { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? IsActive { get; set; }
}
