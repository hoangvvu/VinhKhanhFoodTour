using System.Security.Cryptography;
using System.Text;

namespace VKFoodTour.Mobile.Services.Offline;

public class MediaCacheService : IMediaCacheService
{
    private const string SubDir = "media";
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public MediaCacheService(HttpClient http)
    {
        _http = http;
    }

    private string CacheDir
    {
        get
        {
            var dir = Path.Combine(FileSystem.CacheDirectory, SubDir);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public string? TryGetLocalPath(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var path = BuildPath(url);
        return File.Exists(path) ? path : null;
    }

    public async Task<string?> EnsureCachedAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var existing = TryGetLocalPath(url);
        if (existing is not null) return existing;

        try
        {
            using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!resp.IsSuccessStatusCode) return null;
            var ct = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (ct.Contains("text/html", StringComparison.OrdinalIgnoreCase)) return null;
            var bytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length < 64) return null;
            return await SaveBytesAsync(url, bytes, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MediaCache] download failed {url}: {ex.Message}");
            return null;
        }
    }

    public async Task<string> SaveBytesAsync(string url, byte[] data, CancellationToken cancellationToken = default)
    {
        var path = BuildPath(url);
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var tmp = path + ".tmp";
            await File.WriteAllBytesAsync(tmp, data, cancellationToken);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
        finally { _writeLock.Release(); }
        return path;
    }

    private string BuildPath(string url)
    {
        var hash = HashUrl(url);
        var ext = GuessExtension(url);
        return Path.Combine(CacheDir, hash + ext);
    }

    private static string HashUrl(string url)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static string GuessExtension(string url)
    {
        try
        {
            var pathPart = new Uri(url, UriKind.RelativeOrAbsolute).IsAbsoluteUri
                ? new Uri(url).AbsolutePath
                : url;
            var dot = pathPart.LastIndexOf('.');
            if (dot < 0) return ".bin";
            var ext = pathPart[dot..].Split('?', '#')[0].ToLowerInvariant();
            return ext.Length is > 1 and <= 6 ? ext : ".bin";
        }
        catch { return ".bin"; }
    }
}
