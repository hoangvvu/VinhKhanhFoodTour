namespace VKFoodTour.Mobile.Services;

/// <summary>Chuẩn hóa imageUrl/audioUrl từ API: relative, ~/ , thiếu dấu /, localhost.</summary>
public static class MediaUrlNormalizer
{
    public static string? ToAbsolute(string? url, string apiRoot)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        var raw = url.Trim();
        if (raw.StartsWith("~/", StringComparison.Ordinal))
            raw = raw[2..];

        if (Uri.TryCreate(raw, UriKind.Absolute, out var abs))
        {
            if (IsLoopbackHost(abs.Host))
            {
                var root = apiRoot.Trim().TrimEnd('/');
                if (!Uri.TryCreate(root, UriKind.Absolute, out var api))
                    return raw;
                return $"{api.Scheme}://{api.Authority}{abs.PathAndQuery}";
            }

            return raw;
        }

        if (!raw.StartsWith('/'))
            raw = '/' + raw;

        return $"{apiRoot.Trim().TrimEnd('/')}{raw}";
    }

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase)
        || host.Equals("10.0.2.2", StringComparison.OrdinalIgnoreCase);
}
