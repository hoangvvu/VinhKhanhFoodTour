using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Entities;

namespace VKFoodTour.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // ══════════════════════════════════════════════════
    //  DbSets — Đầy đủ 10 bảng theo DB Schema v2.0
    // ══════════════════════════════════════════════════
    public DbSet<Language> Languages { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Poi> Pois { get; set; }
    public DbSet<Narration> Narrations { get; set; }
    public DbSet<Food> Foods { get; set; }
    public DbSet<FoodTranslation> FoodTranslations { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<QrCode> QrCodes { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<TrackingLog> TrackingLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── LANGUAGE ────────────────────────────────
        modelBuilder.Entity<Language>(entity =>
        {
            entity.ToTable("LANGUAGES");
            entity.HasKey(e => e.LanguageId);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // ── USER ────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("USERS");
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");
        });

        // ── POI ─────────────────────────────────────
        modelBuilder.Entity<Poi>(entity =>
        {
            entity.ToTable("POIS");
            entity.HasKey(e => e.PoiId);

            entity.Property(e => e.PoiId)
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");

            // Quan hệ: Owner (User) → nhiều POI
            entity.HasOne<User>()
                  .WithMany(u => u.Pois)
                  .HasForeignKey(p => p.OwnerId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Index tọa độ (filtered)
            entity.HasIndex(e => new { e.Latitude, e.Longitude })
                  .HasFilter("[is_active] = 1")
                  .HasDatabaseName("idx_pois_geo");
        });

        // ── NARRATION ───────────────────────────────
        modelBuilder.Entity<Narration>(entity =>
        {
            entity.ToTable("NARRATIONS");
            entity.HasKey(e => e.NarrationId);

            // Unique: 1 POI + 1 Language = 1 Narration
            entity.HasIndex(e => new { e.PoiId, e.LanguageId })
                  .IsUnique()
                  .HasDatabaseName("uq_narration_poi_lang");

            entity.HasIndex(e => new { e.PoiId, e.LanguageId })
                  .HasDatabaseName("idx_narrations_poi_lang");
        });

        // ── FOOD ────────────────────────────────────
        modelBuilder.Entity<Food>(entity =>
        {
            entity.ToTable("FOODS");
            entity.HasKey(e => e.FoodId);
        });

        // ── FOOD TRANSLATION ────────────────────────
        modelBuilder.Entity<FoodTranslation>(entity =>
        {
            entity.ToTable("FOOD_TRANSLATIONS");
            entity.HasKey(e => e.TranslationId);

            entity.HasIndex(e => new { e.FoodId, e.LanguageId })
                  .IsUnique()
                  .HasDatabaseName("uq_food_translation");
        });

        // ── IMAGE ───────────────────────────────────
        modelBuilder.Entity<Image>(entity =>
        {
            entity.ToTable("IMAGES");
            entity.HasKey(e => e.ImageId);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");
        });

        // ── QRCODE ──────────────────────────────────
        modelBuilder.Entity<QrCode>(entity =>
        {
            entity.ToTable("QRCODES");
            entity.HasKey(e => e.QrId);

            entity.HasIndex(e => e.QrToken)
                  .IsUnique();

            entity.HasIndex(e => e.QrToken)
                  .HasFilter("[is_active] = 1")
                  .HasDatabaseName("idx_qr_token");

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");
        });

        // ── REVIEW ──────────────────────────────────
        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("REVIEWS");
            entity.HasKey(e => e.ReviewId);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");
        });

        // ── TRACKING LOG ────────────────────────────
        modelBuilder.Entity<TrackingLog>(entity =>
        {
            entity.ToTable("TRACKING_LOGS");
            entity.HasKey(e => e.LogId);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(e => new { e.DeviceId, e.CreatedAt })
                  .IsDescending(false, true)
                  .HasDatabaseName("idx_tracking_device");

            entity.HasIndex(e => new { e.PoiId, e.EventType, e.CreatedAt })
                  .IsDescending(false, false, true)
                  .HasDatabaseName("idx_tracking_poi");

            entity.HasIndex(e => new { e.Latitude, e.Longitude })
                  .HasDatabaseName("idx_tracking_geo");
        });
    }
}