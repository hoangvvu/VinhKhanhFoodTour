using Plugin.Maui.Audio;

namespace VKFoodTour.Mobile.Services;

public sealed class AudioPlaybackService : IAudioPlaybackService
{
    private readonly IAudioManager _audioManager;
    private readonly HttpClient _http;
    private readonly ISettingsService _settings;
    private IAudioPlayer? _player;
    private MemoryStream? _playbackBuffer;

    public AudioPlaybackService(IAudioManager audioManager, HttpClient http, ISettingsService settings)
    {
        _audioManager = audioManager;
        _http = http;
        _settings = settings;
    }

    public bool IsPlaying => _player?.IsPlaying ?? false;

    public async Task<bool> PlayAsync(string? url, CancellationToken cancellationToken = default)
    {
        Stop();
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var full = NormalizeUrl(url.Trim());
        System.Diagnostics.Debug.WriteLine($"[Audio] PlayAsync → {full}");
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, full);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            System.Diagnostics.Debug.WriteLine($"[Audio] HTTP {(int)response.StatusCode} | Content-Type: {response.Content.Headers.ContentType} | Length: {response.Content.Headers.ContentLength}");

            if (!response.IsSuccessStatusCode)
                return false;

            var ct = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (ct.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine("[Audio] Received HTML instead of audio — likely a login/redirect page from Dev Tunnel.");
                return false;
            }

            await using var remote = await response.Content.ReadAsStreamAsync(cancellationToken);
            var ms = new MemoryStream();
            await remote.CopyToAsync(ms, cancellationToken);

            if (ms.Length < 64)
            {
                System.Diagnostics.Debug.WriteLine($"[Audio] Response too small ({ms.Length} bytes) — not a valid audio file.");
                ms.Dispose();
                return false;
            }

            var bytes = ms.ToArray();
            if (LooksLikeHtml(bytes))
            {
                System.Diagnostics.Debug.WriteLine("[Audio] Payload is HTML instead of binary audio.");
                ms.Dispose();
                return false;
            }

            if (!LooksLikeAudioBinary(bytes))
            {
                System.Diagnostics.Debug.WriteLine("[Audio] Payload is not recognized as audio binary.");
                ms.Dispose();
                return false;
            }

            ms.Position = 0;
            _playbackBuffer = ms;
            _player = _audioManager.CreatePlayer(ms);
            _player.Play();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Audio] {full}: {ex.GetType().Name}: {ex.Message}");
            Stop();
            return false;
        }
    }

    public void Stop()
    {
        _player?.Stop();
        _player?.Dispose();
        _player = null;
        _playbackBuffer?.Dispose();
        _playbackBuffer = null;
    }

    private string NormalizeUrl(string url) =>
        (MediaUrlNormalizer.ToAbsolute(url, _settings.ApiBaseUrl) ?? url)
            .Replace("/uploads/uploads/", "/uploads/", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeHtml(byte[] bytes)
    {
        var take = Math.Min(bytes.Length, 512);
        var head = System.Text.Encoding.UTF8.GetString(bytes, 0, take);
        return head.Contains("<html", StringComparison.OrdinalIgnoreCase)
               || head.Contains("<!doctype", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeAudioBinary(byte[] bytes)
    {
        if (bytes.Length < 12)
            return false;

        // MP3 ID3
        if (bytes[0] == (byte)'I' && bytes[1] == (byte)'D' && bytes[2] == (byte)'3')
            return true;

        // MP3 frame sync
        if (bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0)
            return true;

        // WAV RIFF
        if (bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F')
            return true;

        // MP4/M4A ftyp
        if (bytes[4] == (byte)'f' && bytes[5] == (byte)'t' && bytes[6] == (byte)'y' && bytes[7] == (byte)'p')
            return true;

        return false;
    }
}
