namespace VKFoodTour.Mobile.Services;

/// <summary>
/// v6 — Fix lỗi "The 'file' scheme is not supported" trên Android.
/// Trên .NET Android (Mono), Uri.TryCreate("/uploads/...", UriKind.Absolute, ...) trả về
/// true với scheme = "file" (khác hành vi trên Windows/WinUI). Kết quả là BuildAbsoluteUrl
/// nhánh "đã tuyệt đối" nuốt URL relative và trả nguyên chuỗi, khiến HttpClient ném
/// NotSupportedException khi cố request "file:///uploads/...".
///
/// Fix: check tường minh scheme = http/https TRƯỚC khi coi là URL tuyệt đối. Ngoài ra
/// cũng check URL bắt đầu bằng "/" → coi là relative bất kể Uri.TryCreate nói gì.
/// </summary>
public sealed class HttpImageService : IHttpImageService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(2);

    private readonly HttpClient _http;
    private readonly ISettingsService _settings;
    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    public HttpImageService(HttpClient http, ISettingsService settings)
    {
        _http = http;
        _settings = settings;
    }

    public Task<ImageSource?> GetImageSourceAsync(string? url, CancellationToken cancellationToken = default)
        => GetImageSourceAsync(url, forceReload: false, cancellationToken);

    public async Task<ImageSource?> GetImageSourceAsync(string? url, bool forceReload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var input = url.Trim();
        var apiBase = (_settings.ApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        var full = BuildAbsoluteUrl(input, apiBase);

        System.Diagnostics.Debug.WriteLine($"[HttpImageService] INPUT='{input}'");
        System.Diagnostics.Debug.WriteLine($"[HttpImageService] BASE ='{apiBase}'");
        System.Diagnostics.Debug.WriteLine($"[HttpImageService] FULL ='{full}'");

        if (string.IsNullOrWhiteSpace(full) || !Uri.TryCreate(full, UriKind.Absolute, out var absUri)
            || (absUri.Scheme != Uri.UriSchemeHttp && absUri.Scheme != Uri.UriSchemeHttps))
        {
            System.Diagnostics.Debug.WriteLine("[HttpImageService] FAIL: cannot build absolute http(s) URL");
            return null;
        }

        if (!forceReload && TryGetCache(full, out var cachedBytes) && cachedBytes is not null)
        {
            System.Diagnostics.Debug.WriteLine($"[HttpImageService] CACHE HIT ({cachedBytes.Length} bytes)");
            return ImageSource.FromStream(() => new MemoryStream(cachedBytes));
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, absUri);
            request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
            {
                NoCache = true,
                MaxAge = TimeSpan.Zero
            };

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

            var ct = response.Content.Headers.ContentType?.MediaType ?? "(unknown)";
            var len = response.Content.Headers.ContentLength?.ToString() ?? "(unknown)";
            System.Diagnostics.Debug.WriteLine($"[HttpImageService] Status={(int)response.StatusCode} ContentType={ct} Length={len}");

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[HttpImageService] FAIL HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            System.Diagnostics.Debug.WriteLine($"[HttpImageService] Got {bytes.Length} bytes");

            if (bytes.Length < 100)
            {
                System.Diagnostics.Debug.WriteLine($"[HttpImageService] FAIL tiny response ({bytes.Length} bytes)");
                return null;
            }

            if (!LooksLikeImage(bytes))
            {
                var preview = System.Text.Encoding.UTF8.GetString(bytes, 0, Math.Min(200, bytes.Length));
                System.Diagnostics.Debug.WriteLine($"[HttpImageService] FAIL not image. Preview: {preview}");
                return null;
            }

            SetCache(full, bytes);
            System.Diagnostics.Debug.WriteLine($"[HttpImageService] OK cached {bytes.Length} bytes");
            return ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HttpImageService] EXCEPTION {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    public void Invalidate(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        var apiBase = (_settings.ApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        var full = BuildAbsoluteUrl(url.Trim(), apiBase);
        if (string.IsNullOrWhiteSpace(full)) return;
        lock (_sync) { _cache.Remove(full); }
    }

    public void ClearAll()
    {
        lock (_sync) { _cache.Clear(); }
    }

    private bool TryGetCache(string key, out byte[]? bytes)
    {
        lock (_sync)
        {
            if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
            {
                bytes = entry.Bytes;
                return true;
            }
            if (_cache.ContainsKey(key)) _cache.Remove(key);
            bytes = null;
            return false;
        }
    }

    private void SetCache(string key, byte[] bytes)
    {
        lock (_sync)
        {
            _cache[key] = new CacheEntry(bytes, DateTime.UtcNow.Add(DefaultTtl));
        }
    }

    /// <summary>
    /// Ghép URL relative với apiBase.
    ///
    /// QUAN TRỌNG: trên Android, Uri.TryCreate("/path", UriKind.Absolute, ...) trả về
    /// TRUE với scheme="file" (khác Windows trả về false). Nên phải check ĐÚNG scheme
    /// http/https chứ không tin vào TryCreate thuần.
    ///
    /// Luồng:
    ///   1) URL bắt đầu bằng "/" → luôn là relative → ghép với apiBase.
    ///   2) URL có scheme http/https → giữ nguyên (rewrite loopback nếu cần).
    ///   3) Các trường hợp khác → ghép '/' + url vào apiBase.
    /// </summary>
    private static string BuildAbsoluteUrl(string url, string apiBase)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;

        var raw = url.Trim();
        if (raw.StartsWith("~/", StringComparison.Ordinal))
            raw = raw[2..];

        // (1) URL bắt đầu bằng "/" → CHẮC CHẮN là relative → ghép apiBase.
        //     KHÔNG dùng Uri.TryCreate ở đây vì Android coi nó là scheme="file".
        if (raw.StartsWith('/'))
        {
            if (string.IsNullOrWhiteSpace(apiBase)) return string.Empty;
            return apiBase + raw;
        }

        // (2) Có scheme http/https rõ ràng → giữ nguyên (rewrite loopback nếu cần).
        if (Uri.TryCreate(raw, UriKind.Absolute, out var abs)
            && (abs.Scheme == Uri.UriSchemeHttp || abs.Scheme == Uri.UriSchemeHttps))
        {
            if (IsLoopbackHost(abs.Host) && Uri.TryCreate(apiBase, UriKind.Absolute, out var apiUri)
                && (apiUri.Scheme == Uri.UriSchemeHttp || apiUri.Scheme == Uri.UriSchemeHttps))
            {
                return $"{apiUri.Scheme}://{apiUri.Authority}{abs.PathAndQuery}";
            }
            return raw;
        }

        // (3) Các trường hợp khác (không scheme, không bắt đầu '/') → ghép '/' + apiBase.
        if (string.IsNullOrWhiteSpace(apiBase)) return string.Empty;
        return apiBase + "/" + raw;
    }

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase)
        || host.Equals("10.0.2.2", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeImage(byte[] bytes)
    {
        if (bytes.Length < 4) return false;
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return true;                                      // JPEG
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return true;                  // PNG
        if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38) return true;                  // GIF
        if (bytes.Length >= 12
            && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
            && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) return true;             // WEBP
        if (bytes[0] == 0x42 && bytes[1] == 0x4D) return true;                                                          // BMP
        if (bytes.Length >= 12
            && bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70) return true;               // HEIC
        return false;
    }

    private sealed record CacheEntry(byte[] Bytes, DateTime ExpiresAtUtc)
    {
        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    }
}
