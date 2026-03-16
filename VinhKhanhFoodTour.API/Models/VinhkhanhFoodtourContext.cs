using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace VinhKhanhFoodTour.API.Models;

public partial class VinhkhanhFoodtourContext : DbContext
{
    public VinhkhanhFoodtourContext()
    {
    }

    public VinhkhanhFoodtourContext(DbContextOptions<VinhkhanhFoodtourContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Food> Foods { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<Language> Languages { get; set; }

    public virtual DbSet<Narration> Narrations { get; set; }

    public virtual DbSet<Poi> Pois { get; set; }

    public virtual DbSet<Qrcode> Qrcodes { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<TrackingLog> TrackingLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=vinhkhanh_foodtour;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Food>(entity =>
        {
            entity.HasKey(e => e.FoodId).HasName("PK__FOODS__2F4C4DD8CDCE6F43");

            entity.ToTable("FOODS");

            entity.Property(e => e.FoodId).HasColumnName("food_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.PoiId).HasColumnName("poi_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");

            entity.HasOne(d => d.Poi).WithMany(p => p.Foods)
                .HasForeignKey(d => d.PoiId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__FOODS__poi_id__534D60F1");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__IMAGES__DC9AC9559B032593");

            entity.ToTable("IMAGES");

            entity.Property(e => e.ImageId).HasColumnName("image_id");
            entity.Property(e => e.FoodId).HasColumnName("food_id");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("image_url");
            entity.Property(e => e.PoiId).HasColumnName("poi_id");

            entity.HasOne(d => d.Food).WithMany(p => p.Images)
                .HasForeignKey(d => d.FoodId)
                .HasConstraintName("FK__IMAGES__food_id__5AEE82B9");

            entity.HasOne(d => d.Poi).WithMany(p => p.Images)
                .HasForeignKey(d => d.PoiId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__IMAGES__poi_id__59FA5E80");
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasKey(e => e.LanguageId).HasName("PK__LANGUAGE__804CF6B371B22082");

            entity.ToTable("LANGUAGES");

            entity.Property(e => e.LanguageId).HasColumnName("language_id");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Narration>(entity =>
        {
            entity.HasKey(e => e.NarrationId).HasName("PK__NARRATIO__120CCE12ED0C7863");

            entity.ToTable("NARRATIONS");

            entity.Property(e => e.NarrationId).HasColumnName("narration_id");
            entity.Property(e => e.AudioUrl)
                .HasMaxLength(255)
                .HasColumnName("audio_url");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.LanguageId).HasColumnName("language_id");
            entity.Property(e => e.PoiId).HasColumnName("poi_id");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");

            entity.HasOne(d => d.Language).WithMany(p => p.Narrations)
                .HasForeignKey(d => d.LanguageId)
                .HasConstraintName("FK__NARRATION__langu__571DF1D5");

            entity.HasOne(d => d.Poi).WithMany(p => p.Narrations)
                .HasForeignKey(d => d.PoiId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__NARRATION__poi_i__5629CD9C");
        });

        modelBuilder.Entity<Poi>(entity =>
        {
            entity.HasKey(e => e.PoiId).HasName("PK__POIS__6176E7ACFCB1CC62");

            entity.ToTable("POIS");

            entity.Property(e => e.PoiId).HasColumnName("poi_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Latitude)
                .HasColumnType("decimal(10, 8)")
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasColumnType("decimal(11, 8)")
                .HasColumnName("longitude");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Radius)
                .HasDefaultValue(15)
                .HasColumnName("radius");

            entity.HasOne(d => d.Owner).WithMany(p => p.Pois)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__POIS__owner_id__5070F446");
        });

        modelBuilder.Entity<Qrcode>(entity =>
        {
            entity.HasKey(e => e.QrId).HasName("PK__QRCODES__6CD5101BB1C61189");

            entity.ToTable("QRCODES");

            entity.Property(e => e.QrId).HasColumnName("qr_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.PoiId).HasColumnName("poi_id");
            entity.Property(e => e.QrCode)
                .HasMaxLength(255)
                .HasColumnName("qr_code");

            entity.HasOne(d => d.Poi).WithMany(p => p.Qrcodes)
                .HasForeignKey(d => d.PoiId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__QRCODES__poi_id__5EBF139D");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__REVIEWS__60883D9070147D1D");

            entity.ToTable("REVIEWS");

            entity.Property(e => e.ReviewId).HasColumnName("review_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.PoiId).HasColumnName("poi_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Poi).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.PoiId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__REVIEWS__poi_id__6383C8BA");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__REVIEWS__user_id__628FA481");
        });

        modelBuilder.Entity<TrackingLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__TRACKING__9E2397E02A1518A1");

            entity.ToTable("TRACKING_LOGS");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceId)
                .HasMaxLength(255)
                .HasColumnName("device_id");
            entity.Property(e => e.Latitude)
                .HasColumnType("decimal(10, 8)")
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasColumnType("decimal(11, 8)")
                .HasColumnName("longitude");
            entity.Property(e => e.PoiId).HasColumnName("poi_id");

            entity.HasOne(d => d.Poi).WithMany(p => p.TrackingLogs)
                .HasForeignKey(d => d.PoiId)
                .HasConstraintName("FK__TRACKING___poi_i__6754599E");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__USERS__B9BE370FAEE1BC63");

            entity.ToTable("USERS");

            entity.HasIndex(e => e.Email, "UQ__USERS__AB6E616473131A2C").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
