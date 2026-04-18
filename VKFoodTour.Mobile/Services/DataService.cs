using System.Net.Http.Json;
using VKFoodTour.Mobile.Models;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

public class DataService : IDataService
{
    private readonly HttpClient _http;
    private readonly ISettingsService _settings;
    private readonly string _deviceId;
    private readonly SemaphoreSlim _apiDetectLock = new(1, 1);
    private bool _isApiBaseResolved;
    private static readonly HashSet<string> AllowedEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "move", "enter", "exit", "qr_scan", "listen_start", "listen_end"
    };

    public DataService(HttpClient http, ISettingsService settings)
    {
        _http = http;
        _settings = settings;
        _deviceId = GetOrCreateDeviceId();
    }

    public string DeviceId => _deviceId;

    private string ApiRoot => _settings.ApiBaseUrl.Trim().TrimEnd('/');

    private async Task EnsureApiBaseResolvedAsync(CancellationToken cancellationToken = default)
    {
        if (_isApiBaseResolved)
            return;

        await _apiDetectLock.WaitAsync(cancellationToken);
        try
        {
            if (_isApiBaseResolved)
                return;

            foreach (var candidate in _settings.GetApiBaseCandidates())
            {
                if (await CanReachApiAsync(candidate, cancellationToken))
                {
                    _settings.ApiBaseUrl = candidate;
                    _isApiBaseResolved = true;
                    return;
                }
            }
        }
        finally
        {
            _apiDetectLock.Release();
        }
    }

    private async Task<bool> CanReachApiAsync(string baseUrl, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _http.GetAsync($"{baseUrl.TrimEnd('/')}/api/Languages", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Poi>> GetPoisAsync(CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        try
        {
            var response = await _http.GetAsync($"{ApiRoot}/api/Poi", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return FallbackDemo();

            var dtos = await response.Content.ReadFromJsonAsync<List<PoiDto>>(cancellationToken: cancellationToken);
            if (dtos is null)
                return new List<Poi>();

            return dtos.Select(MapToMobile).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API GetPois: {ex.Message}");
            return FallbackDemo();
        }
    }

    public async Task<Poi?> GetPoiByIdAsync(int poiId, CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        try
        {
            var response = await _http.GetAsync($"{ApiRoot}/api/Poi/{poiId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var dto = await response.Content.ReadFromJsonAsync<PoiDto>(cancellationToken: cancellationToken);
            return dto is null ? null : MapToMobile(dto);
        }
        catch
        {
            return null;
        }
    }

    public async Task<PoiDetailDto?> GetPoiDetailAsync(int poiId, CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        try
        {
            var response = await _http.GetAsync($"{ApiRoot}/api/Poi/{poiId}/detail", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var dto = await response.Content.ReadFromJsonAsync<PoiDetailDto>(cancellationToken: cancellationToken);
            if (dto is null)
                return null;

            dto.CoverImageUrl = NormalizeMediaUrl(dto.CoverImageUrl);
            foreach (var g in dto.GalleryImages)
                g.Url = NormalizeMediaUrl(g.Url) ?? g.Url;
            foreach (var m in dto.MenuItems)
            {
                m.ImageUrl = NormalizeMediaUrl(m.ImageUrl);
                m.AudioUrl = NormalizeMediaUrl(m.AudioUrl);
            }
            foreach (var a in dto.AudioItems)
                a.Url = NormalizeMediaUrl(a.Url) ?? a.Url;

            return dto;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AuthResponseDto?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        try
        {
            var response = await _http.PostAsJsonAsync($"{ApiRoot}/api/Auth/login",
                new LoginRequestDto { Email = email, Password = password }, cancellationToken);
            return await ParseAuthResponseAsync(response, cancellationToken);
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Không kết nối được máy chủ ({ApiRoot}). Chi tiết: {ex.Message}"
            };
        }
    }

    public async Task<AuthResponseDto?> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        try
        {
            var response = await _http.PostAsJsonAsync($"{ApiRoot}/api/Auth/register",
                new RegisterRequestDto { Name = name, Email = email, Password = password }, cancellationToken);
            return await ParseAuthResponseAsync(response, cancellationToken);
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Không kết nối được máy chủ ({ApiRoot}). Chi tiết: {ex.Message}"
            };
        }
    }

    public async Task<List<ReviewListItemDto>> GetRecentReviewsAsync(int take = 30, CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        take = Math.Clamp(take, 1, 100);
        try
        {
            var response = await _http.GetAsync($"{ApiRoot}/api/Reviews/recent?take={take}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new List<ReviewListItemDto>();

            var list = await response.Content.ReadFromJsonAsync<List<ReviewListItemDto>>(cancellationToken: cancellationToken);
            return list ?? new List<ReviewListItemDto>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API GetRecentReviews: {ex.Message}");
            return new List<ReviewListItemDto>();
        }
    }

    public async Task<List<ReviewListItemDto>> GetPoiReviewsAsync(int poiId, CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        try
        {
            var response = await _http.GetAsync($"{ApiRoot}/api/Reviews/poi/{poiId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new List<ReviewListItemDto>();

            var list = await response.Content.ReadFromJsonAsync<List<ReviewListItemDto>>(cancellationToken: cancellationToken);
            return list ?? new List<ReviewListItemDto>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API GetPoiReviews: {ex.Message}");
            return new List<ReviewListItemDto>();
        }
    }

    public async Task<ReviewListItemDto?> PostReviewAsync(CreateReviewDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        try
        {
            var response = await _http.PostAsJsonAsync($"{ApiRoot}/api/Reviews", dto, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ReviewListItemDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API PostReview: {ex.Message}");
            return null;
        }
    }

    public async Task TrackEventAsync(int? poiId, string eventType, int? listenedDurationSec = null, string? languageCode = null, double? latitude = null, double? longitude = null, CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        var normalizedEventType = NormalizeEventType(eventType);

        try
        {
            await _http.PostAsJsonAsync($"{ApiRoot}/api/Tracking/log",
                new TrackingLogRequestDto
                {
                    DeviceId = _deviceId,
                    PoiId = poiId,
                    EventType = normalizedEventType,
                    ListenedDurationSec = listenedDurationSec,
                    LanguageCode = languageCode,
                    Latitude = latitude,
                    Longitude = longitude
                }, cancellationToken);
        }
        catch
        {
            // Tracking should not block UX flows
        }
    }

    public async Task<List<LanguageListItemDto>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        try
        {
            var response = await _http.GetAsync($"{ApiRoot}/api/Languages", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return FallbackLanguages();

            var list = await response.Content.ReadFromJsonAsync<List<LanguageListItemDto>>(cancellationToken: cancellationToken);
            return list is { Count: > 0 } ? list : FallbackLanguages();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API GetLanguages: {ex.Message}");
            return FallbackLanguages();
        }
    }

    private static List<LanguageListItemDto> FallbackLanguages() =>
        new()
        {
            new LanguageListItemDto { Code = "vi", Name = "Tiếng Việt" },
            new LanguageListItemDto { Code = "en", Name = "English" }
        };

    public async Task<QrResolveDto?> ResolveQrAsync(string scannedPayload, CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        var token = ExtractQrToken(scannedPayload);
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var encoded = Uri.EscapeDataString(token);
            var response = await _http.GetAsync($"{ApiRoot}/api/Qr/resolve/{encoded}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var dto = await response.Content.ReadFromJsonAsync<QrResolveDto>(cancellationToken: cancellationToken);
            if (dto is null)
                return null;

            if (!string.IsNullOrWhiteSpace(dto.AudioUrl))
                dto.AudioUrl = MediaUrlNormalizer.ToAbsolute(dto.AudioUrl, ApiRoot);

            return dto;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API ResolveQr: {ex.Message}");
            return null;
        }
    }

    private static string ExtractQrToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var t = raw.Trim();

        if (Uri.TryCreate(t, UriKind.Absolute, out var absUri))
        {
            if (!string.IsNullOrWhiteSpace(absUri.Query))
            {
                var query = absUri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in query)
                {
                    var pieces = part.Split('=', 2);
                    if (pieces.Length != 2)
                        continue;
                    if (pieces[0].Equals("data", StringComparison.OrdinalIgnoreCase)
                        || pieces[0].Equals("token", StringComparison.OrdinalIgnoreCase))
                    {
                        t = Uri.UnescapeDataString(pieces[1]).Trim();
                        return NormalizeSchemeToken(t);
                    }
                }
            }

            var path = absUri.AbsolutePath;
            var resolveIdx = path.IndexOf("/resolve/", StringComparison.OrdinalIgnoreCase);
            if (resolveIdx >= 0)
            {
                var after = path[(resolveIdx + "/resolve/".Length)..];
                var seg = after.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(seg))
                    return Uri.UnescapeDataString(seg).Trim();
            }

            var segments = absUri.AbsolutePath.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
            {
                var last = segments[^1];
                if (last.StartsWith("VK-", StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(last).Trim();
            }
        }

        return NormalizeSchemeToken(t);
    }

    private static string NormalizeSchemeToken(string t)
    {
        if (t.StartsWith("vkfoodtour://", StringComparison.OrdinalIgnoreCase))
            return t["vkfoodtour://".Length..].Trim();

        if (t.StartsWith("vkfoodtour:", StringComparison.OrdinalIgnoreCase))
        {
            var idx = t.IndexOf("//", StringComparison.Ordinal);
            if (idx >= 0)
                return t[(idx + 2)..].Trim();
        }

        return t;
    }

    private static async Task<AuthResponseDto> ParseAuthResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Máy chủ trả về mã {(int)response.StatusCode} nhưng không có nội dung phản hồi."
            };
        }

        try
        {
            var payload = System.Text.Json.JsonSerializer.Deserialize<AuthResponseDto>(raw, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (payload is not null)
                return payload;
        }
        catch
        {
            // Fall through and show raw snippet below.
        }

        var snippet = raw.Length > 180 ? raw[..180] + "..." : raw;
        return new AuthResponseDto
        {
            Success = false,
            Message = $"Máy chủ trả về nội dung không hợp lệ (HTTP {(int)response.StatusCode}). {snippet}"
        };
    }

    private string? NormalizeMediaUrl(string? url) =>
        MediaUrlNormalizer.ToAbsolute(url, ApiRoot);

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

    private Poi MapToMobile(PoiDto d) =>
        new()
        {
            PoiId = d.PoiId,
            Name = d.Name,
            Address = d.Address ?? string.Empty,
            Latitude = (double)d.Latitude,
            Longitude = (double)d.Longitude,
            Radius = d.Radius,
            Priority = d.Priority,
            CoverEmoji = StallEmoji(d.Name),
            CoverImageUrl = NormalizeMediaUrl(d.ImageUrl)
        };

    private static string StallEmoji(string name)
    {
        var h = name.Aggregate(0, (a, c) => a + c);
        var emojis = new[] { "🍜", "🦪", "🍢", "🥟", "🍲", "🧋", "🍡", "🥘" };
        return emojis[Math.Abs(h) % emojis.Length];
    }

    private static List<Poi> FallbackDemo() =>
        new()
        {
            new Poi { PoiId = 1, Name = "Ốc Oanh (Demo — không kết nối API)", Address = "534 Vĩnh Khánh", Latitude = 10.758, Longitude = 106.705 },
            new Poi { PoiId = 2, Name = "Ốc Vũ (Demo — không kết nối API)", Address = "37 Vĩnh Khánh", Latitude = 10.759, Longitude = 106.706 }
        };
}
