using Microsoft.EntityFrameworkCore;
using VKFoodTour.Admin.Models; // Đảm bảo bạn đã tạo folder Models và file Poi.cs trước đó

namespace VKFoodTour.Admin.Data;

public class ApplicationDbContext : DbContext
{
    // Constructor này giúp truyền cấu hình (chuỗi kết nối) từ Program.cs vào
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSet này đại diện cho bảng "Pois" trong SQL Server của bạn
    // Nếu bạn có thêm bảng Users hay Feedbacks, bạn sẽ thêm các DbSet tương tự ở đây
    public DbSet<Poi> Pois { get; set; }

    // (Tùy chọn) Dùng để cấu hình chi tiết các cột trong bảng (như khóa chính, độ dài chữ...)
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ví dụ: Đảm bảo tên quán ăn không được để trống khi lưu
        modelBuilder.Entity<Poi>()
            .Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);
    }
}