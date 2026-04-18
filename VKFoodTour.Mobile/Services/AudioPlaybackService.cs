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

    public double GetProgress()
    {
        try
        {
            if (_player == null || !_player.IsPlaying) return 0d;
            var duration = _player.Duration;
            if (duration <= 0) return 0d;
            return Math.Clamp(_player.CurrentPosition / duration, 0d, 1d);
        }
        catch
        {
            return 0d;
        }
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
        if (bytes.Length < 4)
            return false;

        // MP3 — ID3v2 tag header
        if (bytes[0] == 'I' && bytes[1] == 'D' && bytes[2] == '3')
            return true;

        // MP3 — MPEG frame sync (bất kỳ layer + bitrate hợp lệ)
        // byte[0]=0xFF, byte[1]: bits 7-5 phải = 111 (sync), bit 4-3 != 00 (layer)
        if (bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0)
        {
            // Layer bits [4:3] != 00 (00 = reserved, invalid)
            var layer = (bytes[1] >> 1) & 0x03;
            if (layer != 0) return true;
            // Nếu layer=0 thì vẫn cho qua nếu có bitrate khác 0 và 0xF
            var bitrate = (bytes[2] >> 4) & 0x0F;
            if (bitrate != 0 && bitrate != 0xF) return true;
        }

        // WAV — RIFF....WAVE
        if (bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F')
            return true;

        // MP4 / M4A / AAC — ftyp box
        if (bytes.Length >= 8 && bytes[4] == 'f' && bytes[5] == 't' && bytes[6] == 'y' && bytes[7] == 'p')
            return true;

        // OGG Vorbis / OGG Opus
        if (bytes[0] == 'O' && bytes[1] == 'g' && bytes[2] == 'g' && bytes[3] == 'S')
            return true;

        // FLAC
        if (bytes[0] == 'f' && bytes[1] == 'L' && bytes[2] == 'a' && bytes[3] == 'C')
            return true;

        // AAC ADTS — FF F* hoặc FF E*
        if (bytes[0] == 0xFF && (bytes[1] == 0xF1 || bytes[1] == 0xF9))
            return true;

        System.Diagnostics.Debug.WriteLine(
            $"[Audio] Binary check FAILED — first 8 bytes: {string.Join(" ", bytes.Take(Math.Min(8, bytes.Length)).Select(b => b.ToString("X2")))}");
        return false;
    }
}
