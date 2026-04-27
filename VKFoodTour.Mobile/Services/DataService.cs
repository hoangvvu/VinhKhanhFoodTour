using System.Net.Http.Json;
using VKFoodTour.Mobile.Models;
using VKFoodTour.Mobile.Services.Offline;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

public class DataService : IDataService
{
    private const string ApiLogTag = "VKAPI";
    private const int ProbeTimeoutMs = 2500;
    private const int MaxFallbackCandidates = 1;
    private readonly HttpClient _http;
    private readonly ISettingsService _settings;
    private readonly ILocalStore _store;
    private readonly string _deviceId;
    private readonly SemaphoreSlim _apiDetectLock = new(1, 1);
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private bool _isApiBaseResolved;
    private string _resolvedApiBase = string.Empty;
    private static readonly HashSet<string> AllowedEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "move", "enter", "exit", "qr_scan", "listen_start", "listen_end"
    };

    public DataService(HttpClient http, ISettingsService settings, ILocalStore store)
    {
        _http = http;
        _settings = settings;
        _store = store;
        _deviceId = GetOrCreateDeviceId();
        _ = Task.Run(async () =>
        {
            try { await _store.InitAsync(); } catch (Exception ex) { LogApi($"LocalStore init failed: {ex.Message}"); }
        });
    }

    public string DeviceId => _deviceId;

    private string ApiRoot => _settings.ApiBaseUrl.Trim().TrimEnd('/');

    private async Task EnsureApiBaseResolvedAsync(CancellationToken cancellationToken = default)
    {
        var current = ApiRoot;
        if (!string.Equals(_resolvedApiBase, current, StringComparison.OrdinalIgnoreCase))
            _isApiBaseResolved = false;

        if (_isApiBaseResolved)
            return;

        await _apiDetectLock.WaitAsync(cancellationToken);
        try
        {
            if (_isApiBaseResolved)
                return;

            var currentFirst = ApiRoot;
            if (await CanReachApiAsync(currentFirst, cancellationToken))
            {
                _settings.ApiBaseUrl = currentFirst;
                _resolvedApiBase = currentFirst;
                _isApiBaseResolved = true;
                LogApi($"Using current base: {currentFirst}");
                return;
            }

            var extraChecked = 0;
            foreach (var candidate in _settings.GetApiBaseCandidates())
            {
                if (candidate.Equals(currentFirst, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (extraChecked >= MaxFallbackCandidates)
                    break;

                if (await CanReachApiAsync(candidate, cancellationToken))
                {
                    _settings.ApiBaseUrl = candidate;
                    _resolvedApiBase = candidate;
                    _isApiBaseResolved = true;
                    LogApi($"Switched to reachable base: {candidate}");
                    return;
                }

                extraChecked++;
            }
            LogApi($"No reachable API base found. Current={currentFirst}");
        }
        finally
        {
            _apiDetectLock.Release();
        }
    }

    private async Task<HttpResponseMessage?> GetWithReconnectAsync(string relativePath, CancellationToken cancellationToken)
    {
        var firstBase = ApiRoot;
        try
        {
            var first = await _http.GetAsync($"{firstBase}{relativePath}", cancellationToken);
            if (first.IsSuccessStatusCode)
                return first;
            LogApi($"Request {firstBase}{relativePath} => {(int)first.StatusCode} {first.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            LogApi($"Request failed {firstBase}{relativePath}: {ex.GetType().Name} - {ex.Message}");
        }

        _isApiBaseResolved = false;
        await EnsureApiBaseResolvedAsync(cancellationToken);
        var secondBase = ApiRoot;
        if (string.Equals(firstBase, secondBase, StringComparison.OrdinalIgnoreCase))
            return null;
        LogApi($"Retrying request on new base: {secondBase}");

        try
        {
            return await _http.GetAsync($"{secondBase}{relativePath}", cancellationToken);
        }
        catch (Exception ex)
        {
            LogApi($"Retry failed {secondBase}{relativePath}: {ex.GetType().Name} - {ex.Message}");
            return null;
        }
    }

    private async Task<bool> CanReachApiAsync(string baseUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ProbeTimeoutMs);
            var response = await _http.GetAsync($"{baseUrl.TrimEnd('/')}/api/Languages", timeoutCts.Token);
            LogApi($"Probe {baseUrl} => {(int)response.StatusCode} {response.ReasonPhrase}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogApi($"Probe failed {baseUrl}: {ex.GetType().Name} - {ex.Message}");
            return false;
        }
    }

    private static void LogApi(string message)
    {
#if ANDROID
        Android.Util.Log.Warn(ApiLogTag, message);
#endif
        System.Diagnostics.Debug.WriteLine($"[API] {message}");
    }

    public async Task<List<Poi>> GetPoisAsync(CancellationToken cancellationToken = default)
    {
        var lang = _settings.SelectedLanguageCode;
        await EnsureApiBaseResolvedAsync(cancellationToken);
        try
        {
            var langQuery = $"?lang={Uri.EscapeDataString(lang)}";
            var response = await GetWithReconnectAsync($"/api/Poi{langQuery}", cancellationToken);
            if (response is null || !response.IsSuccessStatusCode)
                return await GetCachedPoisOrDemoAsync(lang);

            var dtos = await response.Content.ReadFromJsonAsync<List<PoiDto>>(cancellationToken: cancellationToken);
            if (dtos is null)
                return await GetCachedPoisOrDemoAsync(lang);

            try { await _store.UpsertPoisAsync(lang, dtos, MapToMobileFromDto); }
            catch (Exception cacheEx) { LogApi($"Cache POI failed: {cacheEx.Message}"); }

            _ = Task.Run(() => FlushPendingEventsAsync(CancellationToken.None));
            return dtos.Select(MapToMobile).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API GetPois: {ex.Message}");
            return await GetCachedPoisOrDemoAsync(lang);
        }
    }

    private async Task<List<Poi>> GetCachedPoisOrDemoAsync(string lang)
    {
        try
        {
            var cached = await _store.GetPoisAsync(lang);
            if (cached.Count > 0)
            {
                LogApi($"Using cached POIs ({cached.Count}) for lang={lang}");
                foreach (var p in cached) p.CoverEmoji = StallEmoji(p.Name);
                return cached;
            }
        }
        catch (Exception ex) { LogApi($"Read POI cache failed: {ex.Message}"); }
        return FallbackDemo();
    }

    private Poi MapToMobileFromDto(PoiDto d) => MapToMobile(d);

    public async Task<Poi?> GetPoiByIdAsync(int poiId, CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        try
        {
            var langQuery = $"?lang={Uri.EscapeDataString(_settings.SelectedLanguageCode)}";
            var response = await _http.GetAsync($"{ApiRoot}/api/Poi/{poiId}{langQuery}", cancellationToken);
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
        var lang = _settings.SelectedLanguageCode;
        await EnsureApiBaseResolvedAsync(cancellationToken);
        try
        {
            var langQuery = $"?lang={Uri.EscapeDataString(lang)}";
            var response = await _http.GetAsync($"{ApiRoot}/api/Poi/{poiId}/detail{langQuery}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return await TryGetCachedPoiDetailAsync(poiId, lang);

            var dto = await response.Content.ReadFromJsonAsync<PoiDetailDto>(cancellationToken: cancellationToken);
            if (dto is null)
                return await TryGetCachedPoiDetailAsync(poiId, lang);

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

            try { await _store.UpsertPoiDetailAsync(poiId, lang, dto); }
            catch (Exception ex) { LogApi($"Cache POI detail failed: {ex.Message}"); }

            return dto;
        }
        catch
        {
            return await TryGetCachedPoiDetailAsync(poiId, lang);
        }
    }

    private async Task<PoiDetailDto?> TryGetCachedPoiDetailAsync(int poiId, string lang)
    {
        try { return await _store.GetPoiDetailAsync(poiId, lang); }
        catch (Exception ex) { LogApi($"Read POI detail cache failed: {ex.Message}"); return null; }
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
        // Mặc định lấy ngôn ngữ UI hiện tại của user nếu caller không truyền vào,
        // để dashboard admin thống kê đúng ngôn ngữ đang dùng (kể cả heartbeat move/exit).
        var resolvedLanguage = NormalizeLanguageCode(
            string.IsNullOrWhiteSpace(languageCode) ? _settings.SelectedLanguageCode : languageCode);
        var dto = new TrackingLogRequestDto
        {
            DeviceId = _deviceId,
            PoiId = poiId,
            EventType = normalizedEventType,
            ListenedDurationSec = listenedDurationSec,
            LanguageCode = resolvedLanguage,
            Latitude = latitude,
            Longitude = longitude
        };

        try
        {
            var resp = await _http.PostAsJsonAsync($"{ApiRoot}/api/Tracking/log", dto, cancellationToken);
            if (!resp.IsSuccessStatusCode)
                await EnqueueTrackingAsync(dto);
            else
                _ = Task.Run(() => FlushPendingEventsAsync(CancellationToken.None));
        }
        catch
        {
            await EnqueueTrackingAsync(dto);
        }
    }

    private async Task EnqueueTrackingAsync(TrackingLogRequestDto dto)
    {
        try
        {
            await _store.EnqueueEventAsync(new PendingEventRow
            {
                DeviceId = dto.DeviceId,
                PoiId = dto.PoiId,
                EventType = dto.EventType,
                ListenedDurationSec = dto.ListenedDurationSec,
                LanguageCode = dto.LanguageCode,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            });
        }
        catch (Exception ex) { LogApi($"Enqueue event failed: {ex.Message}"); }
    }

    private async Task FlushPendingEventsAsync(CancellationToken cancellationToken)
    {
        if (!await _flushLock.WaitAsync(0, cancellationToken)) return;
        try
        {
            var pending = await _store.GetPendingEventsAsync(50);
            if (pending.Count == 0) return;
            LogApi($"Flushing {pending.Count} pending tracking events");
            foreach (var row in pending)
            {
                try
                {
                    var resp = await _http.PostAsJsonAsync($"{ApiRoot}/api/Tracking/log",
                        new TrackingLogRequestDto
                        {
                            DeviceId = row.DeviceId,
                            PoiId = row.PoiId,
                            EventType = row.EventType,
                            ListenedDurationSec = row.ListenedDurationSec,
                            LanguageCode = row.LanguageCode,
                            Latitude = row.Latitude,
                            Longitude = row.Longitude
                        }, cancellationToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        await _store.DeleteEventAsync(row.Id);
                    }
                    else
                    {
                        await _store.IncrementAttemptAsync(row.Id);
                        if (row.Attempts >= 8) await _store.DeleteEventAsync(row.Id);
                        break;
                    }
                }
                catch
                {
                    await _store.IncrementAttemptAsync(row.Id);
                    break;
                }
            }
        }
        catch (Exception ex) { LogApi($"Flush pending events failed: {ex.Message}"); }
        finally { _flushLock.Release(); }
    }

    public async Task<List<LanguageListItemDto>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        try
        {
            var response = await GetWithReconnectAsync("/api/Languages", cancellationToken);
            if (response is null || !response.IsSuccessStatusCode)
                return await GetCachedLanguagesOrFallbackAsync();

            var list = await response.Content.ReadFromJsonAsync<List<LanguageListItemDto>>(cancellationToken: cancellationToken);
            if (list is { Count: > 0 })
            {
                try { await _store.UpsertLanguagesAsync(list); }
                catch (Exception ex) { LogApi($"Cache languages failed: {ex.Message}"); }
                return list;
            }
            return await GetCachedLanguagesOrFallbackAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API GetLanguages: {ex.Message}");
            return await GetCachedLanguagesOrFallbackAsync();
        }
    }

    private async Task<List<LanguageListItemDto>> GetCachedLanguagesOrFallbackAsync()
    {
        try
        {
            var cached = await _store.GetLanguagesAsync();
            if (cached.Count > 0) return cached;
        }
        catch (Exception ex) { LogApi($"Read language cache failed: {ex.Message}"); }
        return FallbackLanguages();
    }

    public async Task<bool> PostAppFeedbackAsync(CreateAppFeedbackDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        try
        {
            var response = await _http.PostAsJsonAsync($"{ApiRoot}/api/Feedback/app", dto, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API PostAppFeedback: {ex.Message}");
            return false;
        }
    }

    private static List<LanguageListItemDto> FallbackLanguages() =>
        new()
        {
            new LanguageListItemDto { Code = "vi", Name = "Tiếng Việt" },
            new LanguageListItemDto { Code = "en", Name = "English" }
        };

    public async Task<QrResolveDto?> ResolveQrAsync(string scannedPayload, string? languageCode = null, CancellationToken cancellationToken = default)
    {
        await EnsureApiBaseResolvedAsync(cancellationToken);
        var token = ExtractQrToken(scannedPayload);
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var encoded = Uri.EscapeDataString(token);
            var langQuery = string.IsNullOrEmpty(languageCode) ? "" : $"?lang={Uri.EscapeDataString(languageCode)}";
            var response = await _http.GetAsync($"{ApiRoot}/api/Qr/resolve/{encoded}{langQuery}", cancellationToken);
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

    /// <summary>
    /// Chuẩn hoá language code: bỏ prefix legacy như "anon:", trim, lower-case.
    /// Trả về null nếu không hợp lệ để dashboard không phải đếm các giá trị rác.
    /// </summary>
    private static string? NormalizeLanguageCode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var v = raw.Trim();
        var colon = v.IndexOf(':');
        if (colon >= 0 && colon < v.Length - 1)
            v = v[(colon + 1)..];
        v = v.Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(v) ? null : v;
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
            MembershipTier = d.MembershipTier ?? "Standard",
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
