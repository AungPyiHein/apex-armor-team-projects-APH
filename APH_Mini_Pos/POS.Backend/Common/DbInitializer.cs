using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using POS.Backend.Common;
using POS.data.Data;
using POS.data.Entities;

namespace POS.Backend.Common
{
    public static class DbInitializer
    {
        public static async Task Initialize(AppDbContext context)
        {

            if (await context.Users.AnyAsync())
            {
                return;
            }

            var merchant = new Merchant
            {
                Id = Guid.NewGuid(),
                Name = "Default Merchant",
                ContactEmail = "admin@pos.com",
                Address = "Default Merchant Address",
                PhoneNumber = "09123456789",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            context.Merchants.Add(merchant);

            var branch = new Branch
            {
                Id = Guid.NewGuid(),
                MerchantId = merchant.Id,
                Name = "Main Branch",
                Address = "Default City",
                PhoneNumber = "09123456790",
                CreatedAt = DateTime.UtcNow
            };
            context.Branches.Add(branch);

            var passwordHasher = new PasswordHasher<User>();
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                MerchantId = merchant.Id,
                BranchId = branch.Id,
                Username = "admin",
                Email = "admin@pos.com",
                FullName = "System Admin",
                PhoneNumber = "09123456791",
                IsActive = true,
                Role = UserRole.Admin.ToString(),
                CreatedAt = DateTime.UtcNow
            };
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin@123");
            context.Users.Add(adminUser);

            await context.SaveChangesAsync();
        }
    }
}
