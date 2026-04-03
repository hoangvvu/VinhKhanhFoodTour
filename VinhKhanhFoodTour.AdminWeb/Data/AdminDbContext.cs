using Microsoft.EntityFrameworkCore;
using VinhKhanhFoodTour.AdminWeb.Models;

namespace VinhKhanhFoodTour.AdminWeb.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options)
    {
    }

    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<ManagedShop> ManagedShops { get; set; }
    public DbSet<ShopStatistics> ShopStatistics { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AdminUser configuration
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Password).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
            entity.HasOne(e => e.ManagedShop)
                .WithMany(s => s.Managers)
                .HasForeignKey(e => e.ManagedShopId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ManagedShop configuration
        modelBuilder.Entity<ManagedShop>(entity =>
        {
            entity.HasKey(e => e.ShopId);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        // ShopStatistics configuration
        modelBuilder.Entity<ShopStatistics>(entity =>
        {
            entity.HasKey(e => new { e.ShopId, e.StatisticsDate });
            entity.Property(e => e.TotalRevenue).HasPrecision(18, 2);
            entity.Property(e => e.AverageRating).HasPrecision(3, 2);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId);
            entity.Property(e => e.Action).HasMaxLength(255);
            entity.Property(e => e.EntityType).HasMaxLength(255);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
        });
    }
}
