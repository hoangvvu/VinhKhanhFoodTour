namespace VKFoodTour.Mobile.Services;

public interface ISettingsService
{
    string SelectedLanguageCode { get; set; }
    bool AutoPlayEnabled { get; set; }

    /// <summary>Gốc API: chỉ scheme + host (+ port), không kèm đường dẫn. Ví dụ Dev Tunnel: https://abc-7105.region.devtunnels.ms</summary>
    string ApiBaseUrl { get; set; }

    /// <summary>Danh sách endpoint API thử tự động kết nối (ưu tiên endpoint đã lưu).</summary>
    IReadOnlyList<string> GetApiBaseCandidates();
}

public class SettingsService : ISettingsService
{
    // Dán URL Dev Tunnel hiện tại của bạn vào đây (chỉ phần gốc, không thêm /swagger)
    private const string DevTunnelApiBase = "https://3xr47z2x-7105.asse.devtunnels.ms";

    /// <summary>Bỏ mọi đường dẫn sau host (tránh dán nhầm .../swagger/index.html).</summary>
    private static string NormalizeApiBase(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url?.Trim() ?? string.Empty;
        var trimmed = url.Trim().TrimEnd('/');
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return trimmed;
        return $"{uri.Scheme}://{uri.Authority}";
    }

    private static string DefaultApiBase() =>
#if ANDROID
        DevTunnelApiBase;
#else
        "http://localhost:5242";
#endif

    public string SelectedLanguageCode
    {
        get => Preferences.Default.Get(nameof(SelectedLanguageCode), "vi");
        set => Preferences.Default.Set(nameof(SelectedLanguageCode), value);
    }

    public bool AutoPlayEnabled
    {
        get => Preferences.Default.Get(nameof(AutoPlayEnabled), true);
        set => Preferences.Default.Set(nameof(AutoPlayEnabled), value);
    }

    public string ApiBaseUrl
    {
        get
        {
            var raw = Preferences.Default.Get(nameof(ApiBaseUrl), DefaultApiBase());
            var normalized = NormalizeApiBase(raw);
            // Nếu còn cache URL ngrok cũ, tự động chuyển về Dev Tunnel hiện tại.
            if (normalized.Contains(".ngrok-free.app", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains(".ngrok-free.dev", StringComparison.OrdinalIgnoreCase))
            {
                Preferences.Default.Set(nameof(ApiBaseUrl), DevTunnelApiBase);
                return DevTunnelApiBase;
            }
            if (!string.Equals(raw.Trim(), normalized, StringComparison.OrdinalIgnoreCase))
                Preferences.Default.Set(nameof(ApiBaseUrl), normalized);
            return normalized;
        }
        set
        {
            var normalized = NormalizeApiBase(value);
            Preferences.Default.Set(nameof(ApiBaseUrl), normalized);
        }
    }

    public IReadOnlyList<string> GetApiBaseCandidates()
    {
        var candidates = new List<string>();

        void AddCandidate(string? value)
        {
            var normalized = NormalizeApiBase(value);
            if (string.IsNullOrWhiteSpace(normalized))
                return;
            if (candidates.Any(x => x.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                return;
            candidates.Add(normalized);
        }

        AddCandidate(Preferences.Default.Get(nameof(ApiBaseUrl), string.Empty));
        AddCandidate(DefaultApiBase());
        AddCandidate(DevTunnelApiBase);
        AddCandidate("http://localhost:5242");
        AddCandidate("http://10.0.2.2:5242");
        AddCandidate("http://127.0.0.1:5242");

        return candidates;
    }
}