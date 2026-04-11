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
        // ── 1. Admin ───────────────────────────────────────────
        if (!await db.Users.AnyAsync(u => u.Email == "adminvk@gmail.com"))
        {
            var admin = new User
            {
                Name = "Admin",
                Email = "adminvk@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "Admin",
                IsActive = true
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync();

            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("  ★ Đã tạo tài khoản Admin mặc định:");
            Console.WriteLine("    Email:    adminvk@gmail.com");
            Console.WriteLine("    Password: Admin@123");
            Console.WriteLine("    ⚠ Hãy đổi mật khẩu sau khi đăng nhập!");
            Console.WriteLine("═══════════════════════════════════════");
        }

        // ── 2. Vendor (Chủ gian hàng) ─────────────────────────
        if (!await db.Users.AnyAsync(u => u.Email == "vendorvk@gmail.com"))
        {
            var vendor = new User
            {
                Name = "Vendor",
                Email = "vendorvk@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Vendor@123"),
                Role = "Vendor",
                IsActive = true
            };

            db.Users.Add(vendor);
            await db.SaveChangesAsync();

            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("  ★ Đã tạo tài khoản Vendor mặc định:");
            Console.WriteLine("    Email:    vendorvk@gmail.com");
            Console.WriteLine("    Password: Vendor@123");
            Console.WriteLine("    ⚠ Hãy đổi mật khẩu sau khi đăng nhập!");
            Console.WriteLine("═══════════════════════════════════════");
        }

        // ── 3. User (Người dùng thường) ────────────────────────
        if (!await db.Users.AnyAsync(u => u.Email == "uservk@gmail.com"))
        {
            var user = new User
            {
                Name = "User",
                Email = "uservk@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                Role = "User",
                IsActive = true
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("  ★ Đã tạo tài khoản User mặc định:");
            Console.WriteLine("    Email:    uservk@gmail.com");
            Console.WriteLine("    Password: User@123");
            Console.WriteLine("    ⚠ Hãy đổi mật khẩu sau khi đăng nhập!");
            Console.WriteLine("═══════════════════════════════════════");
        }
    }
}
