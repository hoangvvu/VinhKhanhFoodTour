using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using VKFoodTour.Mobile.Services;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.ViewModels;

public partial class TourPlayerViewModel : ObservableObject, IDisposable
{
    private readonly ITourService _tourService;
    private readonly IAudioQueueService _audioQueue;
    private readonly IAudioPlaybackService _audioPlayer;   // để phát intro
    private readonly ILocalizationService _localization;
    private readonly SemaphoreSlim _scanGate = new(1, 1);
    private string _lastHandledPayload = string.Empty;
    private DateTime _lastHandledAt = DateTime.MinValue;

    // ── Observable state ──────────────────────────────────────────

    [ObservableProperty] private bool isTourActive;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string tourSessionId = string.Empty;
    [ObservableProperty] private string manualCode = string.Empty;
    [ObservableProperty] private bool showCameraScanner;
    [ObservableProperty] private bool showManualEntry;

    // Currently playing info
    [ObservableProperty] private string nowPlayingName = string.Empty;
    [ObservableProperty] private string nowPlayingAddress = string.Empty;
    [ObservableProperty] private string? nowPlayingImage;
    [ObservableProperty] private string nowPlayingDistance = string.Empty;
    [ObservableProperty] private int currentPoiId;

    // Queue info
    [ObservableProperty] private int remainingCount;
    [ObservableProperty] private string remainingDuration = string.Empty;
    [ObservableProperty] private int totalStalls;
    [ObservableProperty] private int completedStalls;

    // Playback state
    [ObservableProperty] private bool isPlaying;
    [ObservableProperty] private bool isPaused;
    [ObservableProperty] private bool canSkip;
    [ObservableProperty] private bool canPause;

    // Intro state
    [ObservableProperty] private bool isPlayingIntro;

    // UI Strings
    [ObservableProperty] private string uiTourTitle = string.Empty;
    [ObservableProperty] private string uiNowPlaying = string.Empty;
    [ObservableProperty] private string uiRemaining = string.Empty;
    [ObservableProperty] private string uiSkip = string.Empty;
    [ObservableProperty] private string uiStop = string.Empty;

    public ObservableCollection<AudioQueueItemDto> QueueItems => _audioQueue.Queue;

    public TourPlayerViewModel(
        ITourService tourService,
        IAudioQueueService audioQueue,
        IAudioPlaybackService audioPlayer,
        ILocalizationService localization)
    {
        _tourService  = tourService;
        _audioQueue   = audioQueue;
        _audioPlayer  = audioPlayer;
        _localization = localization;

        _localization.LanguageChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(RefreshUiStrings);

        _audioQueue.OnAudioStarted   += OnAudioStarted;
        _audioQueue.OnAudioEnded     += OnAudioEnded;
        _audioQueue.OnQueueCompleted += OnQueueCompleted;
        _audioQueue.OnError          += OnError;

        RefreshUiStrings();
        ResetState();
    }

    // ── Scan / Start Tour ─────────────────────────────────────────

    [RelayCommand]
    public async Task StartTourFromQrAsync(string? qrToken)
    {
        if (IsLoading) return;
        await HandleScanPayloadAsync(qrToken);
    }

    [RelayCommand]
    private async Task ResolveManualAsync() => await HandleScanPayloadAsync(ManualCode);

    public async Task HandleScanPayloadAsync(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || IsTourActive) return;

        var payload = raw.Trim();
        if (payload == _lastHandledPayload && (DateTime.UtcNow - _lastHandledAt).TotalSeconds < 2) return;
        if (!await _scanGate.WaitAsync(0)) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Đang tải danh sách audio...";

            var location = await GetCurrentLocationAsync();
            if (location is null)
            {
                StatusMessage = _localization.GetString("Tour_LocationError");
                return;
            }

            var response = await _tourService.StartTourAsync(
                payload,
                location.Latitude,
                location.Longitude,
                _localization.CurrentLanguageCode);

            if (response == null || !response.Success)
            {
                StatusMessage = response?.Message ?? _localization.GetString("Tour_StartError");
                _lastHandledPayload = string.Empty;
                return;
            }

            if (response.AudioQueue.Count == 0)
            {
                StatusMessage = _localization.GetString("Tour_NoAudio");
                _lastHandledPayload = string.Empty;
                return;
            }

            _lastHandledPayload = payload;
            _lastHandledAt = DateTime.UtcNow;
            ManualCode = string.Empty;

            TourSessionId  = response.TourSessionId ?? string.Empty;
            TotalStalls    = response.TotalStalls;
            CompletedStalls = 0;

            await _audioQueue.InitializeQueueAsync(response.AudioQueue);
            UpdateQueueInfo();

            IsTourActive = true;
            UpdateScanUiState();
            StatusMessage = string.Format(
                _localization.GetString("Tour_StartedFmt"),
                response.TotalStalls,
                FormatDuration(response.EstimatedDurationSeconds));

            // ── Phát audio intro trước (nếu có), sau đó StartAsync để kích hoạt Geofence
            if (!string.IsNullOrWhiteSpace(response.IntroAudioUrl))
            {
                await PlayIntroThenStartQueueAsync(response.IntroAudioUrl);
            }
            else
            {
                await _audioQueue.StartAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            _lastHandledPayload = string.Empty;
        }
        finally
        {
            IsLoading = false;
            _scanGate.Release();
        }
    }

    /// <summary>
    /// Phát audio intro phố → khi intro kết thúc (hoặc 120s timeout) → bắt đầu GeofenceMonitor.
    /// </summary>
    private async Task PlayIntroThenStartQueueAsync(string introUrl)
    {
        IsPlayingIntro = true;
        StatusMessage = "Đang phát giới thiệu phố ẩm thực...";

        var ok = await _audioPlayer.PlayAsync(introUrl);
        if (!ok)
        {
            // Intro thất bại → vẫn bắt đầu tour bình thường
            IsPlayingIntro = false;
            await _audioQueue.StartAsync();
            return;
        }

        // Chờ intro kết thúc (poll 500ms, timeout 120s)
        var timeout = DateTime.UtcNow.AddSeconds(120);
        while (_audioPlayer.IsPlaying && DateTime.UtcNow < timeout)
            await Task.Delay(500);

        _audioPlayer.Stop();
        IsPlayingIntro = false;

        StatusMessage = string.Format(
            _localization.GetString("Tour_StartedFmt"),
            TotalStalls,
            FormatDuration(0));

        await _audioQueue.StartAsync();
    }

    // ── Playback controls ─────────────────────────────────────────

    [RelayCommand]
    public async Task SkipAsync()
    {
        if (!IsTourActive || !CanSkip) return;
        await _audioQueue.SkipAsync();
    }

    [RelayCommand]
    public void PauseResume()
    {
        if (!IsTourActive) return;
        if (IsPaused)
        {
            _ = _audioQueue.ResumeAsync();
            IsPaused = false;
        }
        else
        {
            _audioQueue.Pause();
            IsPaused = true;
        }
    }

    [RelayCommand]
    public void StopTour()
    {
        _audioPlayer.Stop(); // dừng cả intro nếu đang phát
        _audioQueue.Stop();
        IsPlayingIntro = false;
        ResetState();
        StatusMessage = _localization.GetString("Tour_Stopped");
    }

    // ── Event handlers ────────────────────────────────────────────

    partial void OnIsTourActiveChanged(bool value) => UpdateScanUiState();

    private void OnAudioStarted(object? sender, AudioQueueItemDto item)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsPlaying  = true;
            IsPaused   = false;
            CanSkip    = true;
            CanPause   = true;

            CurrentPoiId       = item.PoiId;
            NowPlayingName     = item.PoiName;
            NowPlayingAddress  = item.Address ?? string.Empty;
            NowPlayingImage    = item.CoverImageUrl;
            NowPlayingDistance = FormatDistance(item.DistanceMeters);

            UpdateQueueInfo();
            StatusMessage = string.Format(_localization.GetString("Tour_PlayingFmt"), item.PoiName);
        });
    }

    private void OnAudioEnded(object? sender, AudioQueueItemDto item)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CompletedStalls++;
            UpdateQueueInfo();
        });
    }

    private void OnQueueCompleted(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsPlaying  = false;
            CanSkip    = false;
            CanPause   = false;
            NowPlayingName    = string.Empty;
            NowPlayingAddress = string.Empty;
            NowPlayingImage   = null;

            StatusMessage = string.Format(_localization.GetString("Tour_CompletedFmt"), TotalStalls);
        });
    }

    private void OnError(object? sender, string message)
    {
        MainThread.BeginInvokeOnMainThread(() => StatusMessage = message);
    }

    // ── Private helpers ───────────────────────────────────────────

    private void RefreshUiStrings()
    {
        UiTourTitle = _localization.GetString("Tour_Title");
        UiNowPlaying = _localization.GetString("Tour_NowPlaying");
        UiRemaining  = _localization.GetString("Tour_Remaining");
        UiSkip = _localization.GetString("Tour_Skip");
        UiStop = _localization.GetString("Tour_Stop");
    }

    private void ResetState()
    {
        IsTourActive = false;
        IsPlaying    = false;
        IsPaused     = false;
        CanSkip      = false;
        CanPause     = false;
        IsPlayingIntro   = false;
        NowPlayingName    = string.Empty;
        NowPlayingAddress = string.Empty;
        NowPlayingImage   = null;
        NowPlayingDistance = string.Empty;
        CurrentPoiId  = 0;
        RemainingCount = 0;
        RemainingDuration = string.Empty;
        TotalStalls    = 0;
        CompletedStalls = 0;
        StatusMessage = _localization.GetString("Tour_ScanToStart");
        UpdateScanUiState();
    }

    private void UpdateQueueInfo()
    {
        RemainingCount    = _audioQueue.RemainingCount;
        RemainingDuration = FormatDuration(_audioQueue.RemainingDurationSeconds);
    }

    private void UpdateScanUiState()
    {
        ShowManualEntry   = !IsTourActive;
        ShowCameraScanner = !IsTourActive && DeviceInfo.Platform != DevicePlatform.WinUI;
    }

    private static async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var last = await Geolocation.GetLastKnownLocationAsync();
            if (last != null) return last;
            return await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
        }
        catch { return null; }
    }

    private static string FormatDuration(int totalSeconds)
    {
        if (totalSeconds <= 0) return "0:00";
        return $"{totalSeconds / 60}:{totalSeconds % 60:D2}";
    }

    private string FormatDistance(double meters) =>
        meters < 1000 ? $"{meters:F0}m" : $"{meters / 1000:F1}km";

    public void Dispose()
    {
        _audioQueue.OnAudioStarted   -= OnAudioStarted;
        _audioQueue.OnAudioEnded     -= OnAudioEnded;
        _audioQueue.OnQueueCompleted -= OnQueueCompleted;
        _audioQueue.OnError          -= OnError;
    }
}
