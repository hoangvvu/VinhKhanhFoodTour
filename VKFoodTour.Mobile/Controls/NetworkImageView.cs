using Microsoft.Extensions.DependencyInjection;
using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.Controls;

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
    private readonly Label _placeholder = new() { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, FontSize = 40 };
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

    private async void OnLoaded(object? sender, EventArgs e)
    {
        await LoadImageAsync(Url);
    }

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

        var services = Handler?.MauiContext?.Services ?? Application.Current?.Handler?.MauiContext?.Services;
        if (services is null)
            return;

        var loader = services.GetService<IHttpImageService>();
        if (loader is null)
            return;

        var source = await loader.GetImageSourceAsync(url, token);
        if (token.IsCancellationRequested || source is null)
            return;

        _image.Source = source;
        _image.IsVisible = true;
        _placeholder.IsVisible = false;
    }
}
