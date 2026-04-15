using System.Collections.ObjectModel;
using Microsoft.Maui.Devices.Sensors;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

/// <summary>
/// Quản lý hàng đợi audio cho tour.
/// Tự động phát audio tiếp theo khi audio hiện tại kết thúc.
/// </summary>
public interface IAudioQueueService
{
    /// <summary>Danh sách audio trong hàng đợi.</summary>
    ObservableCollection<AudioQueueItemDto> Queue { get; }
    
    /// <summary>Audio đang phát hiện tại.</summary>
    AudioQueueItemDto? CurrentlyPlaying { get; }
    
    /// <summary>Đang phát hay không.</summary>
    bool IsPlaying { get; }
    
    /// <summary>Đang tạm dừng hay không.</summary>
    bool IsPaused { get; }
    
    /// <summary>Số quán còn lại trong hàng đợi.</summary>
    int RemainingCount { get; }
    
    /// <summary>Tổng thời gian còn lại (giây).</summary>
    int RemainingDurationSeconds { get; }

    /// <summary>Khởi tạo queue từ danh sách audio.</summary>
    Task InitializeQueueAsync(List<AudioQueueItemDto> items);
    
    /// <summary>Bắt đầu phát từ đầu queue.</summary>
    Task StartAsync();
    
    /// <summary>Phát audio tiếp theo.</summary>
    Task PlayNextAsync();
    
    /// <summary>Bỏ qua audio hiện tại, chuyển sang audio tiếp theo.</summary>
    Task SkipAsync();
    
    /// <summary>Tạm dừng audio hiện tại.</summary>
    void Pause();
    
    /// <summary>Tiếp tục phát audio đã tạm dừng.</summary>
    Task ResumeAsync();
    
    /// <summary>Dừng và xóa toàn bộ queue.</summary>
    void Stop();
    
    /// <summary>Xóa queue nhưng không dừng audio đang phát.</summary>
    void ClearQueue();

    /// <summary>Sự kiện khi bắt đầu phát một audio.</summary>
    event EventHandler<AudioQueueItemDto>? OnAudioStarted;
    
    /// <summary>Sự kiện khi kết thúc một audio.</summary>
    event EventHandler<AudioQueueItemDto>? OnAudioEnded;
    
    /// <summary>Sự kiện khi phát hết queue.</summary>
    event EventHandler? OnQueueCompleted;
    
    /// <summary>Sự kiện khi có lỗi phát audio.</summary>
    event EventHandler<string>? OnError;
}

public class AudioQueueService : IAudioQueueService, IDisposable
{
    private readonly IAudioPlaybackService _audioPlayer;
    private readonly IDataService _dataService;
    private readonly ILocalizationService _localization;
    
    private readonly Queue<AudioQueueItemDto> _internalQueue = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _playbackCts;
    private bool _isDisposed;
    private DateTime _currentAudioStartTime;
    private static readonly TimeSpan GeofencePollInterval = TimeSpan.FromSeconds(4);
    private const int GeofenceGpsTimeoutSec = 8;

    public ObservableCollection<AudioQueueItemDto> Queue { get; } = new();
    public AudioQueueItemDto? CurrentlyPlaying { get; private set; }
    public bool IsPlaying => _audioPlayer.IsPlaying;
    public bool IsPaused { get; private set; }
    public int RemainingCount => _internalQueue.Count;
    public int RemainingDurationSeconds => _internalQueue.Sum(a => a.DurationSeconds);

    public event EventHandler<AudioQueueItemDto>? OnAudioStarted;
    public event EventHandler<AudioQueueItemDto>? OnAudioEnded;
    public event EventHandler? OnQueueCompleted;
    public event EventHandler<string>? OnError;

    public AudioQueueService(
        IAudioPlaybackService audioPlayer, 
        IDataService dataService,
        ILocalizationService localization)
    {
        _audioPlayer = audioPlayer;
        _dataService = dataService;
        _localization = localization;
    }

    public Task InitializeQueueAsync(List<AudioQueueItemDto> items)
    {
        lock (_lock)
        {
            _internalQueue.Clear();
            Queue.Clear();

            foreach (var item in items)
            {
                _internalQueue.Enqueue(item);
                Queue.Add(item);
            }
        }

        return Task.CompletedTask;
    }

    public async Task StartAsync()
    {
        if (_internalQueue.Count == 0)
        {
            OnError?.Invoke(this, _localization.GetString("AudioQueue_Empty"));
            return;
        }

        _playbackCts?.Cancel();
        _playbackCts = new CancellationTokenSource();

        await PlayNextAsync();
    }

    public async Task PlayNextAsync()
    {
        AudioQueueItemDto? nextItem;

        lock (_lock)
        {
            if (_internalQueue.Count == 0)
            {
                CurrentlyPlaying = null;
                OnQueueCompleted?.Invoke(this, EventArgs.Empty);
                return;
            }

            nextItem = _internalQueue.Dequeue();
            
            // Remove from observable collection
            var toRemove = Queue.FirstOrDefault(q => q.PoiId == nextItem.PoiId);
            if (toRemove != null)
                Queue.Remove(toRemove);
        }

        CurrentlyPlaying = nextItem;
        IsPaused = false;
        var token = _playbackCts?.Token ?? default;
        var enteredGeofence = await WaitUntilEnterGeofenceAsync(nextItem, token);
        if (!enteredGeofence)
            return;

        _currentAudioStartTime = DateTime.UtcNow;

        await _dataService.TrackEventAsync(
            nextItem.PoiId,
            "enter",
            languageCode: nextItem.LanguageCode,
            cancellationToken: token);

        MainThread.BeginInvokeOnMainThread(() =>
            OnAudioStarted?.Invoke(this, nextItem));

        // Track listen start
        await _dataService.TrackEventAsync(
            nextItem.PoiId, 
            "listen_start", 
            languageCode: nextItem.LanguageCode);

        // Play audio
        var success = await _audioPlayer.PlayAsync(nextItem.AudioUrl, token);

        if (!success)
        {
            OnError?.Invoke(this, $"Không thể phát audio: {nextItem.PoiName}");
            // Try next audio
            await PlayNextAsync();
            return;
        }

        // Wait for audio to finish
        _ = WaitForAudioCompletionAsync(nextItem);
    }

    private async Task<bool> WaitUntilEnterGeofenceAsync(AudioQueueItemDto item, CancellationToken cancellationToken)
    {
        var poiRadius = Math.Clamp(item.PoiRadiusMeters, 5, 200);
        var thresholdMeters = poiRadius + 10; // nới nhẹ sai số GPS thực tế

        while (!cancellationToken.IsCancellationRequested)
        {
            var location = await TryGetCurrentLocationAsync(cancellationToken);
            if (location is not null)
            {
                var distanceMeters = CalculateDistanceMeters(
                    location.Latitude,
                    location.Longitude,
                    item.Latitude,
                    item.Longitude);

                if (distanceMeters <= thresholdMeters)
                    return true;
            }

            await Task.Delay(GeofencePollInterval, cancellationToken);
        }

        return false;
    }

    private static async Task<Location?> TryGetCurrentLocationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var last = await Geolocation.GetLastKnownLocationAsync();
            if (last is not null)
                return last;

            return await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(GeofenceGpsTimeoutSec)),
                cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadius = 6371000; // meters
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180d);

    private async Task WaitForAudioCompletionAsync(AudioQueueItemDto item)
    {
        var token = _playbackCts?.Token ?? default;
        var maxWait = TimeSpan.FromSeconds(Math.Max(item.DurationSeconds * 1.5, 60));
        var started = DateTime.UtcNow;

        try
        {
            while (_audioPlayer.IsPlaying && DateTime.UtcNow - started < maxWait)
            {
                if (token.IsCancellationRequested)
                    return;

                await Task.Delay(500, token);
            }

            if (!token.IsCancellationRequested && CurrentlyPlaying?.PoiId == item.PoiId)
            {
                // Audio finished naturally
                var listenedDuration = (int)(DateTime.UtcNow - _currentAudioStartTime).TotalSeconds;
                
                // Track listen end
                await _dataService.TrackEventAsync(
                    item.PoiId, 
                    "listen_end", 
                    listenedDurationSec: listenedDuration,
                    languageCode: item.LanguageCode);

                MainThread.BeginInvokeOnMainThread(() => 
                    OnAudioEnded?.Invoke(this, item));

                // Play next
                await PlayNextAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Playback was cancelled (skip, stop, etc.)
        }
    }

    public async Task SkipAsync()
    {
        if (CurrentlyPlaying == null)
            return;

        var skippedItem = CurrentlyPlaying;
        var listenedDuration = (int)(DateTime.UtcNow - _currentAudioStartTime).TotalSeconds;

        _audioPlayer.Stop();

        // Track as skipped
        await _dataService.TrackEventAsync(
            skippedItem.PoiId, 
            "listen_skip", 
            listenedDurationSec: listenedDuration,
            languageCode: skippedItem.LanguageCode);

        MainThread.BeginInvokeOnMainThread(() => 
            OnAudioEnded?.Invoke(this, skippedItem));

        await PlayNextAsync();
    }

    public void Pause()
    {
        if (!IsPlaying || IsPaused)
            return;

        // Note: Plugin.Maui.Audio doesn't have native Pause
        // We'll stop and track position for resume
        _audioPlayer.Stop();
        IsPaused = true;
    }

    public async Task ResumeAsync()
    {
        if (!IsPaused || CurrentlyPlaying == null)
            return;

        IsPaused = false;
        
        // Replay from start (Plugin.Maui.Audio limitation)
        var success = await _audioPlayer.PlayAsync(CurrentlyPlaying.AudioUrl);
        if (!success)
        {
            OnError?.Invoke(this, $"Không thể tiếp tục phát: {CurrentlyPlaying.PoiName}");
        }
    }

    public void Stop()
    {
        _playbackCts?.Cancel();
        _audioPlayer.Stop();
        
        lock (_lock)
        {
            _internalQueue.Clear();
            Queue.Clear();
        }
        
        CurrentlyPlaying = null;
        IsPaused = false;
    }

    public void ClearQueue()
    {
        lock (_lock)
        {
            _internalQueue.Clear();
            Queue.Clear();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        _playbackCts?.Cancel();
        _playbackCts?.Dispose();
        _isDisposed = true;
    }
}
