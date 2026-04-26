using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;

namespace Admin.Services;

public class DashboardMetricsService
{
    private readonly ApplicationDbContext _db;

    public DashboardMetricsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<VendorDashboardMetrics?> GetVendorMetricsAsync(int userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        var poi = await _db.Pois
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Narrations)
            .FirstOrDefaultAsync(p => p.OwnerId == userId);
        if (poi is null)
            return null;

        var today = DateTime.Today;
        var qrScanCount = await _db.TrackingLogs
            .AsNoTracking()
            .CountAsync(t => t.PoiId == poi.PoiId && t.EventType == "qr_scan" && t.CreatedAt >= today);
        var audioPlayCount = await _db.TrackingLogs
            .AsNoTracking()
            .CountAsync(t => t.PoiId == poi.PoiId && t.EventType == "listen_start" && t.CreatedAt >= today);
        var reviewQuery = _db.Reviews.AsNoTracking().Where(r => r.PoiId == poi.PoiId);
        var totalReviews = await reviewQuery.CountAsync();
        var averageRating = totalReviews > 0 ? await reviewQuery.AverageAsync(r => (double)r.Rating) : 0;
        var menuCount = await _db.Foods.AsNoTracking().CountAsync(f => f.PoiId == poi.PoiId && f.IsAvailable);

        var hasDescription = !string.IsNullOrWhiteSpace(poi.Description);
        var hasAddress = !string.IsNullOrWhiteSpace(poi.Address);
        var hasValidMap = poi.Latitude != 0m && poi.Longitude != 0m;
        var hasCover = !string.IsNullOrWhiteSpace(poi.ImageUrl) || poi.Images.Any(i => i.IsCover);
        var hasAudio = poi.Narrations.Any(n => n.IsActive && (!string.IsNullOrWhiteSpace(n.AudioUrlAuto) || !string.IsNullOrWhiteSpace(n.AudioUrlQr)));

        return new VendorDashboardMetrics
        {
            PoiId = poi.PoiId,
            PoiName = poi.Name,
            MembershipTier = string.IsNullOrWhiteSpace(user?.MembershipTier) ? "Standard" : user!.MembershipTier!,
            QrScanCountToday = qrScanCount,
            AudioPlayCountToday = audioPlayCount,
            MenuCount = menuCount,
            TotalReviews = totalReviews,
            AverageRating = averageRating,
            HasDescription = hasDescription,
            HasAddress = hasAddress,
            HasValidMap = hasValidMap,
            HasCover = hasCover,
            HasAudio = hasAudio,
            LastUpdated = DateTime.Now
        };
    }
}

public sealed class VendorDashboardMetrics
{
    public int PoiId { get; set; }
    public string PoiName { get; set; } = "Chủ quán";
    public string MembershipTier { get; set; } = "Standard";
    public int QrScanCountToday { get; set; }
    public int AudioPlayCountToday { get; set; }
    public int MenuCount { get; set; }
    public int TotalReviews { get; set; }
    public double AverageRating { get; set; }
    public bool HasDescription { get; set; }
    public bool HasAddress { get; set; }
    public bool HasValidMap { get; set; }
    public bool HasCover { get; set; }
    public bool HasAudio { get; set; }
    public DateTime LastUpdated { get; set; }
}
