using Hangfire.Dashboard;
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
            .ToListAsync();
    }

    // Thêm hoặc Cập nhật món ăn
    public async Task SaveItemAsync(MenuItem item)
    {
        if (item.ItemId == 0)
            _db.MenuItems.Add(item);
        else
            _db.MenuItems.Update(item);

        await _db.SaveChangesAsync();
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