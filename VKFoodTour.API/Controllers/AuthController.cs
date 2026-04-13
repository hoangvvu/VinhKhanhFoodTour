using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Infrastructure.Entities;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuthController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new AuthResponseDto { Success = false, Message = "Email và mật khẩu không được trống." });

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);
        if (user is null)
            return Unauthorized(new AuthResponseDto { Success = false, Message = "Sai email hoặc mật khẩu." });

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new AuthResponseDto { Success = false, Message = "Sai email hoặc mật khẩu." });

        if (!string.Equals(user.Role, "User", StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status403Forbidden,
                new AuthResponseDto { Success = false, Message = "Ứng dụng này chỉ dành cho tài khoản du khách." });

        return Ok(new AuthResponseDto
        {
            Success = true,
            Message = "Đăng nhập thành công.",
            User = ToUserDto(user)
        });
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AuthResponseDto { Success = false, Message = "Vui lòng nhập đầy đủ thông tin." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await _context.Users.AnyAsync(u => u.Email.ToLower() == email);
        if (exists)
            return Conflict(new AuthResponseDto { Success = false, Message = "Email đã tồn tại." });

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new AuthResponseDto
        {
            Success = true,
            Message = "Đăng ký thành công.",
            User = ToUserDto(user)
        });
    }

    private static AuthUserDto ToUserDto(User user) =>
        new()
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        };
}
