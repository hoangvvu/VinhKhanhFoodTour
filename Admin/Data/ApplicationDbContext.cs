using Microsoft.EntityFrameworkCore;
using Admin.Models;

namespace Admin.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ── DbSets ─────────────────────────────────────
        public DbSet<Poi> Pois { get; set; }

        // ── Cấu hình Fluent API ────────────────────────
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── POI ─────────────────────────────────────
            modelBuilder.Entity<Poi>(entity =>
            {
                // Tên bảng (đã có [Table] attribute, nhưng khai báo ở đây 
                // cho rõ ràng và dễ maintain khi project lớn lên)
                entity.ToTable("POIS");

                // Khóa chính
                entity.HasKey(e => e.PoiId);

                // Cột poi_id tự tăng (IDENTITY)
                entity.Property(e => e.PoiId)
                      .ValueGeneratedOnAdd();

                // Giá trị mặc định cho created_at — 
                // để SQL Server tự xử lý bằng GETDATE()
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETDATE()");

                // Index trên tọa độ (khớp với idx_pois_geo trong SQL)
                // Chỉ index những POI đang active
                entity.HasIndex(e => new { e.Latitude, e.Longitude })
                      .HasFilter("[is_active] = 1")
                      .HasDatabaseName("idx_pois_geo");
            });
        }
    }
}