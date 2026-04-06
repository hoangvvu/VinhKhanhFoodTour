using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Entities;

namespace VKFoodTour.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Language> Languages { get; set; }
    public DbSet<Poi> Pois { get; set; }
    public DbSet<Narration> Narrations { get; set; }
    public DbSet<TrackingLog> TrackingLogs { get; set; }
    // Thêm DbSet cho Users, Foods, QrCodes...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ánh xạ Unique Constraint cho Narration dựa theo SQL của bạn
        modelBuilder.Entity<Narration>()
            .HasIndex(n => new { n.PoiId, n.LanguageId })
            .IsUnique();

        // Bạn có thể thêm cấu hình Index ở đây nếu muốn đồng bộ hoàn toàn với SQL
    }
}