using VinhKhanhFoodTour.AdminWeb.Data;
using VinhKhanhFoodTour.AdminWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace VinhKhanhFoodTour.AdminWeb.Services;

public interface IAuthenticationService
{
    Task<(bool Success, string Token, string Message)> LoginAsync(string email, string password);
    Task<(bool Success, string Message)> RegisterAdminAsync(string name, string email, string password);
    Task<(bool Success, string Message)> RegisterShopManagerAsync(string name, string email, string password, int shopId);
    Task<AdminUser?> GetUserByIdAsync(int userId);
    Task<AdminUser?> GetUserByEmailAsync(string email);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly AdminDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthenticationService(AdminDbContext context, IPasswordService passwordService, IJwtTokenService jwtTokenService)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<(bool Success, string Token, string Message)> LoginAsync(string email, string password)
    {
        var user = await _context.AdminUsers
            .Include(u => u.ManagedShop)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return (false, "", "Email hoặc mật khẩu không chính xác");

        if (!user.IsActive)
            return (false, "", "Tài khoản đã bị vô hiệu hóa");

        if (!_passwordService.VerifyPassword(password, user.Password ?? ""))
            return (false, "", "Email hoặc mật khẩu không chính xác");

        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtTokenService.GenerateToken(
            user.UserId,
            user.Email ?? "",
            user.Role,
            user.ManagedShop?.Name
        );

        return (true, token, "Đăng nhập thành công");
    }

    public async Task<(bool Success, string Message)> RegisterAdminAsync(string name, string email, string password)
    {
        if (await _context.AdminUsers.AnyAsync(u => u.Email == email))
            return (false, "Email đã tồn tại");

        var hashedPassword = _passwordService.HashPassword(password);
        var adminUser = new AdminUser
        {
            Name = name,
            Email = email,
            Password = hashedPassword,
            Role = "Admin",
            IsActive = true
        };

        _context.AdminUsers.Add(adminUser);
        await _context.SaveChangesAsync();

        return (true, "Tạo tài khoản Admin thành công");
    }

    public async Task<(bool Success, string Message)> RegisterShopManagerAsync(string name, string email, string password, int shopId)
    {
        if (await _context.AdminUsers.AnyAsync(u => u.Email == email))
            return (false, "Email đã tồn tại");

        var shop = await _context.ManagedShops.FindAsync(shopId);
        if (shop == null)
            return (false, "Cửa hàng không tồn tại");

        var hashedPassword = _passwordService.HashPassword(password);
        var shopManager = new AdminUser
        {
            Name = name,
            Email = email,
            Password = hashedPassword,
            Role = "ShopManager",
            ManagedShopId = shopId,
            IsActive = true
        };

        _context.AdminUsers.Add(shopManager);
        await _context.SaveChangesAsync();

        return (true, "Tạo tài khoản Quản lý cửa hàng thành công");
    }

    public async Task<AdminUser?> GetUserByIdAsync(int userId)
    {
        return await _context.AdminUsers
            .Include(u => u.ManagedShop)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<AdminUser?> GetUserByEmailAsync(string email)
    {
        return await _context.AdminUsers
            .Include(u => u.ManagedShop)
            .FirstOrDefaultAsync(u => u.Email == email);
    }
}
