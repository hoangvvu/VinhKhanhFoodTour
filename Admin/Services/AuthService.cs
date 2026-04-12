using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Infrastructure.Entities;

namespace Admin.Services;

public class AuthService
{
    private readonly ApplicationDbContext _db;

    public AuthService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Xác thực đăng nhập bằng email + password.
    /// Trả về User nếu đúng, null nếu sai.
    /// </summary>
    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        // Tìm user theo email (không phân biệt hoa thường)
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email.Trim().ToLower()
                && u.IsActive);

        if (user is null)
        {
            Console.WriteLine($"---> KHÔNG tìm thấy user với email: {email}");
            return null;
        }
        // So sánh password với hash trong DB
        // BCrypt.Verify tự động xử lý salt
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

        return isPasswordValid ? user : null;
    }

    /// <summary>
    /// Lấy thông tin user theo ID (dùng sau khi đã đăng nhập).
    /// </summary>
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    /// <summary>
    /// Lấy tất cả Users (dành cho trang quản lý nhân sự).
    /// </summary>
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _db.Users
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Tạo user mới (Admin tạo tài khoản cho Vendor).
    /// Password sẽ được hash bằng BCrypt trước khi lưu.
    /// </summary>
    public async Task<User> CreateUserAsync(string name, string email, string password, string role)
    {
        var user = new User
        {
            Name = name,
            Email = email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Kiểm tra email đã tồn tại chưa.
    /// </summary>
    public async Task<bool> IsEmailExistsAsync(string email, int excludeId = 0)
    {
        return await _db.Users.AnyAsync(u =>
            u.Email.ToLower() == email.Trim().ToLower()
            && u.UserId != excludeId);
    }

    public async Task<User?> GetUserByEmailAsync(string email) =>
        await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email.Trim().ToLower()
                && u.IsActive);

    /// <summary>Đăng nhập Google: tìm theo email; nếu chưa có thì tạo vendor + gian hàng. Admin hiện có giữ vai trò Admin.</summary>
    public async Task<User> FindOrCreateUserFromGoogleAsync(string email, string? displayName)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);

        if (user is not null)
        {
            if (!string.IsNullOrWhiteSpace(displayName) && user.Name != displayName)
            {
                user.Name = displayName.Trim();
                user.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            if (string.Equals(user.Role, "Vendor", StringComparison.OrdinalIgnoreCase))
                await EnsureVendorPoiExistsAsync(user.UserId, user.Name);
            return user;
        }

        var name = string.IsNullOrWhiteSpace(displayName)
            ? normalized.Split('@')[0]
            : displayName.Trim();

        user = new User
        {
            Name = name,
            Email = normalized,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
            Role = "Vendor",
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await EnsureVendorPoiExistsAsync(user.UserId, user.Name);
        return user;
    }

    private async Task EnsureVendorPoiExistsAsync(int userId, string stallName)
    {
        if (await _db.Pois.AnyAsync(p => p.OwnerId == userId))
            return;

        _db.Pois.Add(new Poi
        {
            OwnerId = userId,
            Name = string.IsNullOrWhiteSpace(stallName) ? "Quán của tôi" : stallName.Trim(),
            Address = "",
            Latitude = 10.7578m,
            Longitude = 106.7095m,
            Radius = 20,
            Priority = 3,
            IsActive = true
        });
        await _db.SaveChangesAsync();
    }
}