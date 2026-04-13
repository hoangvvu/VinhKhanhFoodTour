using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
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

    public async Task<List<User>> GetActiveVendorsAsync()
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role == "Vendor")
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy danh sách POI theo owner
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

    // ============================================================
    // MỚI: Lấy 1 POI duy nhất cho trang ThongTinQuan.razor
    // ============================================================
    public async Task<Poi?> GetPoiByOwnerIdAsync(int ownerId)
    {
        return await _db.Pois
            .Include(p => p.QrCodes)
            .Include(p => p.Images)
            .Include(p => p.Narrations)
            .ThenInclude(n => n.Language)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OwnerId == ownerId);
    }

    /// <summary>Tạo gian hàng mặc định nếu vendor chưa có POI (đăng nhập Gmail hoặc tài khoản mới).</summary>
    public async Task<Poi> EnsureVendorPoiAsync(int userId, string defaultStallName)
    {
        var existing = await _db.Pois.FirstOrDefaultAsync(p => p.OwnerId == userId);
        if (existing is not null)
            return existing;

        var name = string.IsNullOrWhiteSpace(defaultStallName) ? "Quán của tôi" : defaultStallName.Trim();
        var poi = new Poi
        {
            OwnerId = userId,
            Name = name,
            Address = "",
            Latitude = 10.7578m,
            Longitude = 106.7095m,
            Radius = 20,
            Priority = 3,
            IsActive = true
        };
        _db.Pois.Add(poi);
        await _db.SaveChangesAsync();
        return poi;
    }

    public async Task<List<Image>> GetStallGalleryAsync(int poiId, int ownerUserId)
    {
        var ok = await _db.Pois.AnyAsync(p => p.PoiId == poiId && p.OwnerId == ownerUserId);
        if (!ok)
            return new List<Image>();

        return await _db.Images
            .AsNoTracking()
            .Where(i => i.PoiId == poiId && i.FoodId == null && !i.IsCover)
            .OrderBy(i => i.SortOrder)
            .ToListAsync();
    }

    public async Task<bool> AddStallGalleryImageAsync(int poiId, int ownerUserId, string imageUrl)
    {
        var poi = await _db.Pois.FirstOrDefaultAsync(p => p.PoiId == poiId && p.OwnerId == ownerUserId);
        if (poi is null)
            return false;

        var maxOrder = await _db.Images
            .Where(i => i.PoiId == poiId && i.FoodId == null && !i.IsCover)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync() ?? 0;

        _db.Images.Add(new Image
        {
            PoiId = poiId,
            FoodId = null,
            ImageUrl = imageUrl,
            IsCover = false,
            SortOrder = maxOrder + 1
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveStallGalleryImageAsync(int imageId, int ownerUserId)
    {
        var img = await _db.Images
            .Include(i => i.Poi)
            .FirstOrDefaultAsync(i => i.ImageId == imageId);

        if (img is null || img.Poi?.OwnerId != ownerUserId || img.IsCover)
            return false;

        _db.Images.Remove(img);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Tạo mã QR đầu tiên cho quán nếu chưa có (token dạng VK-XXXXXXXX).</summary>
    public async Task<string?> EnsurePrimaryQrForVendorAsync(int poiId, int ownerUserId, string? locationNote = null)
    {
        var poi = await _db.Pois
            .Include(p => p.QrCodes)
            .FirstOrDefaultAsync(p => p.PoiId == poiId && p.OwnerId == ownerUserId);
        if (poi is null)
            return null;

        var existing = poi.QrCodes.FirstOrDefault(q => q.IsActive);
        if (existing is not null)
            return existing.QrToken;

        string token;
        do
        {
            token = $"VK-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        } while (await _db.QrCodes.AnyAsync(q => q.QrToken == token));

        _db.QrCodes.Add(new QrCode
        {
            PoiId = poiId,
            QrToken = token,
            LocationNote = string.IsNullOrWhiteSpace(locationNote) ? "Quét tại quán" : locationNote.Trim(),
            IsActive = true
        });
        await _db.SaveChangesAsync();
        return token;
    }

    /// <summary>
    /// Tái tạo mã QR từ mô tả quán: mô tả thay đổi thì token đổi theo.
    /// Chỉ giữ 1 mã active mới nhất cho mỗi quán.
    /// </summary>
    public async Task<string?> RegenerateQrFromDescriptionAsync(int poiId, int ownerUserId, string? description, string? locationNote = null)
    {
        var poi = await _db.Pois
            .Include(p => p.QrCodes)
            .FirstOrDefaultAsync(p => p.PoiId == poiId && p.OwnerId == ownerUserId);
        if (poi is null)
            return null;

        var normalizedDesc = (description ?? string.Empty).Trim();
        var descHash = ComputeShortHash(normalizedDesc);
        var tokenBase = $"VK-{poiId:D3}-{descHash}";
        var token = tokenBase;
        var suffix = 1;
        while (await _db.QrCodes.AnyAsync(q => q.QrToken == token && q.PoiId != poiId))
        {
            token = $"{tokenBase}-{suffix++:D2}";
        }

        foreach (var qr in poi.QrCodes.Where(q => q.IsActive))
            qr.IsActive = false;

        _db.QrCodes.Add(new QrCode
        {
            PoiId = poiId,
            QrToken = token,
            LocationNote = string.IsNullOrWhiteSpace(locationNote) ? "Tại quán (tạo từ mô tả)" : locationNote.Trim(),
            IsActive = true
        });

        await _db.SaveChangesAsync();
        return token;
    }

    public async Task<bool> DeleteActiveQrForVendorAsync(int poiId, int ownerUserId)
    {
        var poi = await _db.Pois
            .Include(p => p.QrCodes)
            .FirstOrDefaultAsync(p => p.PoiId == poiId && p.OwnerId == ownerUserId);
        if (poi is null)
            return false;

        var activeList = poi.QrCodes.Where(q => q.IsActive).ToList();
        if (!activeList.Any())
            return true;

        _db.QrCodes.RemoveRange(activeList);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task UpsertNarrationAutoAsync(int poiId, int languageId, string title, string content, string? ttsVoice, string? autoAudioUrl)
    {
        var existing = await _db.Narrations.FirstOrDefaultAsync(n => n.PoiId == poiId && n.LanguageId == languageId);
        if (existing is not null)
        {
            existing.Title = title;
            existing.Content = content;
            existing.TtsVoice = ttsVoice;
            existing.AudioUrlAuto = autoAudioUrl;
            // Giữ tương thích ngược cho các màn hình/endpoint cũ.
            existing.AudioUrl = autoAudioUrl;
            existing.UpdatedAt = DateTime.Now;
            existing.IsActive = true;
        }
        else
        {
            _db.Narrations.Add(new Narration
            {
                PoiId = poiId,
                LanguageId = languageId,
                Title = title,
                Content = content,
                TtsVoice = ttsVoice,
                AudioUrlAuto = autoAudioUrl,
                AudioUrl = autoAudioUrl,
                IsActive = true
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task UpsertNarrationQrAsync(int poiId, int languageId, string title, string content, string? qrAudioUrl)
    {
        var existing = await _db.Narrations.FirstOrDefaultAsync(n => n.PoiId == poiId && n.LanguageId == languageId);
        if (existing is not null)
        {
            existing.Title = title;
            existing.Content = content;
            existing.AudioUrlQr = qrAudioUrl;
            existing.UpdatedAt = DateTime.Now;
            existing.IsActive = true;
        }
        else
        {
            _db.Narrations.Add(new Narration
            {
                PoiId = poiId,
                LanguageId = languageId,
                Title = title,
                Content = content,
                AudioUrlQr = qrAudioUrl,
                IsActive = true
            });
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>Ẩn gian hàng (không xóa bản ghi).</summary>
    public async Task<bool> HideStallAsync(int id)
    {
        var poi = await _db.Pois.FindAsync(id);
        if (poi is null)
            return false;

        poi.IsActive = false;
        poi.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return true;
    }

    // ============================================================
    // MỚI: Cập nhật thông tin quán từ giao diện Vendor
    // ============================================================
    public async Task<bool> UpdateStallInfoAsync(int id, string name, string desc, string addr, double lat, double lng, string imgUrl)
    {
        var poi = await _db.Pois.FindAsync(id);
        if (poi == null) return false;

        poi.Name = name;
        poi.Address = addr;
        poi.Latitude = (decimal)lat;   // Ép kiểu sang decimal
        poi.Longitude = (decimal)lng;  // Ép kiểu sang decimal
        poi.UpdatedAt = DateTime.Now;
        poi.Description = desc;

        if (!string.IsNullOrEmpty(imgUrl))
        {
            poi.ImageUrl = imgUrl;
        }

        await _db.SaveChangesAsync();
        return true;
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

    public async Task<int> GetVietnameseLanguageIdAsync()
    {
        var lang = await _db.Languages.AsNoTracking().FirstOrDefaultAsync(l => l.Code == "vi");
        if (lang is null)
            throw new InvalidOperationException("Thiếu ngôn ngữ 'vi' trong bảng LANGUAGES. Khởi động lại app để chạy Seed.");
        return lang.LanguageId;
    }

    private static string ComputeShortHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..8];
    }
}