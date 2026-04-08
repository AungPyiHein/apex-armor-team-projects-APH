using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using POS.Backend.Common;
using POS.data.Data;

namespace POS.Backend.Features.User
{


    public class CreateUserRequest
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string PlainPassword { get; set; } = null!;
        public UserRole Role { get; set; }
        public Guid? MerchantId { get; set; }
        public Guid? BranchId { get; set; }
    }
    public class UpdateUserRequest
    {
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? IsActive { get; set; }
        public string? PlainPassword { get; set; }
        public UserRole? Role { get; set; }
        public Guid? MerchantId { get; set; }
        public Guid? BranchId { get; set; }
    }

    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
        public Guid? MerchantId { get; set; }
        public Guid? BranchId { get; set; }
        public string? MerchantName { get; set; }
        public string? BranchName { get; set; }
    }

    public interface IUserServices
    {
        Task<Result<Guid>> CreateUserAsync(CreateUserRequest request);
        Task<Result<UserResponseDto>> GetUserByIdAsync(Guid id);
        Task<Result<PagedResponse<UserResponseDto>>> GetAllUsersAsync(PaginationFilter filter);
        Task<Result<bool>> UpdateUserAsync(Guid id, UpdateUserRequest request);
        Task<Result<bool>> DeleteUserAsync(Guid id);
    }

    public class UserServices : IUserServices
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<POS.data.Entities.User> _passwordHasher;
        private readonly ICurrentUserService _currentUser;

        public UserServices(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<POS.data.Entities.User>();
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> CreateUserAsync(CreateUserRequest request)
        {
            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin || _currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                if (_currentUser.MerchantId.HasValue)
                {
                    request.MerchantId = _currentUser.MerchantId.Value;
                }
                else
                {
                    return Result<Guid>.Failure("Merchant ID is missing from user claims.");
                }

                if (_currentUser.Role == POS.Shared.Models.UserRole.Staff && _currentUser.BranchId.HasValue)
                {
                    request.BranchId = _currentUser.BranchId.Value;
                }
            }

            var userExists = await _context.Users.AnyAsync(u => (u.Username == request.Username || u.Email == request.Email) && u.DeletedAt == null);
            if (userExists) return Result<Guid>.Failure("Username or Email already exists.");

            var user = new data.Entities.User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                MerchantId = request.MerchantId,
                BranchId = request.BranchId,
                Role = request.Role.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.PlainPassword);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Result<Guid>.Success(user.Id);
        }

        public async Task<Result<UserResponseDto>> GetUserByIdAsync(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.Merchant)
                .Include(u => u.Branch)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null) return Result<UserResponseDto>.Failure("User not found.");

            if (_currentUser.Role != POS.Shared.Models.UserRole.Admin && user.MerchantId != _currentUser.MerchantId)
            {
                return Result<UserResponseDto>.Failure("You do not have permission to view this user.");
            }

            return Result<UserResponseDto>.Success(new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsActive = user.IsActive,
                MerchantId = user.MerchantId,
                BranchId = user.BranchId,
                MerchantName = user.Merchant?.Name,
                BranchName = user.Branch?.Name
            });
        }

        public async Task<Result<PagedResponse<UserResponseDto>>> GetAllUsersAsync(PaginationFilter filter)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.DeletedAt == null)
                .AsQueryable();

            if (_currentUser.Role == POS.Shared.Models.UserRole.MerchantAdmin || _currentUser.Role == POS.Shared.Models.UserRole.Staff)
            {
                query = query.Where(u => u.MerchantId == _currentUser.MerchantId);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(u => EF.Functions.Like(u.Username, $"%{filter.SearchTerm}%") ||
                                         EF.Functions.Like(u.Email, $"%{filter.SearchTerm}%"));
            }

            var totalRecords = await query.CountAsync();

            var users = await query
                .Include(u => u.Merchant)
                .Include(u => u.Branch)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    MerchantId = u.MerchantId,
                    BranchId = u.BranchId,
                    MerchantName = u.Merchant != null ? u.Merchant.Name : null,
                    BranchName = u.Branch != null ? u.Branch.Name : null
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<UserResponseDto>(users, totalRecords, filter.PageNumber, filter.PageSize);
            return Result<PagedResponse<UserResponseDto>>.Success(pagedResponse);
        }

        public async Task<Result<bool>> UpdateUserAsync(Guid id, UpdateUserRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
            if (user == null) return Result<bool>.Failure("User not found.");

            if (_currentUser.Role != POS.Shared.Models.UserRole.Admin && user.MerchantId != _currentUser.MerchantId)
            {
                return Result<bool>.Failure("You do not have permission to update this user.");
            }

            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id && u.DeletedAt == null);
                if (emailExists) return Result<bool>.Failure("Email already in use.");
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.FullName))
            {
                user.FullName = request.FullName;
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                user.PhoneNumber = request.PhoneNumber;
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            if (request.Role.HasValue)
            {
                user.Role = request.Role.Value.ToString();
            }

            if (request.MerchantId.HasValue)
            {
                user.MerchantId = request.MerchantId.Value;
            }

            if (request.BranchId.HasValue)
            {
                user.BranchId = request.BranchId.Value;
            }

            if (!string.IsNullOrEmpty(request.PlainPassword))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, request.PlainPassword);
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
            if (user == null) return Result<bool>.Failure("User not found.");

            if (_currentUser.Role != POS.Shared.Models.UserRole.Admin && user.MerchantId != _currentUser.MerchantId)
            {
                return Result<bool>.Failure("You do not have permission to delete this user.");
            }

            if (user.Role == POS.Shared.Models.UserRole.MerchantAdmin.ToString())
            {
                var activeAdminsCount = await _context.Users.CountAsync(u => u.MerchantId == user.MerchantId && u.Role == POS.Shared.Models.UserRole.MerchantAdmin.ToString() && u.DeletedAt == null && u.Id != id);
                if (activeAdminsCount == 0)
                {
                    return Result<bool>.Failure("Cannot delete this user. Every merchant must have at least one active Merchant Admin.");
                }
            }

            user.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
    }


}
