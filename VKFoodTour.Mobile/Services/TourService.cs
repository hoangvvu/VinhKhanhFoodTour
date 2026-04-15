using System.Net.Http.Json;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

/// <summary>
/// Service gọi API tour để lấy audio queue.
/// </summary>
public interface ITourService
{
    /// <summary>
    /// Bắt đầu tour sau khi quét QR.
    /// </summary>
    Task<StartTourResponseDto?> StartTourAsync(
        string? qrToken, 
        double latitude, 
        double longitude, 
        string languageCode = "vi",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy audio queue theo vị trí hiện tại (refresh khi di chuyển).
    /// </summary>
    Task<List<AudioQueueItemDto>> GetAudioQueueAsync(
        double latitude, 
        double longitude, 
        string languageCode = "vi",
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Track khi nghe xong một audio.
    /// </summary>
    Task TrackListenAsync(
        int poiId,
        string eventType,
        int? listenedDurationSec = null,
        double? latitude = null,
        double? longitude = null,
        string? languageCode = null,
        CancellationToken cancellationToken = default);
}

public class TourService : ITourService
{
    private readonly HttpClient _http;
    private readonly ISettingsService _settings;
    private readonly string _deviceId;

    public TourService(HttpClient http, ISettingsService settings)
    {
        _http = http;
        _settings = settings;
        _deviceId = GetOrCreateDeviceId();
    }

    private string ApiRoot => _settings.ApiBaseUrl.Trim().TrimEnd('/');

    public async Task<StartTourResponseDto?> StartTourAsync(
        string? qrToken,
        double latitude,
        double longitude,
        string languageCode = "vi",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new StartTourRequestDto
            {
                QrToken = qrToken,
                Latitude = latitude,
                Longitude = longitude,
                DeviceId = _deviceId,
                LanguageCode = languageCode
            };

            var response = await _http.PostAsJsonAsync(
                $"{ApiRoot}/api/Tour/start", 
                request, 
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[Tour] StartTour failed: {response.StatusCode}");
                return new StartTourResponseDto
                {
                    Success = false,
                    Message = $"Lỗi kết nối: {response.StatusCode}"
                };
            }

            return await response.Content.ReadFromJsonAsync<StartTourResponseDto>(
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tour] StartTour error: {ex.Message}");
            return new StartTourResponseDto
            {
                Success = false,
                Message = $"Không thể kết nối server: {ex.Message}"
            };
        }
    }

    public async Task<List<AudioQueueItemDto>> GetAudioQueueAsync(
        double latitude,
        double longitude,
        string languageCode = "vi",
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{ApiRoot}/api/Tour/audio-queue?lat={latitude}&lng={longitude}&languageCode={languageCode}";
            if (limit.HasValue)
                url += $"&limit={limit.Value}";

            var response = await _http.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[Tour] GetAudioQueue failed: {response.StatusCode}");
                return new List<AudioQueueItemDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<List<AudioQueueItemDto>>(
                cancellationToken: cancellationToken);

            return result ?? new List<AudioQueueItemDto>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tour] GetAudioQueue error: {ex.Message}");
            return new List<AudioQueueItemDto>();
        }
    }

    public async Task TrackListenAsync(
        int poiId,
        string eventType,
        int? listenedDurationSec = null,
        double? latitude = null,
        double? longitude = null,
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = new
            {
                DeviceId = _deviceId,
                PoiId = poiId,
                EventType = eventType,
                ListenedDurationSec = listenedDurationSec,
                Latitude = latitude,
                Longitude = longitude,
                LanguageCode = languageCode
            };

            await _http.PostAsJsonAsync($"{ApiRoot}/api/Tour/track-listen", dto, cancellationToken);
        }
        catch
        {
            // Tracking should not block user experience
        }
    }

    private static string GetOrCreateDeviceId()
    {
        const string key = "TrackingDeviceId";
        var id = Preferences.Default.Get(key, string.Empty);
        if (!string.IsNullOrWhiteSpace(id))
            return id;

        id = $"vk-{Guid.NewGuid():N}";
        Preferences.Default.Set(key, id);
        return id;
    }
}
