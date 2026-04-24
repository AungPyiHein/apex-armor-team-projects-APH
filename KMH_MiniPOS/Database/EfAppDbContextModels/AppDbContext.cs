using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Database.EfAppDbContextModels;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<BranchInventory> BranchInventories { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Merchant> Merchants { get; set; }

    public virtual DbSet<MerchantAdmin> MerchantAdmins { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Branches__3214EC07E489372B");

            entity.HasIndex(e => e.MerchantId, "IX_Branches_MerchantId").HasFilter("([DeletedAt] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Merchant).WithMany(p => p.Branches)
                .HasForeignKey(d => d.MerchantId)
                .HasConstraintName("FK_Branches_Merchants");
        });

        modelBuilder.Entity<BranchInventory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BranchIn__3214EC073EFA602C");

            entity.ToTable("BranchInventory");

            entity.HasIndex(e => e.BranchId, "IX_BranchInventory_BranchId").HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => e.ProductId, "IX_BranchInventory_ProductId").HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => new { e.BranchId, e.ProductId }, "UQ_BranchInventory_Branch_Product")
                .IsUnique()
                .HasFilter("([DeletedAt] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Branch).WithMany(p => p.BranchInventories)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchInventory_Branches");

            entity.HasOne(d => d.Product).WithMany(p => p.BranchInventories)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_BranchInventory_Products");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC072704BE8F");

            entity.HasIndex(e => e.MerchantId, "IX_Categories_MerchantId").HasFilter("([DeletedAt] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Merchant).WithMany(p => p.Categories)
                .HasForeignKey(d => d.MerchantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Categories_Merchants");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Customer__3214EC071AE5326C");

            entity.HasIndex(e => e.MerchantId, "IX_Customers_MerchantId").HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => new { e.MerchantId, e.Email }, "IX_Customers_Merchant_Email").HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => new { e.MerchantId, e.PhoneNumber }, "IX_Customers_Merchant_Phone").HasFilter("([DeletedAt] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);

            entity.HasOne(d => d.Merchant).WithMany(p => p.Customers)
                .HasForeignKey(d => d.MerchantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Customers_Merchants");
        });

        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Merchant__3214EC07476D85FB");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ContactEmail).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<MerchantAdmin>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.MerchantId });

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Merchant).WithMany(p => p.MerchantAdmins)
                .HasForeignKey(d => d.MerchantId)
                .HasConstraintName("FK_MerchantAdmins_Merchants");

            entity.HasOne(d => d.User).WithMany(p => p.MerchantAdmins)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_MerchantAdmins_Users");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Orders__3214EC07CAC59078");

            entity.HasIndex(e => e.BranchId, "IX_Orders_BranchId").HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => new { e.BranchId, e.OrderDate }, "IX_Orders_Branch_Date")
                .IsDescending(false, true)
                .HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => e.CustomerId, "IX_Orders_CustomerId");

            entity.HasIndex(e => e.MerchantId, "IX_Orders_MerchantId").HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => e.ProcessedById, "IX_Orders_ProcessedById");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.OrderDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Branch).WithMany(p => p.Orders)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Branches");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Orders_Customers");

            entity.HasOne(d => d.Merchant).WithMany(p => p.Orders)
                .HasForeignKey(d => d.MerchantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Merchants");

            entity.HasOne(d => d.ProcessedBy).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ProcessedById)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderIte__3214EC07297A9A80");

            entity.HasIndex(e => e.OrderId, "IX_OrderItems_OrderId");

            entity.HasIndex(e => e.ProductId, "IX_OrderItems_ProductId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Products");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Products__3214EC07ECF3359B");

            entity.HasIndex(e => e.CategoryId, "IX_Products_CategoryId").HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => e.MerchantId, "IX_Products_MerchantId").HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => new { e.MerchantId, e.Name }, "IX_Products_Merchant_Name").HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => new { e.MerchantId, e.Sku }, "UQ_Products_Merchant_SKU")
                .IsUnique()
                .HasFilter("([DeletedAt] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .HasColumnName("SKU");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Categories");

            entity.HasOne(d => d.Merchant).WithMany(p => p.Products)
                .HasForeignKey(d => d.MerchantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Merchants");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProductI__3214EC07A6EE7438");

            entity.HasIndex(e => e.ProductId, "IX_ProductImages_ProductId").HasFilter("([DeletedAt] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ImageUrl).HasMaxLength(2048);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductImages_Products");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC07DB1EED5C");

            entity.HasIndex(e => e.UserId, "IX_RefreshTokens_UserId").HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => e.Token, "UQ_RefreshTokens_Token")
                .IsUnique()
                .HasFilter("([DeletedAt] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ReplacedByTokenHash).HasMaxLength(255);
            entity.Property(e => e.Token).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07C4E4BE91");

            entity.HasIndex(e => e.BranchId, "IX_Users_BranchId").HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => e.MerchantId, "IX_Users_MerchantId").HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => e.Email, "UQ_Users_Email")
                .IsUnique()
                .HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => e.Username, "UQ_Users_Username")
                .IsUnique()
                .HasFilter("([DeletedAt] IS NULL)");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E473704BEA").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105342B30EDB2").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Branch).WithMany(p => p.Users)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK_Users_Branches");

            entity.HasOne(d => d.Merchant).WithMany(p => p.Users)
                .HasForeignKey(d => d.MerchantId)
                .HasConstraintName("FK_Users_Merchants");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
