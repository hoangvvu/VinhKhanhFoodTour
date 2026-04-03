using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhFoodTour.AdminWeb.Data;
using VinhKhanhFoodTour.AdminWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace VinhKhanhFoodTour.AdminWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShopsController : ControllerBase
{
    private readonly AdminDbContext _context;

    public ShopsController(AdminDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllShops()
    {
        var shops = await _context.ManagedShops
            .Include(s => s.Managers)
            .ToListAsync();

        return Ok(shops.Select(s => new
        {
            s.ShopId,
            s.Name,
            s.Address,
            s.Phone,
            s.IsVerified,
            s.IsActive,
            s.AverageRating,
            s.TotalOrders,
            ManagerCount = s.Managers.Count,
            CreatedAt = s.CreatedAt
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetShop(int id)
    {
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userRole == "ShopManager" && userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            var userShop = await _context.AdminUsers
                .Where(u => u.UserId == userId)
                .Select(u => u.ManagedShopId)
                .FirstOrDefaultAsync();

            if (userShop != id)
                return Forbid();
        }

        var shop = await _context.ManagedShops
            .Include(s => s.Managers)
            .FirstOrDefaultAsync(s => s.ShopId == id);

        if (shop == null)
            return NotFound();

        return Ok(new
        {
            shop.ShopId,
            shop.Name,
            shop.Address,
            shop.Phone,
            shop.Description,
            shop.Latitude,
            shop.Longitude,
            shop.Radius,
            shop.IsVerified,
            shop.IsActive,
            shop.AverageRating,
            shop.TotalOrders,
            Managers = shop.Managers.Select(m => new
            {
                m.UserId,
                m.Name,
                m.Email,
                m.LastLogin
            }),
            CreatedAt = shop.CreatedAt,
            UpdatedAt = shop.UpdatedAt
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateShop([FromBody] CreateShopRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Tên cửa hàng là bắt buộc" });

        var shop = new ManagedShop
        {
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            Description = request.Description,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Radius = request.Radius,
            IsActive = true
        };

        _context.ManagedShops.Add(shop);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetShop), new { id = shop.ShopId }, new { shop.ShopId, shop.Name });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,ShopManager")]
    public async Task<IActionResult> UpdateShop(int id, [FromBody] UpdateShopRequest request)
    {
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        var shop = await _context.ManagedShops.FindAsync(id);
        if (shop == null)
            return NotFound();

        if (userRole == "ShopManager" && userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            var userShop = await _context.AdminUsers
                .Where(u => u.UserId == userId)
                .Select(u => u.ManagedShopId)
                .FirstOrDefaultAsync();

            if (userShop != id)
                return Forbid();
        }

        shop.Name = request.Name ?? shop.Name;
        shop.Address = request.Address ?? shop.Address;
        shop.Phone = request.Phone ?? shop.Phone;
        shop.Description = request.Description ?? shop.Description;
        if (request.Latitude.HasValue) shop.Latitude = request.Latitude;
        if (request.Longitude.HasValue) shop.Longitude = request.Longitude;
        if (request.Radius.HasValue) shop.Radius = request.Radius;
        shop.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật cửa hàng thành công" });
    }

    [HttpPut("{id}/verify")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> VerifyShop(int id)
    {
        var shop = await _context.ManagedShops.FindAsync(id);
        if (shop == null)
            return NotFound();

        shop.IsVerified = true;
        shop.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Xác minh cửa hàng thành công" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateShop(int id)
    {
        var shop = await _context.ManagedShops.FindAsync(id);
        if (shop == null)
            return NotFound();

        shop.IsActive = false;
        shop.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Vô hiệu hóa cửa hàng thành công" });
    }
}

public class CreateShopRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? Radius { get; set; }
}

public class UpdateShopRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? Radius { get; set; }
}
