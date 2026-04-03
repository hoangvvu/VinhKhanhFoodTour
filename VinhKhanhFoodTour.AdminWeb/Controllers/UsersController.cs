using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhFoodTour.AdminWeb.Data;
using VinhKhanhFoodTour.AdminWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace VinhKhanhFoodTour.AdminWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly AdminDbContext _context;

    public UsersController(AdminDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.AdminUsers
            .Include(u => u.ManagedShop)
            .ToListAsync();

        return Ok(users.Select(u => new
        {
            u.UserId,
            u.Name,
            u.Email,
            u.Role,
            u.IsActive,
            u.LastLogin,
            ManagedShop = u.ManagedShop != null ? new { u.ManagedShop.ShopId, u.ManagedShop.Name } : null,
            CreatedAt = u.CreatedAt
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.AdminUsers
            .Include(u => u.ManagedShop)
            .FirstOrDefaultAsync(u => u.UserId == id);

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
            ManagedShop = user.ManagedShop != null ? new { user.ManagedShop.ShopId, user.ManagedShop.Name } : null,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var user = await _context.AdminUsers.FindAsync(id);
        if (user == null)
            return NotFound();

        user.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Vô hiệu hóa người dùng thành công" });
    }

    [HttpPut("{id}/activate")]
    public async Task<IActionResult> ActivateUser(int id)
    {
        var user = await _context.AdminUsers.FindAsync(id);
        if (user == null)
            return NotFound();

        user.IsActive = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Kích hoạt người dùng thành công" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.AdminUsers.FindAsync(id);
        if (user == null)
            return NotFound();

        _context.AdminUsers.Remove(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Xóa người dùng thành công" });
    }
}
