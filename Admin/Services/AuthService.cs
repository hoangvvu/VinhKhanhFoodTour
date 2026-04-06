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
}