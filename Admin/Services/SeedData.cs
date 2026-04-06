using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Infrastructure.Entities;

namespace Admin.Services;

/// <summary>
/// Seed dữ liệu khởi tạo khi ứng dụng chạy lần đầu.
/// Được gọi trong Program.cs trước khi app.Run().
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        // Kiểm tra đích danh xem email admin này đã tồn tại trong DB chưa
        bool hasAdmin = await db.Users.AnyAsync(u => u.Email == "admin@vinhkhanh.vn");

        if (!hasAdmin)
        {
            // Tạo tài khoản Admin mặc định
            var admin = new User
            {
                Name = "Admin",
                Email = "admin@vinhkhanh.vn",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "Admin",
                IsActive = true
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync();

            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("  ★ Đã tạo tài khoản Admin mặc định:");
            Console.WriteLine("    Email:    admin@vinhkhanh.vn");
            Console.WriteLine("    Password: Admin@123");
            Console.WriteLine("    ⚠ Hãy đổi mật khẩu sau khi đăng nhập!");
            Console.WriteLine("═══════════════════════════════════════");
        }
    }
}