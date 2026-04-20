using System.Collections.ObjectModel;
using System.Threading;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

/// <summary>
/// Quản lý hàng đợi audio cho tour theo mô hình Geofence-Trigger.
///
/// Thay vì sequential WaitUntilEnterGeofence, service này:
///   - Nhận tín hiệu từ GeofenceMonitorService khi du khách vào zone của 1 POI
///   - Quyết định: phát ngay / chèn tiếp theo / bỏ qua — dựa trên tiến độ track đang phát
///
/// Quy tắc interrupt (threshold = 60%):
///   track hiện tại >= 60% → InsertNext(poi mới)
///   track hiện tại <  60% → InterruptAndPlay(poi mới), chèn track cũ vào sau
/// </summary>
public interface IAudioQueueService
{
    ObservableCollection<AudioQueueItemDto> Queue { get; }
    AudioQueueItemDto? CurrentlyPlaying { get; }
    bool IsPlaying { get; }
    bool IsPaused { get; }
    int RemainingCount { get; }
    int RemainingDurationSeconds { get; }

    Task InitializeQueueAsync(List<AudioQueueItemDto> items);
    Task StartAsync();
    Task SkipAsync();
    void Pause();
    Task ResumeAsync();
    void Stop();
    void ClearQueue();

    /// <summary>Gọi khi GeofenceMonitor báo du khách vào zone của poiId.</summary>
    Task HandlePoiEnteredAsync(int poiId);

    event EventHandler<AudioQueueItemDto>? OnAudioStarted;
    event EventHandler<AudioQueueItemDto>? OnAudioEnded;
    event EventHandler? OnQueueCompleted;
    event EventHandler<string>? OnError;
}

public class AudioQueueService : IAudioQueueService, IDisposable
{
    // ── Dependencies ──────────────────────────────────────────────
    private readonly IAudioPlaybackService _audioPlayer;
    private readonly IDataService _dataService;
    private readonly ILocalizationService _localization;
    private readonly GeofenceMonitorService _geofence;

    // ── Queue state ───────────────────────────────────────────────
    private readonly List<AudioQueueItemDto> _queue = new();   // không dùng Queue<T> vì cần InsertNext
    private readonly object _lock = new();
    private readonly HashSet<int> _playedPois = new();          // đã phát xong
    private readonly SemaphoreSlim _enterGate = new(1, 1);      // tuần tự hóa event geofence enter
    private bool _disposed;
    private bool _started;

    // ── Playback state ────────────────────────────────────────────
    private CancellationTokenSource? _playCts;
    private DateTime _currentAudioStartTime;

    // ── Constants ─────────────────────────────────────────────────
    private const double InterruptThreshold = 0.60; // < 60% → interrupt; >= 60% → InsertNext

    // ── Public properties ─────────────────────────────────────────
    public ObservableCollection<AudioQueueItemDto> Queue { get; } = new();
    public AudioQueueItemDto? CurrentlyPlaying { get; private set; }
    public bool IsPlaying => _audioPlayer.IsPlaying;
    public bool IsPaused { get; private set; }
    public int RemainingCount { get { lock (_lock) return _queue.Count; } }
    public int RemainingDurationSeconds { get { lock (_lock) return _queue.Sum(a => a.DurationSeconds); } }

    public event EventHandler<AudioQueueItemDto>? OnAudioStarted;
    public event EventHandler<AudioQueueItemDto>? OnAudioEnded;
    public event EventHandler? OnQueueCompleted;
    public event EventHandler<string>? OnError;

    public AudioQueueService(
        IAudioPlaybackService audioPlayer,
        IDataService dataService,
        ILocalizationService localization,
        GeofenceMonitorService geofence)
    {
        _audioPlayer = audioPlayer;
        _dataService = dataService;
        _localization = localization;
        _geofence = geofence;

        _geofence.PoiEntered += OnGeofencePoiEntered;
    }

    // ═══════════════════════════════════════════════════════════════
    //  InitializeQueueAsync — thiết lập danh sách ban đầu
    // ═══════════════════════════════════════════════════════════════
    public Task InitializeQueueAsync(List<AudioQueueItemDto> items)
    {
        lock (_lock)
        {
            _queue.Clear();
            _queue.AddRange(items);
            _playedPois.Clear();
            _started = false;
            SyncObservableQueue();
        }
        return Task.CompletedTask;
    }

    // ═══════════════════════════════════════════════════════════════
    //  StartAsync — kích hoạt GeofenceMonitor, không phát ngay
    //  (sẽ phát khi du khách enter zone đầu tiên)
    // ═══════════════════════════════════════════════════════════════
    public Task StartAsync()
    {
        List<AudioQueueItemDto> snapshot;
        lock (_lock) snapshot = new List<AudioQueueItemDto>(_queue);

        if (!snapshot.Any())
        {
            OnError?.Invoke(this, _localization.GetString("AudioQueue_Empty"));
            return Task.CompletedTask;
        }

        _started = true;
        _geofence.StartMonitoring(snapshot);
        return Task.CompletedTask;
    }

    // ═══════════════════════════════════════════════════════════════
    //  HandlePoiEnteredAsync — quyết định phát / chèn / bỏ qua
    // ═══════════════════════════════════════════════════════════════
    public async Task HandlePoiEnteredAsync(int poiId)
    {
        if (!_started) return;
        await _enterGate.WaitAsync();
        try
        {
            AudioQueueItemDto? item;
            lock (_lock)
            {
                // Đã phát xong → bỏ qua
                if (_playedPois.Contains(poiId)) return;

                // Đang phát chính xác POI này → bỏ qua
                if (CurrentlyPlaying?.PoiId == poiId) return;

                // Tìm trong queue
                item = _queue.FirstOrDefault(q => q.PoiId == poiId);
            }

            if (item == null) return;

            if (CurrentlyPlaying != null)
            {
                // Có track đang active (kể cả đang khởi động playback) thì không phát đè ngay.
                if (_audioPlayer.IsPlaying)
                {
                    var progress = _audioPlayer.GetProgress();
                    if (progress >= InterruptThreshold)
                    {
                        // Track hiện tại đã > 60% → chèn POI mới vào next slot
                        InsertNext(item);
                        System.Diagnostics.Debug.WriteLine($"[AudioQueue] InsertNext POI {poiId} (progress={progress:P0})");
                    }
                    else
                    {
                        // Track hiện tại < 60% → ngắt, phát POI mới ngay
                        System.Diagnostics.Debug.WriteLine($"[AudioQueue] InterruptAndPlay POI {poiId} (progress={progress:P0})");
                        await InterruptAndPlayAsync(item);
                    }
                }
                else
                {
                    // Tránh race-condition lúc track 1 vừa bắt đầu nhưng IsPlaying chưa true.
                    InsertNext(item);
                    System.Diagnostics.Debug.WriteLine($"[AudioQueue] InsertNext POI {poiId} (bootstrap)");
                }

                return;
            }

            // Không có gì đang phát → phát ngay
            lock (_lock) _queue.Remove(item);
            SyncObservableQueue();
            await PlayItemAsync(item);
        }
        finally
        {
            _enterGate.Release();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Skip / Pause / Resume / Stop / Clear
    // ═══════════════════════════════════════════════════════════════
    public async Task SkipAsync()
    {
        if (CurrentlyPlaying == null) return;

        var skipped = CurrentlyPlaying;
        var listenedSec = (int)(DateTime.UtcNow - _currentAudioStartTime).TotalSeconds;

        CancelCurrentPlayback();
        _audioPlayer.Stop();

        await SafeTrackAsync(skipped.PoiId, "listen_end", listenedSec, skipped.LanguageCode);

        MainThread.BeginInvokeOnMainThread(() => OnAudioEnded?.Invoke(this, skipped));
        await PlayNextFromQueueAsync();
    }

    public void Pause()
    {
        if (!IsPlaying || IsPaused) return;
        _audioPlayer.Stop();
        IsPaused = true;
    }

    public async Task ResumeAsync()
    {
        if (!IsPaused || CurrentlyPlaying == null) return;
        IsPaused = false;
        await _audioPlayer.PlayAsync(CurrentlyPlaying.AudioUrl);
    }

    public void Stop()
    {
        CancelCurrentPlayback();
        _audioPlayer.Stop();
        _geofence.StopMonitoring();

        lock (_lock)
        {
            _queue.Clear();
            _playedPois.Clear();
            _started = false;
        }
        SyncObservableQueue();
        CurrentlyPlaying = null;
        IsPaused = false;
    }

    public void ClearQueue()
    {
        lock (_lock) _queue.Clear();
        SyncObservableQueue();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Internal helpers
    // ═══════════════════════════════════════════════════════════════

    private async Task InterruptAndPlayAsync(AudioQueueItemDto newItem)
    {
        var interrupted = CurrentlyPlaying;
        if (interrupted != null)
        {
            var listenedSec = (int)(DateTime.UtcNow - _currentAudioStartTime).TotalSeconds;
            CancelCurrentPlayback();
            _audioPlayer.Stop();

            await SafeTrackAsync(interrupted.PoiId, "listen_end", listenedSec, interrupted.LanguageCode);
            MainThread.BeginInvokeOnMainThread(() => OnAudioEnded?.Invoke(this, interrupted));

            // Chèn track bị ngắt vào NGAY SAU newItem trong queue (sẽ phát lại sau)
            lock (_lock)
            {
                _queue.Remove(newItem);
                // Đặt interrupted vào đầu queue (newItem sẽ được phát, sau đó tới interrupted)
                if (!_playedPois.Contains(interrupted.PoiId))
                {
                    _queue.Insert(0, interrupted);
                }
            }
        }
        else
        {
            lock (_lock) _queue.Remove(newItem);
        }

        SyncObservableQueue();
        await PlayItemAsync(newItem);
    }

    private void InsertNext(AudioQueueItemDto item)
    {
        lock (_lock)
        {
            _queue.Remove(item);
            _queue.Insert(0, item); // đặt ngay đầu queue → phát tiếp theo
        }
        SyncObservableQueue();
    }

    private async Task PlayNextFromQueueAsync()
    {
        AudioQueueItemDto? next;
        lock (_lock)
        {
            if (!_queue.Any())
            {
                CurrentlyPlaying = null;
                MainThread.BeginInvokeOnMainThread(() => OnQueueCompleted?.Invoke(this, EventArgs.Empty));
                return;
            }
            next = _queue[0];
            _queue.RemoveAt(0);
        }
        SyncObservableQueue();
        await PlayItemAsync(next);
    }

    private async Task PlayItemAsync(AudioQueueItemDto item)
    {
        CancelCurrentPlayback();
        _playCts = new CancellationTokenSource();
        var token = _playCts.Token;

        CurrentlyPlaying = item;
        IsPaused = false;
        _currentAudioStartTime = DateTime.UtcNow;

        // Track enter + listen_start
        _ = SafeTrackAsync(item.PoiId, "enter", null, item.LanguageCode);
        _ = SafeTrackAsync(item.PoiId, "listen_start", null, item.LanguageCode);

        MainThread.BeginInvokeOnMainThread(() => OnAudioStarted?.Invoke(this, item));

        // Phát audio
        var success = await _audioPlayer.PlayAsync(item.AudioUrl, token);
        if (!success)
        {
            OnError?.Invoke(this, _localization.GetString("AudioQueue_PlayFailFmt", item.PoiName));
            await PlayNextFromQueueAsync();
            return;
        }

        // Chờ kết thúc tự nhiên (background task an toàn)
        _ = Task.Run(async () =>
        {
            try { await WaitForCompletionAsync(item, token); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[AudioQueue] completion error: {ex.Message}"); }
        });
    }

    private async Task WaitForCompletionAsync(AudioQueueItemDto item, CancellationToken token)
    {
        var maxWait = TimeSpan.FromSeconds(Math.Max(item.DurationSeconds * 1.5, 60));
        var started = DateTime.UtcNow;

        while (_audioPlayer.IsPlaying && DateTime.UtcNow - started < maxWait)
        {
            if (token.IsCancellationRequested) return;
            await Task.Delay(500, token);
        }

        if (token.IsCancellationRequested) return;
        if (CurrentlyPlaying?.PoiId != item.PoiId) return;

        // Audio kết thúc tự nhiên
        var listenedSec = (int)(DateTime.UtcNow - _currentAudioStartTime).TotalSeconds;
        await SafeTrackAsync(item.PoiId, "listen_end", listenedSec, item.LanguageCode);

        lock (_lock) _playedPois.Add(item.PoiId);
        _geofence.MarkPoiPlayed(item.PoiId);

        MainThread.BeginInvokeOnMainThread(() => OnAudioEnded?.Invoke(this, item));

        await MainThread.InvokeOnMainThreadAsync(() => PlayNextFromQueueAsync());
    }

    private void CancelCurrentPlayback()
    {
        _playCts?.Cancel();
        _playCts?.Dispose();
        _playCts = null;
    }

    private void SyncObservableQueue()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Queue.Clear();
            lock (_lock)
            {
                foreach (var item in _queue)
                    Queue.Add(item);
            }
        });
    }

    private async Task SafeTrackAsync(int poiId, string evt, int? durationSec, string? langCode)
    {
        try { await _dataService.TrackEventAsync(poiId, evt, listenedDurationSec: durationSec, languageCode: langCode); }
        catch { /* tracking không block UX */ }
    }

    /// <summary>Geofence callback — chạy trên MainThread do GeofenceMonitorService đảm bảo.</summary>
    private void OnGeofencePoiEntered(object? sender, int poiId)
    {
        _ = HandlePoiEnteredAsync(poiId);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _geofence.PoiEntered -= OnGeofencePoiEntered;
        CancelCurrentPlayback();
        _enterGate.Dispose();
    }
}
