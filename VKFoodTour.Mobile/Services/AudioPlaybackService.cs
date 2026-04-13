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
        try
        {
            using var response = await _http.GetAsync(full, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[Audio] HTTP {(int)response.StatusCode} for {full}");
                return false;
            }

            await using var remote = await response.Content.ReadAsStreamAsync(cancellationToken);
            var ms = new MemoryStream();
            await remote.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            _playbackBuffer = ms;
            _player = _audioManager.CreatePlayer(ms);
            _player.Play();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Audio] {full}: {ex.Message}");
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
        MediaUrlNormalizer.ToAbsolute(url, _settings.ApiBaseUrl) ?? url;
}
