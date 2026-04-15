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
    private readonly ILocalizationService _localization;
    private readonly SemaphoreSlim _scanGate = new(1, 1);
    private string _lastHandledPayload = string.Empty;
    private DateTime _lastHandledAt = DateTime.MinValue;

    [ObservableProperty]
    private bool isTourActive;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string tourSessionId = string.Empty;

    [ObservableProperty]
    private string manualCode = string.Empty;

    [ObservableProperty]
    private bool showCameraScanner;

    [ObservableProperty]
    private bool showManualEntry;

    // Currently playing info
    [ObservableProperty]
    private string nowPlayingName = string.Empty;

    [ObservableProperty]
    private string nowPlayingAddress = string.Empty;

    [ObservableProperty]
    private string? nowPlayingImage;

    [ObservableProperty]
    private string nowPlayingDistance = string.Empty;

    [ObservableProperty]
    private int currentPoiId;

    // Queue info
    [ObservableProperty]
    private int remainingCount;

    [ObservableProperty]
    private string remainingDuration = string.Empty;

    [ObservableProperty]
    private int totalStalls;

    [ObservableProperty]
    private int completedStalls;

    // Playback state
    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private bool isPaused;

    [ObservableProperty]
    private bool canSkip;

    [ObservableProperty]
    private bool canPause;

    // UI Strings
    [ObservableProperty]
    private string uiTourTitle = string.Empty;

    [ObservableProperty]
    private string uiNowPlaying = string.Empty;

    [ObservableProperty]
    private string uiRemaining = string.Empty;

    [ObservableProperty]
    private string uiSkip = string.Empty;

    [ObservableProperty]
    private string uiStop = string.Empty;

    public ObservableCollection<AudioQueueItemDto> QueueItems => _audioQueue.Queue;

    public TourPlayerViewModel(
        ITourService tourService,
        IAudioQueueService audioQueue,
        ILocalizationService localization)
    {
        _tourService = tourService;
        _audioQueue = audioQueue;
        _localization = localization;

        _localization.LanguageChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(RefreshUiStrings);

        _audioQueue.OnAudioStarted += OnAudioStarted;
        _audioQueue.OnAudioEnded += OnAudioEnded;
        _audioQueue.OnQueueCompleted += OnQueueCompleted;
        _audioQueue.OnError += OnError;

        RefreshUiStrings();
        ResetState();
    }

    private void RefreshUiStrings()
    {
        UiTourTitle = _localization.GetString("Tour_Title");
        UiNowPlaying = _localization.GetString("Tour_NowPlaying");
        UiRemaining = _localization.GetString("Tour_Remaining");
        UiSkip = _localization.GetString("Tour_Skip");
        UiStop = _localization.GetString("Tour_Stop");
    }

    private void ResetState()
    {
        IsTourActive = false;
        IsPlaying = false;
        IsPaused = false;
        CanSkip = false;
        CanPause = false;
        NowPlayingName = string.Empty;
        NowPlayingAddress = string.Empty;
        NowPlayingImage = null;
        NowPlayingDistance = string.Empty;
        CurrentPoiId = 0;
        RemainingCount = 0;
        RemainingDuration = string.Empty;
        TotalStalls = 0;
        CompletedStalls = 0;
        StatusMessage = _localization.GetString("Tour_ScanToStart");
        UpdateScanUiState();
    }

    /// <summary>
    /// Bắt đầu tour sau khi quét QR.
    /// </summary>
    [RelayCommand]
    public async Task StartTourFromQrAsync(string? qrToken)
    {
        if (IsLoading) return;

        IsLoading = true;
        StatusMessage = _localization.GetString("Tour_Starting");

        try
        {
            // Get current location
            var location = await GetCurrentLocationAsync();
            if (location == null)
            {
                StatusMessage = _localization.GetString("Tour_LocationError");
                IsLoading = false;
                return;
            }

            // Call API to start tour
            var response = await _tourService.StartTourAsync(
                qrToken,
                location.Latitude,
                location.Longitude,
                _localization.CurrentLanguageCode);

            if (response == null || !response.Success)
            {
                StatusMessage = response?.Message ?? _localization.GetString("Tour_StartError");
                IsLoading = false;
                return;
            }

            if (response.AudioQueue.Count == 0)
            {
                StatusMessage = _localization.GetString("Tour_NoAudio");
                IsLoading = false;
                return;
            }

            // Initialize queue
            TourSessionId = response.TourSessionId ?? string.Empty;
            TotalStalls = response.TotalStalls;
            CompletedStalls = 0;

            await _audioQueue.InitializeQueueAsync(response.AudioQueue);
            UpdateQueueInfo();

            IsTourActive = true;
            StatusMessage = string.Format(
                _localization.GetString("Tour_StartedFmt"),
                response.TotalStalls,
                FormatDuration(response.EstimatedDurationSeconds));

            // Start playback
            await _audioQueue.StartAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[Tour] Start error: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ResolveManualAsync()
    {
        await HandleScanPayloadAsync(ManualCode);
    }

    public async Task HandleScanPayloadAsync(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || IsTourActive)
            return;

        var payload = raw.Trim();
        if (payload == _lastHandledPayload && (DateTime.UtcNow - _lastHandledAt).TotalSeconds < 2)
            return;

        if (!await _scanGate.WaitAsync(0))
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Dang tai danh sach audio...";

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

            TourSessionId = response.TourSessionId ?? string.Empty;
            TotalStalls = response.TotalStalls;
            CompletedStalls = 0;

            await _audioQueue.InitializeQueueAsync(response.AudioQueue);
            UpdateQueueInfo();

            IsTourActive = true;
            UpdateScanUiState();
            StatusMessage = string.Format(
                _localization.GetString("Tour_StartedFmt"),
                response.TotalStalls,
                FormatDuration(response.EstimatedDurationSeconds));

            await _audioQueue.StartAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Loi: {ex.Message}";
            _lastHandledPayload = string.Empty;
        }
        finally
        {
            IsLoading = false;
            _scanGate.Release();
        }
    }

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
        _audioQueue.Stop();
        ResetState();
        StatusMessage = _localization.GetString("Tour_Stopped");
    }

    partial void OnIsTourActiveChanged(bool value)
    {
        UpdateScanUiState();
    }

    private void OnAudioStarted(object? sender, AudioQueueItemDto item)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsPlaying = true;
            IsPaused = false;
            CanSkip = true;
            CanPause = true;

            CurrentPoiId = item.PoiId;
            NowPlayingName = item.PoiName;
            NowPlayingAddress = item.Address ?? string.Empty;
            NowPlayingImage = item.CoverImageUrl;
            NowPlayingDistance = FormatDistance(item.DistanceMeters);

            UpdateQueueInfo();
            StatusMessage = string.Format(
                _localization.GetString("Tour_PlayingFmt"),
                item.PoiName);
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
            IsPlaying = false;
            CanSkip = false;
            CanPause = false;
            NowPlayingName = string.Empty;
            NowPlayingAddress = string.Empty;
            NowPlayingImage = null;

            StatusMessage = string.Format(
                _localization.GetString("Tour_CompletedFmt"),
                TotalStalls);
        });
    }

    private void OnError(object? sender, string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusMessage = message;
        });
    }

    private void UpdateQueueInfo()
    {
        RemainingCount = _audioQueue.RemainingCount;
        RemainingDuration = FormatDuration(_audioQueue.RemainingDurationSeconds);
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
        catch
        {
            return null;
        }
    }

    private void UpdateScanUiState()
    {
        ShowManualEntry = !IsTourActive;
        ShowCameraScanner = !IsTourActive && DeviceInfo.Platform != DevicePlatform.WinUI;
    }

    private static string FormatDuration(int totalSeconds)
    {
        if (totalSeconds <= 0) return "0:00";

        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        return $"{minutes}:{seconds:D2}";
    }

    private string FormatDistance(double meters)
    {
        if (meters < 1000)
            return $"{meters:F0}m";
        return $"{meters / 1000:F1}km";
    }

    public void Dispose()
    {
        _audioQueue.OnAudioStarted -= OnAudioStarted;
        _audioQueue.OnAudioEnded -= OnAudioEnded;
        _audioQueue.OnQueueCompleted -= OnQueueCompleted;
        _audioQueue.OnError -= OnError;
    }
}
