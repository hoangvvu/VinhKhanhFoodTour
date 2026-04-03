using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhFoodTour.AdminWeb.Services;

namespace VinhKhanhFoodTour.AdminWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AuthController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email và mật khẩu là bắt buộc" });

        var (success, token, message) = await _authService.LoginAsync(request.Email, request.Password);

        if (!success)
            return Unauthorized(new { message });

        return Ok(new { token, message });
    }

    [HttpPost("register-admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Tên, email và mật khẩu là bắt buộc" });

        if (request.Password.Length < 6)
            return BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự" });

        var (success, msg) = await _authService.RegisterAdminAsync(request.Name, request.Email, request.Password);
        return success ? Ok(new { message = msg }) : BadRequest(new { message = msg });
    }

    [HttpPost("register-shop-manager")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterShopManager([FromBody] RegisterShopManagerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Tên, email, mật khẩu là bắt buộc" });

        if (request.Password.Length < 6)
            return BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự" });

        var (success, msg) = await _authService.RegisterShopManagerAsync(request.Name, request.Email, request.Password, request.ShopId);
        return success ? Ok(new { message = msg }) : BadRequest(new { message = msg });
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.UserId,
            user.Name,
            user.Email,
            user.Role,
            user.IsActive,
            user.LastLogin,
            ManagedShop = user.ManagedShop != null ? new
            {
                user.ManagedShop.ShopId,
                user.ManagedShop.Name,
                user.ManagedShop.Address
            } : null
        });
    }
}

public class LoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class RegisterRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class RegisterShopManagerRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public int ShopId { get; set; }
}
