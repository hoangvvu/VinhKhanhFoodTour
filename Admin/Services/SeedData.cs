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

        // ── 1b. Ngôn ngữ thuyết minh (bắt buộc cho TTS / Narration) ──
        if (!await db.Languages.AnyAsync())
        {
            db.Languages.AddRange(
                new Language { Code = "vi", Name = "Tiếng Việt", IsActive = true },
                new Language { Code = "en", Name = "English", IsActive = true });
            await db.SaveChangesAsync();
        }

        // ── 2. Vendor (Chủ gian hàng) ─────────────────────────
        User? vendorUser = null;
        if (!await db.Users.AnyAsync(u => u.Email == "vendorvk@gmail.com"))
        {
            vendorUser = new User
            {
                Name = "Vendor",
                Email = "vendorvk@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Vendor@123"),
                Role = "Vendor",
                IsActive = true
            };

            db.Users.Add(vendorUser);
            await db.SaveChangesAsync();

            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("  ★ Đã tạo tài khoản Vendor mặc định:");
            Console.WriteLine("    Email:    vendorvk@gmail.com");
            Console.WriteLine("    Password: Vendor@123");
            Console.WriteLine("    ⚠ Hãy đổi mật khẩu sau khi đăng nhập!");
            Console.WriteLine("═══════════════════════════════════════");
        }
        else
        {
            vendorUser = await db.Users.FirstAsync(u => u.Email == "vendorvk@gmail.com");
        }

        if (vendorUser is not null && !await db.Pois.AnyAsync(p => p.OwnerId == vendorUser.UserId))
        {
            db.Pois.Add(new Poi
            {
                OwnerId = vendorUser.UserId,
                Name = vendorUser.Name,
                Address = "534 Vĩnh Khánh (mẫu)",
                Latitude = 10.7578m,
                Longitude = 106.7095m,
                Radius = 20,
                Priority = 2,
                IsActive = true,
                Description = "Gian hàng mẫu — chỉnh sửa tại trang Mô tả & hình ảnh."
            });
            await db.SaveChangesAsync();
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
