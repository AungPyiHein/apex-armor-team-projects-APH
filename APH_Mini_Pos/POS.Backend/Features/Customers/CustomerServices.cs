using Microsoft.EntityFrameworkCore;
using POS.Backend.Common;
using POS.data.Data;
using POS.data.Entities;

namespace POS.Backend.Features.Customers
{
    public class CustomerResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string MerchantName { get; set; } = string.Empty;
    }

    public class CreateCustomerRequest
    {
        public Guid MerchantId { get; set; }
        public string Name { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }

    public interface ICustomerServices
    {
        Task<Result<PagedResponse<CustomerResponseDto>>> GetCustomersAsync(Guid merchantId, PaginationFilter filter);
        Task<Result<CustomerResponseDto>> GetCustomerByIdAsync(Guid id);
        Task<Result<Guid>> CreateCustomerAsync(CreateCustomerRequest request);
        Task<Result<bool>> UpdateCustomerAsync(Guid id, CreateCustomerRequest request);
        Task<Result<bool>> DeleteCustomerAsync(Guid id);
    }

    public class CustomerServices : ICustomerServices
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CustomerServices(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result<PagedResponse<CustomerResponseDto>>> GetCustomersAsync(Guid merchantId, PaginationFilter filter)
        {
            var query = _context.Customers
                .Include(c => c.Merchant)
                .Where(c => c.DeletedAt == null)
                .AsQueryable();

            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin || _currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                var targetMerchantId = _currentUser.MerchantId ?? merchantId;
                query = query.Where(c => c.MerchantId == targetMerchantId);
            }
            else if (merchantId != Guid.Empty)
            {
                query = query.Where(c => c.MerchantId == merchantId);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(c => EF.Functions.Like(c.Name, $"%{filter.SearchTerm}%") ||
                                         EF.Functions.Like(c.Email, $"%{filter.SearchTerm}%") ||
                                         EF.Functions.Like(c.PhoneNumber, $"%{filter.SearchTerm}%"));
            }

            var totalRecords = await query.CountAsync();

            var customers = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new CustomerResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber,
                    Email = c.Email,
                    MerchantName = c.Merchant.Name
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<CustomerResponseDto>(customers, totalRecords, filter.PageNumber, filter.PageSize);
            return Result<PagedResponse<CustomerResponseDto>>.Success(pagedResponse);
        }

        public async Task<Result<CustomerResponseDto>> GetCustomerByIdAsync(Guid id)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (customer == null) return Result<CustomerResponseDto>.Failure("Customer not found.");

            if (_currentUser.Role != POS.Shared.Models.UserRole.Admin && customer.MerchantId != _currentUser.MerchantId)
            {
                return Result<CustomerResponseDto>.Failure("You do not have permission to view this customer.");
            }

            return Result<CustomerResponseDto>.Success(new CustomerResponseDto
            {
                Id = customer.Id,
                Name = customer.Name,
                PhoneNumber = customer.PhoneNumber,
                Email = customer.Email,
                MerchantName = customer.Merchant.Name
            });
        }

        public async Task<Result<Guid>> CreateCustomerAsync(CreateCustomerRequest request)
        {
            if (_currentUser.Role != POS.Shared.Models.UserRole.Admin)
            {
                request.MerchantId = _currentUser.MerchantId ?? request.MerchantId;
            }

            var merchantExists = await _context.Merchants.AnyAsync(m => m.Id == request.MerchantId && m.DeletedAt == null);
            if (!merchantExists) return Result<Guid>.Failure("Merchant not found.");

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                MerchantId = request.MerchantId,
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Result<Guid>.Success(customer.Id);
        }

        public async Task<Result<bool>> UpdateCustomerAsync(Guid id, CreateCustomerRequest request)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null || customer.DeletedAt != null) return Result<bool>.Failure("Customer not found.");

            if (_currentUser.Role != POS.Shared.Models.UserRole.Admin && customer.MerchantId != _currentUser.MerchantId)
            {
                return Result<bool>.Failure("You do not have permission to update this customer.");
            }

            customer.Name = request.Name;
            customer.PhoneNumber = request.PhoneNumber;
            customer.Email = request.Email;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeleteCustomerAsync(Guid id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null || customer.DeletedAt != null) return Result<bool>.Failure("Customer not found.");

            if (_currentUser.Role != POS.Shared.Models.UserRole.Admin && customer.MerchantId != _currentUser.MerchantId)
            {
                return Result<bool>.Failure("You do not have permission to delete this customer.");
            }

            customer.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }
    }
}
