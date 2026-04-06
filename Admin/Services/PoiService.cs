using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Infrastructure.Entities;

namespace Admin.Services;

public class PoiService
{
    private readonly ApplicationDbContext _db;

    public PoiService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Poi>> GetAllAsync()
    {
        return await _db.Pois
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Lấy POI theo owner (dùng cho Vendor — chỉ thấy POI của mình).
    /// </summary>
    public async Task<List<Poi>> GetByOwnerAsync(int ownerId)
    {
        return await _db.Pois
            .Where(p => p.OwnerId == ownerId)
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Poi>> GetActiveAsync()
    {
        return await _db.Pois
            .Where(p => p.IsActive)
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Poi?> GetByIdAsync(int id)
    {
        return await _db.Pois
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PoiId == id);
    }

    public async Task<Poi> CreateAsync(Poi poi)
    {
        poi.UpdatedAt = null;
        _db.Pois.Add(poi);
        await _db.SaveChangesAsync();
        return poi;
    }

    public async Task<bool> UpdateAsync(Poi poi)
    {
        var existing = await _db.Pois.FindAsync(poi.PoiId);
        if (existing is null) return false;

        existing.Name = poi.Name;
        existing.Address = poi.Address;
        existing.Phone = poi.Phone;
        existing.Latitude = poi.Latitude;
        existing.Longitude = poi.Longitude;
        existing.Radius = poi.Radius;
        existing.Priority = poi.Priority;
        existing.OwnerId = poi.OwnerId;
        existing.IsActive = poi.IsActive;
        existing.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var poi = await _db.Pois.FindAsync(id);
        if (poi is null) return false;

        _db.Pois.Remove(poi);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var poi = await _db.Pois.FindAsync(id);
        if (poi is null) return false;

        poi.IsActive = !poi.IsActive;
        poi.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountAsync() =>
        await _db.Pois.CountAsync();

    public async Task<int> CountActiveAsync() =>
        await _db.Pois.CountAsync(p => p.IsActive);

    public async Task<bool> IsNameExistsAsync(string name, int excludeId = 0) =>
        await _db.Pois.AnyAsync(p => p.Name == name && p.PoiId != excludeId);
}