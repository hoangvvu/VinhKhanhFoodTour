namespace VKFoodTour.Mobile.Services;

/// <summary>
/// v3 — BULLETPROOF: Tự build URL tuyệt đối không phụ thuộc MediaUrlNormalizer.
/// Nếu URL bắt đầu bằng "/" hoặc không có scheme → tự ghép với ApiBaseUrl.
/// Luôn log 4 dòng: INPUT → BASE → FINAL → RESULT để debug dễ.
/// </summary>
public sealed class HttpImageService : IHttpImageService
{
    private readonly HttpClient _http;
    private readonly ISettingsService _settings;
    private readonly Dictionary<string, byte[]> _cache = new(StringComparer.Ordinal);

    public HttpImageService(HttpClient http, ISettingsService settings)
    {
        _http = http;
        _settings = settings;
    }

    public async Task<ImageSource?> GetImageSourceAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var input = url.Trim();
        var apiBase = (_settings.ApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        var full = BuildAbsoluteUrl(input, apiBase);

        System.Diagnostics.Debug.WriteLine($"[HttpImageService] INPUT={input}");
        System.Diagnostics.Debug.WriteLine($"[HttpImageService] BASE={apiBase}");
        System.Diagnostics.Debug.WriteLine($"[HttpImageService] FINAL={full}");

        if (string.IsNullOrWhiteSpace(full) || !Uri.TryCreate(full, UriKind.Absolute, out var absUri))
        {
            System.Diagnostics.Debug.WriteLine($"[HttpImageService] FAIL: cannot build absolute URL");
            return null;
        }

        if (_cache.TryGetValue(full, out var cachedBytes))
            return ImageSource.FromStream(() => new MemoryStream(cachedBytes));

        try
        {
            using var response = await _http.GetAsync(absUri, HttpCompletionOption.ResponseContentRead, cancellationToken);

            System.Diagnostics.Debug.WriteLine($"[HttpImageService] Status={response.StatusCode} " +
                $"ContentType={response.Content.Headers.ContentType?.MediaType} " +
                $"Length={response.Content.Headers.ContentLength}");

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[HttpImageService] FAIL HTTP {(int)response.StatusCode}");
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

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

            _cache[full] = bytes;
            System.Diagnostics.Debug.WriteLine($"[HttpImageService] OK {bytes.Length} bytes cached");
            return ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HttpImageService] EXCEPTION {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>Ghép URL relative với apiBase. Nếu URL đã absolute thì giữ nguyên (trừ loopback).</summary>
    private static string BuildAbsoluteUrl(string url, string apiBase)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var raw = url.Trim();
        if (raw.StartsWith("~/", StringComparison.Ordinal))
            raw = raw[2..];

        // Nếu đã là URL tuyệt đối
        if (Uri.TryCreate(raw, UriKind.Absolute, out var abs))
        {
            // Nếu host là loopback thì rewrite sang apiBase
            if (IsLoopbackHost(abs.Host) && Uri.TryCreate(apiBase, UriKind.Absolute, out var apiUri))
                return $"{apiUri.Scheme}://{apiUri.Authority}{abs.PathAndQuery}";

            return raw;
        }

        // URL relative → cần ghép với apiBase
        if (string.IsNullOrWhiteSpace(apiBase))
            return string.Empty; // không có base → không thể build

        if (!raw.StartsWith('/'))
            raw = '/' + raw;

        return apiBase + raw;
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
}
