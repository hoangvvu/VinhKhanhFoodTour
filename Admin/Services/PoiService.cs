using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Infrastructure.Entities;

namespace Admin.Services;

public class PoiService
{
    public const string MasterTourQrToken = "VINH-KHANH-TOUR";
    private readonly ApplicationDbContext _db;
    private readonly GoogleTranslateService _translate;
    private readonly TtsService _tts;

    public PoiService(ApplicationDbContext db, GoogleTranslateService translate, TtsService tts)
    {
        _db = db;
        _translate = translate;
        _tts = tts;
    }

    public async Task<List<Language>> GetActiveLanguagesAsync()
    {
        return await _db.Languages
            .AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.Code == "vi" ? 0 : 1)
            .ThenBy(l => l.Code)
            .ToListAsync();
    }

    public async Task<List<Poi>> GetAllAsync()
    {
        return await _db.Pois
            .OrderByDescending(p => p.Priority)
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
            .OrderByDescending(p => p.Priority)
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
            IsActive = false,
            Status = "Pending"
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

        // TỰ ĐỘNG HÓA: Cập nhật thuyết minh đa ngôn ngữ ngay khi có QR mới
        _ = Task.Run(() => UpdateMultilingualNarrationsAsync(poiId));

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
        // UNIQUE trên qr_token là toàn bảng — gồm cả bản ghi cũ IsActive=0 của chính POI này.
        while (await _db.QrCodes.AnyAsync(q => q.QrToken == token))
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

        // TỰ ĐỘNG HÓA: Cập nhật thuyết minh đa ngôn ngữ ngay khi có QR mới
        _ = Task.Run(() => UpdateMultilingualNarrationsAsync(poiId));

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

    /// <summary>Xoa toan bo QR hien co trong he thong (Admin).</summary>
    public async Task<int> DeleteAllQrAsync()
    {
        var all = await _db.QrCodes.ToListAsync();
        if (all.Count == 0)
            return 0;

        _db.QrCodes.RemoveRange(all);
        await _db.SaveChangesAsync();
        return all.Count;
    }

    /// <summary>Tao 1 QR duy nhat cho tour tong, do admin quan ly.</summary>
    public async Task<string?> EnsureMasterTourQrAsync()
    {
        var existing = await _db.QrCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.QrToken == MasterTourQrToken && q.IsActive);
        if (existing is not null)
            return existing.QrToken;

        var anchorPoi = await _db.Pois
            .AsNoTracking()
            .Where(p => p.IsActive && p.OwnerId != null)
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.PoiId)
            .FirstOrDefaultAsync();

        if (anchorPoi is null)
            return null;

        _db.QrCodes.Add(new QrCode
        {
            PoiId = anchorPoi.PoiId,
            QrToken = MasterTourQrToken,
            LocationNote = "QR tong tour do Admin quan ly",
            IsActive = true
        });

        await _db.SaveChangesAsync();
        return MasterTourQrToken;
    }

    public async Task UpsertNarrationAutoAsync(int poiId, int languageId, string title, string content, string? ttsVoice, string? autoAudioUrl)
    {
        title = TruncateForDb(string.IsNullOrWhiteSpace(title) ? $"Gian hàng #{poiId}" : title, 200);
        content = string.IsNullOrWhiteSpace(content) ? " " : content;
        ttsVoice = string.IsNullOrWhiteSpace(ttsVoice) ? null : TruncateForDb(ttsVoice, 100);
        autoAudioUrl = string.IsNullOrWhiteSpace(autoAudioUrl) ? null : TruncateForDb(autoAudioUrl, 2048);

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
        title = TruncateForDb(string.IsNullOrWhiteSpace(title) ? $"Gian hàng #{poiId}" : title, 200);
        content = string.IsNullOrWhiteSpace(content) ? " " : content;
        qrAudioUrl = string.IsNullOrWhiteSpace(qrAudioUrl) ? null : TruncateForDb(qrAudioUrl, 2048);

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

    /// <summary>Xoá audio thuyết minh tự động (Auto/Geo) cho POI.</summary>
    public async Task<bool> DeleteNarrationAutoAudioAsync(int poiId)
    {
        var narration = await _db.Narrations
            .FirstOrDefaultAsync(n => n.PoiId == poiId && n.IsActive);
        if (narration is null)
            return false;

        narration.AudioUrlAuto = null;
        narration.AudioUrl = null;
        narration.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Xoá audio dành riêng cho quét QR của POI.</summary>
    public async Task<bool> DeleteNarrationQrAudioAsync(int poiId)
    {
        var narration = await _db.Narrations
            .FirstOrDefaultAsync(n => n.PoiId == poiId && n.IsActive);
        if (narration is null)
            return false;

        narration.AudioUrlQr = null;
        narration.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return true;
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
        poi.Latitude = (decimal)lat;
        poi.Longitude = (decimal)lng;
        poi.Description = desc;
        poi.ImageUrl = imgUrl;
        poi.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RequestApprovalAsync(int poiId)
    {
        var poi = await _db.Pois.FindAsync(poiId);
        if (poi is null) return false;

        poi.Status = "Pending";
        poi.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Poi>> GetActiveAsync()
    {
        return await _db.Pois
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Priority)
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
        poi.Status ??= "Pending";
        poi.IsActive = false; // Luôn tạo ở trạng thái chưa active để chờ duyệt
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
        existing.Status = poi.Status;
        existing.RejectionNote = poi.RejectionNote;
        existing.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ApprovePoiAsync(int poiId, int priority)
    {
        var poi = await _db.Pois.FindAsync(poiId);
        if (poi is null) return false;

        poi.Status = "Approved";
        poi.IsActive = true;
        poi.Priority = priority;
        poi.UpdatedAt = DateTime.Now;
        poi.RejectionNote = null;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectPoiAsync(int poiId, string note)
    {
        var poi = await _db.Pois.FindAsync(poiId);
        if (poi is null) return false;

        poi.Status = "Rejected";
        poi.IsActive = false;
        poi.RejectionNote = note;
        poi.UpdatedAt = DateTime.Now;

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

    /// <summary>Lấy tất cả POI kèm theo Narrations và QrCodes để hiển thị trong trang quản lý Audio.</summary>
    public async Task<List<Poi>> GetAllWithNarrationsAsync()
    {
        return await _db.Pois
            .Include(p => p.Narrations.Where(n => n.IsActive))
            .Include(p => p.QrCodes.Where(q => q.IsActive))
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    private static string ComputeShortHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..8];
    }

    /// <summary>Tránh lỗi SQL “string truncation” khi URL FPT hoặc tiêu đề vượt MaxLength.</summary>
    private static string TruncateForDb(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        var t = value.Trim();
        return t.Length <= maxLen ? t : t[..maxLen];
    }

    /// <summary>
    /// Xóa toàn bộ audio thủ công (AudioUrl, AudioUrlQr) của tất cả quán ăn
    /// để chuyển sang dùng hoàn toàn TTS Automation.
    /// </summary>
    public async Task<int> GlobalCleanupLegacyAudioAsync()
    {
        var narrations = await _db.Narrations.ToListAsync();
        foreach (var n in narrations)
        {
            n.AudioUrl = null;
            n.AudioUrlQr = null;
            n.UpdatedAt = DateTime.Now;
        }
        return await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Tự động dịch và tạo Audio TTS cho các ngôn ngữ active (vi, en, ja, ko, zh) từ nội dung Tiếng Việt.
    /// </summary>
    public async Task<List<(string Code, bool Success, string? Error)>> UpdateMultilingualNarrationsAsync(int poiId, bool forceRefresh = false)
    {
        var results = new List<(string Code, bool Success, string? Error)>();
        
        var poi = await _db.Pois
            .Include(p => p.Narrations)
            .FirstOrDefaultAsync(p => p.PoiId == poiId);
            
        if (poi == null) return results;

        // 1. Lấy nguồn Tiếng Việt (Description của POI là gốc)
        string sourceContent = (poi.Description ?? "").Trim();
        if (string.IsNullOrWhiteSpace(sourceContent)) 
        {
            // Nếu POI description trống, thử tìm narration vi hiện có
            var viNar = await _db.Narrations.Include(n => n.Language)
                .FirstOrDefaultAsync(n => n.PoiId == poiId && n.Language.Code == "vi");
            sourceContent = viNar?.Content ?? "";
        }

        if (string.IsNullOrWhiteSpace(sourceContent)) return results;

        // 2. Lấy danh sách toàn bộ ngôn ngữ active
        var activeLanguages = await _db.Languages.AsNoTracking()
            .Where(l => l.IsActive)
            .ToListAsync();

        foreach (var lang in activeLanguages)
        {
            try
            {
                string targetTitle = poi.Name;
                string targetContent = sourceContent;

                // Nếu không phải tiếng Việt thì mới dịch
                if (lang.Code != "vi")
                {
                    targetTitle = await _translate.TranslateAsync(poi.Name, "vi", lang.Code);
                    targetContent = await _translate.TranslateAsync(sourceContent, "vi", lang.Code);
                }

                var existing = poi.Narrations.FirstOrDefault(n => n.LanguageId == lang.LanguageId);
                
                // Kiểm tra xem có cần tạo lại audio không?
                // Nếu content thay đổi HOẶC chưa có audio HOẶC forceRefresh = true thì mới tạo.
                bool needsTts = forceRefresh || existing == null || existing.Content != targetContent || 
                                (string.IsNullOrEmpty(existing.AudioUrlQr) && string.IsNullOrEmpty(existing.AudioUrlAuto));

                string? audioUrl = existing?.AudioUrlQr ?? existing?.AudioUrlAuto ?? existing?.AudioUrl;

                if (needsTts && !string.IsNullOrEmpty(lang.TtsVoice))
                {
                    var stem = $"auto_{lang.Code}_p{poiId}";
                    var tts = await _tts.GenerateAndPersistLocalAsync(targetContent, lang.TtsVoice, stem, lang.Code);
                    if (string.IsNullOrEmpty(tts.ErrorMessage))
                        audioUrl = tts.Url;
                }

                if (existing != null)
                {
                    existing.Title = TruncateForDb(targetTitle, 200);
                    existing.Content = targetContent;
                    existing.AudioUrlQr = audioUrl;
                    existing.AudioUrlAuto = audioUrl;
                    existing.AudioUrl = audioUrl; // Legacy
                    existing.TtsVoice = lang.TtsVoice;
                    existing.UpdatedAt = DateTime.Now;
                    existing.IsActive = true;
                }
                else
                {
                    _db.Narrations.Add(new Narration
                    {
                        PoiId = poiId,
                        LanguageId = lang.LanguageId,
                        Title = TruncateForDb(targetTitle, 200),
                        Content = targetContent,
                        AudioUrlQr = audioUrl,
                        AudioUrlAuto = audioUrl,
                        AudioUrl = audioUrl,
                        TtsVoice = lang.TtsVoice,
                        IsActive = true,
                        UpdatedAt = DateTime.Now
                    });
                }
                
                results.Add((lang.Code, true, null));
            }
            catch (Exception ex)
            {
                results.Add((lang.Code, false, ex.Message));
            }
        }

        await _db.SaveChangesAsync();
        return results;
    }
}