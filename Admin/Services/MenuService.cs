using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Infrastructure.Entities;
using MenuItem = VKFoodTour.Infrastructure.Entities.MenuItem;

namespace Admin.Services;

public class MenuService
{
    private readonly ApplicationDbContext _db;

    public MenuService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Lấy danh sách món ăn của một quán cụ thể
    public async Task<List<MenuItem>> GetByPoiIdAsync(int poiId)
    {
        return await _db.MenuItems
            .Where(m => m.PoiId == poiId)
            .OrderByDescending(m => m.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    // Thêm món mới — trả về entity đã có ItemId từ DB
    public async Task<MenuItem> AddItemAsync(MenuItem item)
    {
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    // Cập nhật món đã có — Find tracked entity rồi copy properties
    public async Task<bool> UpdateItemAsync(int itemId, string name, decimal price, 
        string category, string? description, string? imageUrl)
    {
        var existing = await _db.MenuItems.FindAsync(itemId);
        if (existing == null) return false;

        existing.Name = name;
        existing.Price = price;
        existing.Category = category;
        existing.Description = description;
        existing.ImageUrl = imageUrl;

        await _db.SaveChangesAsync();
        return true;
    }

    // Đổi trạng thái Ẩn/Hiện
    public async Task ToggleStatusAsync(int itemId)
    {
        var item = await _db.MenuItems.FindAsync(itemId);
        if (item != null)
        {
            item.Status = (item.Status == "AVAILABLE") ? "HIDDEN" : "AVAILABLE";
            await _db.SaveChangesAsync();
        }
    }
}
