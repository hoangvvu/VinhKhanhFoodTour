namespace VKFoodTour.Mobile.Services;

public sealed class HttpImageService : IHttpImageService
{
    private readonly HttpClient _http;
    private readonly ISettingsService _settings;
    private readonly Dictionary<string, ImageSource> _cache = new(StringComparer.Ordinal);

    public HttpImageService(HttpClient http, ISettingsService settings)
    {
        _http = http;
        _settings = settings;
    }

    public async Task<ImageSource?> GetImageSourceAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var full = NormalizeUrl(url.Trim());
        if (_cache.TryGetValue(full, out var cached))
            return cached;

        try
        {
            var response = await _http.GetAsync(full, HttpCompletionOption.ResponseContentRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length == 0)
                return null;

            var source = ImageSource.FromStream(() => new MemoryStream(bytes));
            _cache[full] = source;
            return source;
        }
        catch
        {
            return null;
        }
    }

    private string NormalizeUrl(string url) =>
        MediaUrlNormalizer.ToAbsolute(url, _settings.ApiBaseUrl) ?? url;
}
