using Common;
using Database.EfAppDbContextModels;
using Microsoft.EntityFrameworkCore;
using MiniPos.Backend.Features.Loyalties;

namespace MiniPos.Backend.Features.Customers;

public interface ICustomerService
{
    Task<Result<PagedResult<CustomerDto>>> GetList(CustomerListRequest request);
    Task<Result<CustomerDetails>> GetById(Guid customerId);
    Task<Result<CustomerDetails>> Lookup(CustomerLookup request);
    Task<Result> Create(CustomerCreateRequest request);
    Task<Result> Update(Guid id, CustomerUpdateRequest request);
    Task<Result> Delete(Guid id);
}

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;
    private readonly ILoyaltyService _loyaltyService;

    public CustomerService(AppDbContext db, ILoyaltyService loyaltyService)
    {
        _db = db;
        _loyaltyService = loyaltyService;
    }

    public async Task<Result<PagedResult<CustomerDto>>> GetList(CustomerListRequest request)
    {
        const string errCode = "Customer.GetList";
        try
        {
            if (!await IsMerchantOwner(request.MerchantId, request.ProcessedById))
                return Result<PagedResult<CustomerDto>>.Failure(new UnAuthorizedError(errCode,
                    "User is not authorized to access customers for this merchant"));

            var query = _db.Customers
                .Where(c => c.MerchantId == request.MerchantId)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(c =>
                    c.Name.Contains(request.SearchTerm) ||
                    c.PhoneNumber != null && c.PhoneNumber.Contains(request.SearchTerm) ||
                    c.Email != null && c.Email.Contains(request.SearchTerm));
            }

            var skip = (request.PageNumber - 1) * request.PageSize;
            var take = request.PageSize;
            var totalCount = await query.CountAsync();

            var items = await query
                .Skip(skip)
                .Take(take)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber ?? "",
                    Email = c.Email ?? "",
                })
                .ToListAsync();

            var result = new PagedResult<CustomerDto>(items, totalCount, request.PageNumber, request.PageSize);
            return Result<PagedResult<CustomerDto>>.Success(result);
        }
        catch (Exception e)
        {
            return Result<PagedResult<CustomerDto>>.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result<CustomerDetails>> GetById(Guid customerId)
    {
        const string errCode = "Customer.GetById";
        try
        {
            var customer = await _db.Customers
                .AsNoTracking()
                .Where(c => c.Id == customerId && c.DeletedAt == null)
                .Select(c => new CustomerDetails
                {
                    Id = c.Id,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber,
                    Email = c.Email,
                    CreatedAt = c.CreatedAt,
                    MerchantId = c.MerchantId,
                    MerchantName = c.Merchant.Name,
                    Orders = c.Orders.Select(o => new OrderDto
                    {
                        Id = o.Id,
                        TotalAmount = o.TotalAmount,
                        CreatedAt = o.CreatedAt
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (customer == null)
                return Result<CustomerDetails>.Failure(new NotFoundError(errCode, "Customer does not exist"));

            var result = await _loyaltyService.LookupAccountAsync(customer.Id);
            if (result is not { IsSuccess: true, Data: not null }) return Result<CustomerDetails>.Success(customer);

            var r = result.Data;
            var royalty = new RoyaltyDto
            {
                CurrentPoint = r.CurrentBalance,
                LifetimePoints = r.LifetimePoints,
                Tier = r.Tier,
            };
            customer.Royalty = royalty;

            return Result<CustomerDetails>.Success(customer);
        }
        catch (Exception e)
        {
            return Result<CustomerDetails>.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result<CustomerDetails>> Lookup(CustomerLookup request)
    {
        const string errCode = "Customer.GetById";
        try
        {
            var customer = await _db.Customers
                .AsNoTracking()
                .Where(c => c.PhoneNumber == request.PhoneNo)
                .Select(c => new CustomerDetails
                {
                    Id = c.Id,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber,
                    Email = c.Email,
                    CreatedAt = c.CreatedAt,
                    MerchantId = c.MerchantId,
                    MerchantName = c.Merchant.Name,
                    Orders = c.Orders.Select(o => new OrderDto
                    {
                        Id = o.Id,
                        TotalAmount = o.TotalAmount,
                        CreatedAt = o.CreatedAt
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (customer == null)
                return Result<CustomerDetails>.Failure(new NotFoundError(errCode, "Customer does not exist"));

            var result = await _loyaltyService.LookupAccountAsync(customer.Id);
            if (result is not { IsSuccess: true, Data: not null }) return Result<CustomerDetails>.Success(customer);

            var r = result.Data;
            var royalty = new RoyaltyDto
            {
                CurrentPoint = r.CurrentBalance,
                LifetimePoints = r.LifetimePoints,
                Tier = r.Tier,
            };
            customer.Royalty = royalty;

            return Result<CustomerDetails>.Success(customer);
        }
        catch (Exception e)
        {
            return Result<CustomerDetails>.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Create(CustomerCreateRequest request)
    {
        const string errCode = "Customer.Create";
        try
        {
            var merchantExists = await _db.Merchants.AnyAsync(m => m.Id == request.MerchantId);
            if (!merchantExists)
                return Result.Failure(new NotFoundError(errCode, "Merchant does not exist"));

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                MerchantId = request.MerchantId,
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Customers.AddAsync(customer);
            var result = await _db.SaveChangesAsync();
            if (result <= 0)
                return Result.Failure(new InternalError(errCode, "Failed to create customer"));

            await TrySendSignupEvent(customer);
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    private async Task TrySendSignupEvent(Customer customer)
    {
        try
        {
            var loyaltyRequest = new CreateEventRequest
            {
                ExternalUserId = customer.Id.ToString(),
                EventKey = "SIGNUP",
                EventValue = 0,
                ReferenceId = $"CUST-{customer.Id}",
                Description = $"Customer signup: {customer.Name}",
                Mobile = customer.PhoneNumber ?? "",
                Email = customer.Email ?? ""
            };

            var result = await _loyaltyService.CreateEventAsync(loyaltyRequest);
            if (!result.IsSuccess)
                Console.WriteLine($"[Loyalty] SIGNUP failed: {result.Error?.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Loyalty] SIGNUP exception: {e.Message}");
        }
    }

    public async Task<Result> Update(Guid id, CustomerUpdateRequest request)
    {
        const string errCode = "Customer.Update";
        try
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
            if (customer == null)
                return Result.Failure(new NotFoundError(errCode, "Customer does not exist"));

            customer.Name = request.Name;
            customer.PhoneNumber = request.PhoneNumber;
            customer.Email = request.Email;
            customer.UpdatedAt = DateTime.UtcNow;

            var result = await _db.SaveChangesAsync();
            return result > 0
                ? Result.Success()
                : Result.Failure(new InternalError(errCode, "Failed to update customer"));
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    public async Task<Result> Delete(Guid id)
    {
        const string errCode = "Customer.Delete";
        try
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
            if (customer == null)
                return Result.Failure(new NotFoundError(errCode, "Customer does not exist"));

            customer.DeletedAt = DateTime.UtcNow;
            var result = await _db.SaveChangesAsync();

            return result > 0
                ? Result.Success()
                : Result.Failure(new InternalError(errCode, "Failed to delete customer"));
        }
        catch (Exception e)
        {
            return Result.Failure(new InternalError(errCode, e.Message));
        }
    }

    private async Task<bool> IsMerchantOwner(Guid merchantId, Guid merchantAdminId)
    {
        return await _db.MerchantAdmins.AnyAsync(ma => ma.MerchantId == merchantId && ma.UserId == merchantAdminId);
    }
}

public class CustomerListRequest : PaginationFilter
{
    public Guid ProcessedById { get; set; }
    public Guid MerchantId { get; set; }
    public string? SearchTerm { get; set; }
}

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
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
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderDto> Orders { get; set; } = new();
    public RoyaltyDto? Royalty { get; set; } = null;
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

public class CustomerLookup
{
    public string? PhoneNo { get; set; } = string.Empty;
}