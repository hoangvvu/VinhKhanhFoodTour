using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Infrastructure.Entities;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TrackingController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    private static readonly HashSet<string> AllowedEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "move", "enter", "exit", "qr_scan", "listen_start", "listen_end"
    };

    public TrackingController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ================================================================
    //  POST /api/Tracking/log     — mobile gửi heartbeat + events
    // ================================================================
    [HttpPost("log")]
    public async Task<IActionResult> Log([FromBody] TrackingLogRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DeviceId) || string.IsNullOrWhiteSpace(dto.EventType))
            return BadRequest("Missing deviceId/eventType.");

        var log = new TrackingLog
        {
            DeviceId = dto.DeviceId.Trim(),
            PoiId = dto.PoiId,
            Latitude = (decimal)(dto.Latitude ?? 0),
            Longitude = (decimal)(dto.Longitude ?? 0),
            EventType = NormalizeEventType(dto.EventType),
            ListenedDurationSec = dto.ListenedDurationSec,
            LanguageCode = dto.LanguageCode
        };

        _context.TrackingLogs.Add(log);
        await _context.SaveChangesAsync();
        return Ok();
    }

    // ================================================================
    //  GET /api/Tracking/online-count?minutes=2
    //  Đếm số DeviceId duy nhất có bất kỳ log nào trong N phút gần nhất.
    // ================================================================
    [HttpGet("online-count")]
    public async Task<ActionResult<OnlineCountDto>> GetOnlineCount([FromQuery] int minutes = 2)
    {
        minutes = Math.Clamp(minutes, 1, 60);
        // TRACKING_LOGS.created_at lưu theo DateTime.Now (local giờ máy chủ), so sánh cùng hệ.
        var threshold = DateTime.Now.AddMinutes(-minutes);

        var count = await _context.TrackingLogs
            .AsNoTracking()
            .Where(t => t.CreatedAt >= threshold)
            .Select(t => t.DeviceId)
            .Distinct()
            .CountAsync();

        return Ok(new OnlineCountDto
        {
            OnlineCount = count,
            WindowMinutes = minutes,
            ServerTimeUtc = DateTime.UtcNow
        });
    }

    // ================================================================
    //  GET /api/Tracking/heatmap?hours=24&eventType=move
    //  Gom cụm vị trí → mảng điểm heatmap cho Leaflet.
    //  Làm tròn toạ độ tới 4 chữ số (~11m) để gom trùng.
    // ================================================================
    [HttpGet("heatmap")]
    public async Task<ActionResult<HeatmapResponseDto>> GetHeatmap(
        [FromQuery] int hours = 24,
        [FromQuery] string? eventType = null)
    {
        hours = Math.Clamp(hours, 1, 24 * 30);
        var threshold = DateTime.Now.AddHours(-hours);
        var normalizedType = string.IsNullOrWhiteSpace(eventType)
            ? null
            : eventType.Trim().ToLowerInvariant();

        var query = _context.TrackingLogs
            .AsNoTracking()
            .Where(t => t.CreatedAt >= threshold
                        && t.Latitude != 0m
                        && t.Longitude != 0m);

        if (normalizedType is not null && AllowedEventTypes.Contains(normalizedType))
            query = query.Where(t => t.EventType == normalizedType);

        // Pull tối đa 50k bản ghi, aggregate phía server C# để tránh truy vấn phức tạp trên EF.
        var raw = await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(50_000)
            .Select(t => new { t.Latitude, t.Longitude })
            .ToListAsync();

        var buckets = new Dictionary<(double, double), int>(raw.Count);
        foreach (var row in raw)
        {
            var lat = Math.Round((double)row.Latitude, 4);
            var lng = Math.Round((double)row.Longitude, 4);
            var key = (lat, lng);
            buckets[key] = buckets.TryGetValue(key, out var c) ? c + 1 : 1;
        }

        var points = buckets
            .Select(kvp => new HeatmapPointDto
            {
                Latitude = kvp.Key.Item1,
                Longitude = kvp.Key.Item2,
                Weight = kvp.Value
            })
            .OrderByDescending(p => p.Weight)
            .ToList();

        return Ok(new HeatmapResponseDto
        {
            Hours = hours,
            EventType = normalizedType,
            TotalPoints = points.Count,
            Points = points
        });
    }

    private static string NormalizeEventType(string? rawEventType)
    {
        if (string.IsNullOrWhiteSpace(rawEventType))
            return "move";

        var normalized = rawEventType.Trim().ToLowerInvariant();
        if (normalized == "tour_start")
            return "qr_scan";
        if (normalized == "listen_skip")
            return "listen_end";

        return AllowedEventTypes.Contains(normalized) ? normalized : "move";
    }
}
