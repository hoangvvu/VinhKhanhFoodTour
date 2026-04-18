using Microsoft.Extensions.DependencyInjection;
using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.Controls;

/// <summary>
/// View hiển thị ảnh từ URL.
///
/// v6 — LOGIC CHỌN LOADER:
///   • Host CDN công khai (unsplash, cloudinary…) → UriImageSource (MAUI tự xử lý).
///   • Host API của mình (devtunnels.ms, ngrok, LAN, loopback, relative) → HttpImageService.
///
/// Lý do: MAUI Glide trên Android đôi khi tải thất bại ảnh từ dev tunnel dù
/// trình duyệt trên cùng emulator mở được URL đó (có thể do header Accept mà Glide
/// gửi khiến tunnel xử lý khác, hoặc cert handshake của HttpUrlConnection nội bộ
/// Glide không khớp). HttpClient của ta qua AndroidMessageHandler làm việc ổn định.
///
/// Có log chi tiết để dễ chẩn đoán: tìm tag "[NetworkImageView]" trong Logcat/Output.
/// </summary>
public class NetworkImageView : ContentView
{
    private static readonly TimeSpan ImageCacheValidity = TimeSpan.FromMinutes(2);

    public static readonly BindableProperty UrlProperty = BindableProperty.Create(
        nameof(Url),
        typeof(string),
        typeof(NetworkImageView),
        null,
        propertyChanged: OnUrlChanged);

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder),
        typeof(string),
        typeof(NetworkImageView),
        "🍜");

    private readonly Image _image = new() { Aspect = Aspect.AspectFill, IsVisible = false };
    private readonly Label _placeholder = new()
    {
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center,
        FontSize = 40
    };
    private CancellationTokenSource? _loadCts;

    public NetworkImageView()
    {
        Content = new Grid { Children = { _image, _placeholder } };
        Loaded += OnLoaded;
        HandlerChanged += OnHandlerChanged;
    }

    public string? Url
    {
        get => (string?)GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    private static async void OnUrlChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (NetworkImageView)bindable;
        await view.LoadImageAsync(newValue as string);
    }

    private async void OnLoaded(object? sender, EventArgs e) => await LoadImageAsync(Url);

    private async void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (Handler is not null)
            await LoadImageAsync(Url);
    }

    private async Task LoadImageAsync(string? url)
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        _image.IsVisible = false;
        _placeholder.Text = Placeholder ?? "🍜";
        _placeholder.IsVisible = true;

        if (string.IsNullOrWhiteSpace(url))
        {
            System.Diagnostics.Debug.WriteLine($"[NetworkImageView] URL null/empty → placeholder");
            return;
        }

        var trimmed = url.Trim();
        if (trimmed.StartsWith("~/", StringComparison.Ordinal))
            trimmed = trimmed[2..];

        System.Diagnostics.Debug.WriteLine($"[NetworkImageView] Loading url='{trimmed}'");

        // Phân loại host để chọn loader:
        //   • CDN công khai → UriImageSource (Glide/SDWebImage xử lý).
        //   • API của mình (tunnel/LAN/loopback) → HttpImageService.
        //   • Relative (không có host) → HttpImageService.
        bool useHttpLoader;
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var abs)
            && (abs.Scheme == Uri.UriSchemeHttp || abs.Scheme == Uri.UriSchemeHttps))
        {
            useHttpLoader = IsOurApiHost(abs.Host);
            System.Diagnostics.Debug.WriteLine(
                $"[NetworkImageView] Host='{abs.Host}' isOurApi={useHttpLoader} " +
                $"→ loader={(useHttpLoader ? "HttpImageService" : "UriImageSource")}");
        }
        else
        {
            useHttpLoader = true;
            System.Diagnostics.Debug.WriteLine("[NetworkImageView] Relative URL → HttpImageService");
        }

        // ─── UriImageSource: CDN công khai ───
        if (!useHttpLoader && abs is not null)
        {
            try
            {
                _image.Source = new UriImageSource
                {
                    Uri = abs,
                    CachingEnabled = true,
                    CacheValidity = ImageCacheValidity
                };
                _image.IsVisible = true;
                _placeholder.IsVisible = false;
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[NetworkImageView] UriImageSource failed: {ex.Message} — fallback to HttpImageService");
            }
        }

        // ─── HttpImageService: host API ───
        var services = Handler?.MauiContext?.Services ?? Application.Current?.Handler?.MauiContext?.Services;
        if (services is null)
        {
            System.Diagnostics.Debug.WriteLine("[NetworkImageView] FAIL: MauiContext.Services is null");
            return;
        }

        var loader = services.GetService<IHttpImageService>();
        if (loader is null)
        {
            System.Diagnostics.Debug.WriteLine("[NetworkImageView] FAIL: IHttpImageService not registered");
            return;
        }

        var source = await loader.GetImageSourceAsync(trimmed, token);
        if (token.IsCancellationRequested)
        {
            System.Diagnostics.Debug.WriteLine("[NetworkImageView] Cancelled");
            return;
        }
        if (source is null)
        {
            System.Diagnostics.Debug.WriteLine("[NetworkImageView] HttpImageService returned null → placeholder");
            return;
        }

        _image.Source = source;
        _image.IsVisible = true;
        _placeholder.IsVisible = false;
        System.Diagnostics.Debug.WriteLine("[NetworkImageView] Image displayed OK");
    }

    /// <summary>
    /// Host thuộc nhóm "API của mình":
    ///   • Loopback: localhost, 127.0.0.1, 10.0.2.2
    ///   • Dev Tunnel: *.devtunnels.ms
    ///   • ngrok: *.ngrok-free.app, *.ngrok-free.dev, *.ngrok.dev, *.ngrok.io
    ///   • IP LAN: 10.x / 172.16-31.x / 192.168.x
    /// Những host trên phải đi qua HttpImageService vì cần header tùy biến
    /// và/hoặc HttpClient của app, không dùng UriImageSource được.
    /// </summary>
    private static bool IsOurApiHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        // Loopback
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.Equals("10.0.2.2", StringComparison.OrdinalIgnoreCase)) return true;

        // Dev tunnels (Visual Studio)
        if (host.EndsWith(".devtunnels.ms", StringComparison.OrdinalIgnoreCase)) return true;

        // ngrok
        if (host.EndsWith(".ngrok-free.app", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.EndsWith(".ngrok-free.dev", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.EndsWith(".ngrok.dev", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.EndsWith(".ngrok.io", StringComparison.OrdinalIgnoreCase)) return true;

        // IP LAN private
        if (System.Net.IPAddress.TryParse(host, out var ip))
        {
            var b = ip.GetAddressBytes();
            if (b.Length == 4)
            {
                if (b[0] == 10) return true;                          // 10.0.0.0/8
                if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true; // 172.16.0.0/12
                if (b[0] == 192 && b[1] == 168) return true;          // 192.168.0.0/16
            }
        }

        return false;
    }
}
