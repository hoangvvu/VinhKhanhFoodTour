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
    /// Tạo user thuộc Role Vendor và tạo luôn POI tương ứng trong cùng 1 transaction.
    /// </summary>
    public async Task<User> CreateVendorWithPoiAsync(string name, string email, string password, string poiName)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var user = new User
            {
                Name = name,
                Email = email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "Vendor",
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(); // Lấy được user.UserId

            var poi = new Poi
            {
                OwnerId = user.UserId,
                Name = string.IsNullOrWhiteSpace(poiName) ? $"{name}'s Stall" : poiName.Trim(),
                Address = null,
                Latitude = 10.7578m,
                Longitude = 106.7095m,
                Radius = 20, // default
                IsActive = false,
                Status = "Pending"
            };

            _db.Pois.Add(poi);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
            return user;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
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

    /// <summary>Ẩn / kích hoạt tài khoản (Admin).</summary>
    public async Task SetUserActiveAsync(int userId, bool isActive)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null)
            return;

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
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
            if (!user.IsActive)
                throw new InvalidOperationException("ACCOUNT_DISABLED");

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
            IsActive = false,
            Status = "Pending"
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>Cập nhật gói thành viên cho vendor.</summary>
    public async Task UpdateMembershipTierAsync(int userId, string newTier)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null) return;
        
        user.MembershipTier = newTier;
        user.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    /// <summary>Xóa vĩnh viễn user và tất cả POI kèm theo (narrations, QR, images cascade).</summary>
    public async Task DeleteUserAsync(int userId)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var poiIds = await _db.Pois.Where(p => p.OwnerId == userId).Select(p => p.PoiId).ToListAsync();
            if (poiIds.Any())
            {
                var narrations = await _db.Narrations.Where(n => poiIds.Contains(n.PoiId)).ToListAsync();
                if (narrations.Any()) _db.Narrations.RemoveRange(narrations);

                var qrCodes = await _db.QrCodes.Where(q => poiIds.Contains(q.PoiId)).ToListAsync();
                if (qrCodes.Any()) _db.QrCodes.RemoveRange(qrCodes);

                var foodIds = await _db.Foods.Where(f => poiIds.Contains(f.PoiId)).Select(f => f.FoodId).ToListAsync();
                if (foodIds.Any())
                {
                    var foodTranslations = await _db.FoodTranslations.Where(ft => foodIds.Contains(ft.FoodId)).ToListAsync();
                    if (foodTranslations.Any()) _db.FoodTranslations.RemoveRange(foodTranslations);

                    var foodImages = await _db.Images.Where(i => i.FoodId != null && foodIds.Contains(i.FoodId!.Value)).ToListAsync();
                    if (foodImages.Any()) _db.Images.RemoveRange(foodImages);

                    var foods = await _db.Foods.Where(f => poiIds.Contains(f.PoiId)).ToListAsync();
                    _db.Foods.RemoveRange(foods);
                }

                var poiImages = await _db.Images.Where(i => poiIds.Contains(i.PoiId)).ToListAsync();
                if (poiImages.Any()) _db.Images.RemoveRange(poiImages);

                var menuItems = await _db.MenuItems.Where(m => poiIds.Contains(m.PoiId)).ToListAsync();
                if (menuItems.Any()) _db.MenuItems.RemoveRange(menuItems);

                var reviews = await _db.Reviews.Where(r => poiIds.Contains(r.PoiId)).ToListAsync();
                if (reviews.Any()) _db.Reviews.RemoveRange(reviews);

                var trackingLogs = await _db.TrackingLogs.Where(t => poiIds.Contains(t.PoiId)).ToListAsync();
                if (trackingLogs.Any()) _db.TrackingLogs.RemoveRange(trackingLogs);

                var pois = await _db.Pois.Where(p => poiIds.Contains(p.PoiId)).ToListAsync();
                _db.Pois.RemoveRange(pois);
                
                await _db.SaveChangesAsync();
            }

            // Xóa user
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is not null)
            {
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}