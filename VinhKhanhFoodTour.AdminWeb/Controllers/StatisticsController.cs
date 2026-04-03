using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhFoodTour.AdminWeb.Data;
using VinhKhanhFoodTour.AdminWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace VinhKhanhFoodTour.AdminWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly AdminDbContext _context;

    public StatisticsController(AdminDbContext context)
    {
        _context = context;
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDashboardStatistics()
    {
        var totalShops = await _context.ManagedShops.CountAsync(s => s.IsActive);
        var totalManagers = await _context.AdminUsers.CountAsync(u => u.Role == "ShopManager" && u.IsActive);
        var verifiedShops = await _context.ManagedShops.CountAsync(s => s.IsVerified && s.IsActive);
        
        var topShops = await _context.ManagedShops
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.AverageRating)
            .Take(10)
            .Select(s => new { s.ShopId, s.Name, s.AverageRating, s.TotalOrders })
            .ToListAsync();

        var recentShops = await _context.ManagedShops
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .Take(5)
            .Select(s => new { s.ShopId, s.Name, s.CreatedAt, s.IsVerified })
            .ToListAsync();

        return Ok(new
        {
            summary = new
            {
                totalShops,
                totalManagers,
                verifiedShops,
                activeShops = totalShops
            },
            topShops,
            recentShops
        });
    }

    [HttpGet("shop/{shopId}")]
    public async Task<IActionResult> GetShopStatistics(int shopId)
    {
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userRole == "ShopManager" && userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            var userShop = await _context.AdminUsers
                .Where(u => u.UserId == userId)
                .Select(u => u.ManagedShopId)
                .FirstOrDefaultAsync();

            if (userShop != shopId)
                return Forbid();
        }

        var shop = await _context.ManagedShops.FindAsync(shopId);
        if (shop == null)
            return NotFound();

        var statistics = await _context.ShopStatistics
            .Where(s => s.ShopId == shopId)
            .OrderByDescending(s => s.StatisticsDate)
            .Take(30)
            .ToListAsync();

        return Ok(new
        {
            shop = new { shop.ShopId, shop.Name, shop.Address },
            statistics = statistics.Select(s => new
            {
                s.TotalVisits,
                s.TotalOrders,
                s.TotalRevenue,
                s.AverageRating,
                s.ReviewCount,
                s.StatisticsDate
            })
        });
    }

    [HttpPost("shop/{shopId}/record")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RecordStatistics(int shopId, [FromBody] RecordStatisticsRequest request)
    {
        var shop = await _context.ManagedShops.FindAsync(shopId);
        if (shop == null)
            return NotFound();

        var statistic = new ShopStatistics
        {
            ShopId = shopId,
            ShopName = shop.Name,
            TotalVisits = request.TotalVisits,
            TotalOrders = request.TotalOrders,
            TotalRevenue = request.TotalRevenue,
            AverageRating = request.AverageRating,
            ReviewCount = request.ReviewCount,
            StatisticsDate = DateTime.UtcNow.Date
        };

        _context.ShopStatistics.Add(statistic);

        // Update shop's current statistics
        shop.TotalOrders = request.TotalOrders;
        shop.AverageRating = request.AverageRating;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Ghi nhận thống kê thành công" });
    }

    [HttpGet("audit-logs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
    {
        var totalLogs = await _context.AuditLogs.CountAsync();
        var logs = await _context.AuditLogs
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            total = totalLogs,
            pageNumber,
            pageSize,
            logs = logs.Select(l => new
            {
                l.AuditId,
                l.UserId,
                l.Action,
                l.EntityType,
                l.EntityId,
                l.IpAddress,
                l.CreatedAt
            })
        });
    }
}

public class RecordStatisticsRequest
{
    public int TotalVisits { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
