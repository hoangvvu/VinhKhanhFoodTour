using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TourController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private static readonly HashSet<string> AllowedEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "move", "enter", "exit", "qr_scan", "listen_start", "listen_end"
    };

    public TourController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// Bắt đầu tour sau khi quét QR.
    /// Trả về danh sách audio của TẤT CẢ quán trên phố, sắp xếp theo khoảng cách.
    /// Nếu QR là mã của quán cụ thể, quán đó sẽ được đưa lên đầu.
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<StartTourResponseDto>> StartTour([FromBody] StartTourRequestDto request)
    {
        if (request.Latitude == 0 && request.Longitude == 0)
            return BadRequest(new StartTourResponseDto 
            { 
                Success = false, 
                Message = "Vị trí không hợp lệ. Vui lòng bật GPS." 
            });

        // Tìm POI từ QR token (nếu có)
        int? priorityPoiId = null;
        var masterToken = (_configuration["Tour:MasterQrToken"] ?? "VINH-KHANH-TOUR").Trim();
        if (!string.IsNullOrWhiteSpace(request.QrToken))
        {
            var normalizedToken = NormalizeQrToken(request.QrToken);
            if (!string.Equals(normalizedToken, masterToken, StringComparison.OrdinalIgnoreCase))
            {
                var qrCode = await _context.QrCodes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(q => q.QrToken == normalizedToken && q.IsActive);

                if (qrCode is null)
                {
                    return BadRequest(new StartTourResponseDto
                    {
                        Success = false,
                        Message = "Mã QR không hợp lệ hoặc đã bị vô hiệu hóa."
                    });
                }

                priorityPoiId = qrCode.PoiId;
            }
        }

        // Lấy language ID
        var language = await _context.Languages
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Code == request.LanguageCode && l.IsActive);
        
        var languageId = language?.LanguageId ?? 1; // fallback to Vietnamese

        // Lấy tất cả POI active có audio
        var poisWithAudio = await _context.Pois
            .AsNoTracking()
            .Include(p => p.Narrations.Where(n => n.IsActive && n.LanguageId == languageId))
                .ThenInclude(n => n.Language)
            .Where(p => p.IsActive && p.OwnerId != null && p.Status == "Approved")
            .Where(p => p.Narrations.Any(n => 
                n.IsActive && 
                n.LanguageId == languageId && 
                (!string.IsNullOrEmpty(n.AudioUrl) || 
                 !string.IsNullOrEmpty(n.AudioUrlAuto) || 
                 !string.IsNullOrEmpty(n.AudioUrlQr))))
            .ToListAsync();

        if (!poisWithAudio.Any())
        {
            return Ok(new StartTourResponseDto
            {
                Success = false,
                Message = "Không tìm thấy quán nào có audio thuyết minh.",
                TotalStalls = 0
            });
        }

        var apiBaseUrl = GetApiBaseUrl();
        var audioQueue = new List<AudioQueueItemDto>();

        foreach (var poi in poisWithAudio)
        {
            var narration = poi.Narrations.FirstOrDefault();
            if (narration == null) continue;

            // Ưu tiên: AudioUrlAuto (tự động gần POI) > AudioUrl > AudioUrlQr
            var audioUrl = !string.IsNullOrEmpty(narration.AudioUrlAuto) 
                ? narration.AudioUrlAuto 
                : !string.IsNullOrEmpty(narration.AudioUrl) 
                    ? narration.AudioUrl 
                    : narration.AudioUrlQr;

            if (string.IsNullOrEmpty(audioUrl)) continue;

            var distance = CalculateDistanceMeters(
                request.Latitude, request.Longitude,
                (double)poi.Latitude, (double)poi.Longitude);

            audioQueue.Add(new AudioQueueItemDto
            {
                PoiId = poi.PoiId,
                PoiName = poi.Name,
                Address = poi.Address,
                Description = poi.Description,
                AudioUrl = NormalizeAudioUrl(audioUrl, apiBaseUrl),
                DistanceMeters = distance,
                DurationSeconds = EstimateAudioDuration(narration.Content),
                SortOrder = poi.Priority,
                LanguageCode = narration.Language?.Code ?? "vi",
                CoverImageUrl = NormalizeAudioUrl(poi.ImageUrl, apiBaseUrl),
                Latitude = (double)poi.Latitude,
                Longitude = (double)poi.Longitude,
                PoiRadiusMeters = poi.Radius
            });
        }

        // Sắp xếp: nếu có priority POI (từ QR) thì đưa lên đầu, còn lại theo khoảng cách
        if (priorityPoiId.HasValue)
        {
            audioQueue = audioQueue
                .OrderByDescending(a => a.PoiId == priorityPoiId.Value) // POI từ QR lên đầu
                .ThenBy(a => a.DistanceMeters) // Sau đó theo khoảng cách
                .ThenBy(a => a.SortOrder) // Cuối cùng theo priority
                .ToList();
        }
        else
        {
            audioQueue = audioQueue
                .OrderBy(a => a.DistanceMeters)
                .ThenBy(a => a.SortOrder)
                .ToList();
        }

        // Tạo tour session ID
        var sessionId = $"tour-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";

        // Track tour start
        if (!string.IsNullOrWhiteSpace(request.DeviceId))
        {
            _context.TrackingLogs.Add(new Infrastructure.Entities.TrackingLog
            {
                DeviceId = request.DeviceId,
                PoiId = priorityPoiId,
                Latitude = (decimal)request.Latitude,
                Longitude = (decimal)request.Longitude,
                EventType = "qr_scan",
                LanguageCode = request.LanguageCode
            });
            await _context.SaveChangesAsync();
        }

        var totalDuration = audioQueue.Sum(a => a.DurationSeconds);

        // Lấy audio intro phố theo ngôn ngữ (key = "intro_audio_vi", "intro_audio_en", ...)
        var introKey = $"intro_audio_{(request.LanguageCode ?? "vi").ToLowerInvariant()}";
        var introFallbackKey = "intro_audio_vi";
        var introSetting = await _context.TourSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == introKey)
            ?? await _context.TourSettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == introFallbackKey);

        var introAudioUrl = string.IsNullOrWhiteSpace(introSetting?.SettingValue)
            ? null
            : NormalizeAudioUrl(introSetting.SettingValue, apiBaseUrl);

        return Ok(new StartTourResponseDto
        {
            Success = true,
            TourSessionId = sessionId,
            AudioQueue = audioQueue,
            TotalStalls = audioQueue.Count,
            EstimatedDurationSeconds = totalDuration,
            StartingPoiName = audioQueue.FirstOrDefault()?.PoiName,
            IntroAudioUrl = introAudioUrl
        });
    }

    /// <summary>
    /// Lấy audio queue theo vị trí hiện tại (không cần QR).
    /// Dùng cho việc refresh queue khi user di chuyển.
    /// </summary>
    [HttpGet("audio-queue")]
    public async Task<ActionResult<List<AudioQueueItemDto>>> GetAudioQueue(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] string languageCode = "vi",
        [FromQuery] int? limit = null)
    {
        var language = await _context.Languages
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Code == languageCode && l.IsActive);
        
        var languageId = language?.LanguageId ?? 1;

        var poisWithAudio = await _context.Pois
            .AsNoTracking()
            .Include(p => p.Narrations.Where(n => n.IsActive && n.LanguageId == languageId))
                .ThenInclude(n => n.Language)
            .Where(p => p.IsActive && p.OwnerId != null && p.Status == "Approved")
            .Where(p => p.Narrations.Any(n => 
                n.IsActive && 
                n.LanguageId == languageId && 
                (!string.IsNullOrEmpty(n.AudioUrl) || 
                 !string.IsNullOrEmpty(n.AudioUrlAuto))))
            .ToListAsync();

        var apiBaseUrl = GetApiBaseUrl();
        var audioQueue = new List<AudioQueueItemDto>();

        foreach (var poi in poisWithAudio)
        {
            var narration = poi.Narrations.FirstOrDefault();
            if (narration == null) continue;

            var audioUrl = !string.IsNullOrEmpty(narration.AudioUrlAuto) 
                ? narration.AudioUrlAuto 
                : narration.AudioUrl;

            if (string.IsNullOrEmpty(audioUrl)) continue;

            var distance = CalculateDistanceMeters(lat, lng, (double)poi.Latitude, (double)poi.Longitude);

            audioQueue.Add(new AudioQueueItemDto
            {
                PoiId = poi.PoiId,
                PoiName = poi.Name,
                Address = poi.Address,
                Description = poi.Description,
                AudioUrl = NormalizeAudioUrl(audioUrl, apiBaseUrl),
                DistanceMeters = distance,
                DurationSeconds = EstimateAudioDuration(narration.Content),
                SortOrder = poi.Priority,
                LanguageCode = narration.Language?.Code ?? "vi",
                CoverImageUrl = NormalizeAudioUrl(poi.ImageUrl, apiBaseUrl),
                Latitude = (double)poi.Latitude,
                Longitude = (double)poi.Longitude,
                PoiRadiusMeters = poi.Radius
            });
        }

        var sorted = audioQueue
            .OrderBy(a => a.DistanceMeters)
            .ThenBy(a => a.SortOrder)
            .ToList();

        if (limit.HasValue && limit.Value > 0)
            sorted = sorted.Take(limit.Value).ToList();

        return Ok(sorted);
    }

    /// <summary>
    /// Track khi user nghe xong một audio trong tour.
    /// </summary>
    [HttpPost("track-listen")]
    public async Task<IActionResult> TrackListen([FromBody] TrackListenDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DeviceId))
            return BadRequest();

        _context.TrackingLogs.Add(new Infrastructure.Entities.TrackingLog
        {
            DeviceId = dto.DeviceId,
            PoiId = dto.PoiId,
            Latitude = (decimal)(dto.Latitude ?? 0),
            Longitude = (decimal)(dto.Longitude ?? 0),
            EventType = NormalizeEventType(dto.EventType),
            ListenedDurationSec = dto.ListenedDurationSec,
            LanguageCode = dto.LanguageCode
        });
        await _context.SaveChangesAsync();
        return Ok();
    }

    #region Helper Methods

    private string GetApiBaseUrl()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}";
    }

    private static string NormalizeAudioUrl(string? url, string apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        url = url.Trim();

        if (url.StartsWith("http://") || url.StartsWith("https://"))
            return url;

        if (url.StartsWith("/"))
            return $"{apiBaseUrl}{url}";

        return $"{apiBaseUrl}/{url}";
    }

    private static string NormalizeQrToken(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var normalized = Uri.UnescapeDataString(raw).Trim();

        if (normalized.StartsWith("vkfoodtour://", StringComparison.OrdinalIgnoreCase))
            return normalized["vkfoodtour://".Length..].Trim();

        return normalized;
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180);

    /// <summary>
    /// Ước tính thời lượng audio dựa trên độ dài text.
    /// Trung bình: ~150 từ/phút tiếng Việt đọc chậm = ~2.5 từ/giây
    /// </summary>
    private static int EstimateAudioDuration(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 30; // default 30 seconds

        var wordCount = content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var seconds = (int)Math.Ceiling(wordCount / 2.5);
        return Math.Max(15, Math.Min(seconds, 300)); // 15s - 5 min
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

    #endregion
}

public class TrackListenDto
{
    public string DeviceId { get; set; } = string.Empty;
    public int PoiId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? EventType { get; set; }
    public int? ListenedDurationSec { get; set; }
    public string? LanguageCode { get; set; }
}
