using Microsoft.Extensions.DependencyInjection;
using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.Controls;

/// <summary>
/// View hiển thị ảnh từ URL (nội bộ API hoặc web ngoài).
/// Chiến lược:
///   1) Nếu URL là http/https hợp lệ (web ngoài: unsplash, cloudinary...) → dùng UriImageSource (MAUI tự tải, tự cache).
///   2) Nếu URL là relative (/uploads/poi/abc.jpg) → gọi IHttpImageService để ghép ApiBaseUrl + kiểm tra content-type + tải bytes.
/// Tự fallback về placeholder emoji nếu fail.
/// </summary>
public class NetworkImageView : ContentView
{
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
            return;

        // ───────────────────────────────────────────────
        //  CHIẾN LƯỢC 1: URL web tuyệt đối (http/https) và KHÔNG phải loopback
        //  → dùng UriImageSource. MAUI có Glide (Android) / SDWebImage (iOS) xử lý
        //    redirect + cert + cache đĩa rất tốt, KHÔNG bị chặn bởi HttpClient custom header.
        // ───────────────────────────────────────────────
        var trimmed = url.Trim();
        if (trimmed.StartsWith("~/", StringComparison.Ordinal))
            trimmed = trimmed[2..];

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var abs)
            && (abs.Scheme == Uri.UriSchemeHttp || abs.Scheme == Uri.UriSchemeHttps)
            && !IsLoopbackHost(abs.Host))
        {
            try
            {
                _image.Source = new UriImageSource
                {
                    Uri = abs,
                    CachingEnabled = true,
                    CacheValidity = TimeSpan.FromDays(7)
                };
                _image.IsVisible = true;
                _placeholder.IsVisible = false;
                return;
            }
            catch
            {
                // Rơi về chiến lược 2 nếu vì lý do nào đó UriImageSource lỗi.
            }
        }

        // ───────────────────────────────────────────────
        //  CHIẾN LƯỢC 2: URL relative hoặc loopback → tải qua IHttpImageService
        //  (service này ghép ApiBaseUrl + chuyển 10.0.2.2 → Dev Tunnel nếu cần).
        // ───────────────────────────────────────────────
        var services = Handler?.MauiContext?.Services ?? Application.Current?.Handler?.MauiContext?.Services;
        if (services is null)
            return;

        var loader = services.GetService<IHttpImageService>();
        if (loader is null)
            return;

        var source = await loader.GetImageSourceAsync(trimmed, token);
        if (token.IsCancellationRequested || source is null)
            return;

        _image.Source = source;
        _image.IsVisible = true;
        _placeholder.IsVisible = false;
    }

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase)
        || host.Equals("10.0.2.2", StringComparison.OrdinalIgnoreCase);
}
