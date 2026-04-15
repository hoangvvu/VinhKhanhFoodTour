using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices.Sensors;
using VKFoodTour.Mobile.Models;
using VKFoodTour.Mobile.Services;
using VKFoodTour.Mobile.Views;

namespace VKFoodTour.Mobile.ViewModels;

public partial class QrScanViewModel : ObservableObject
{
    private readonly IDataService _data;
    private readonly ITourService _tourService;
    private readonly IAudioQueueService _audioQueue;
    private readonly ILocalizationService _localization;
    private readonly SemaphoreSlim _scanGate = new(1, 1);
    private string _lastHandledPayload = string.Empty;
    private DateTime _lastHandledAt = DateTime.MinValue;

    [ObservableProperty]
    private string statusMessage = "Đưa mã QR quán vào khung hình.";

    [ObservableProperty]
    private string manualCode = string.Empty;

    [ObservableProperty]
    private bool showCameraScanner = true;

    [ObservableProperty]
    private bool showManualEntry;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string uiTitle = string.Empty;

    [ObservableProperty]
    private string uiManualLabel = string.Empty;

    [ObservableProperty]
    private string uiManualPlaceholder = string.Empty;

    [ObservableProperty]
    private string uiManualButton = string.Empty;

    public QrScanViewModel(
        IDataService data,
        ITourService tourService,
        IAudioQueueService audioQueue,
        ILocalizationService localization)
    {
        _data = data;
        _tourService = tourService;
        _audioQueue = audioQueue;
        _localization = localization;
        _localization.LanguageChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(RefreshQrUiStrings);
        ShowManualEntry = DeviceInfo.Platform == DevicePlatform.WinUI;
        ShowCameraScanner = !ShowManualEntry;
        RefreshQrUiStrings();
        ApplyDefaultStatusMessage();
    }

    private void RefreshQrUiStrings()
    {
        UiTitle = _localization.GetString("Qr_Title");
        UiManualLabel = _localization.GetString("Qr_ManualLabel");
        UiManualPlaceholder = _localization.GetString("Qr_ManualPlaceholder");
        UiManualButton = _localization.GetString("Qr_ManualButton");
    }

    private void ApplyDefaultStatusMessage()
    {
        StatusMessage = ShowManualEntry
            ? _localization.GetString("Qr_StatusWindows")
            : _localization.GetString("Qr_StatusDefault");
    }

    [RelayCommand]
    private async Task ResolveManualAsync()
    {
        await HandleScanPayloadAsync(ManualCode);
    }

    public async Task HandleScanPayloadAsync(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;

        var payload = raw.Trim();

        // Debounce: tránh quét lại cùng mã trong 2 giây
        if (payload == _lastHandledPayload && (DateTime.UtcNow - _lastHandledAt).TotalSeconds < 2)
            return;

        if (!await _scanGate.WaitAsync(0))
            return;

        try
        {
            IsLoading = true;
            StatusMessage = _localization.GetString("Qr_Lookup");

            // Lấy vị trí người dùng
            var location = await TryGetCurrentLocationAsync();
            if (location == null)
            {
                StatusMessage = "❌ Không lấy được vị trí GPS. Vui lòng bật định vị.";
                return;
            }

            // Gọi API bắt đầu tour
            StatusMessage = "🎙️ Đang tải danh sách audio...";
            var response = await _tourService.StartTourAsync(
                payload,
                location.Latitude,
                location.Longitude,
                _localization.CurrentLanguageCode);

            if (response == null || !response.Success)
            {
                StatusMessage = response?.Message ?? "❌ Không thể bắt đầu tour.";
                _lastHandledPayload = string.Empty;
                return;
            }

            if (response.AudioQueue.Count == 0)
            {
                StatusMessage = "⚠️ Không tìm thấy audio nào. Vui lòng thử lại sau.";
                _lastHandledPayload = string.Empty;
                return;
            }

            _lastHandledPayload = payload;
            _lastHandledAt = DateTime.UtcNow;
            ManualCode = string.Empty;

            // Khởi tạo queue và bắt đầu phát
            await _audioQueue.InitializeQueueAsync(response.AudioQueue);

            StatusMessage = $"✓ Đã tải {response.TotalStalls} quán. Đang chuyển sang Player...";

            // Chuyển sang trang Tour Player
            await Shell.Current.GoToAsync($"//tour");

            // Bắt đầu phát audio
            await _audioQueue.StartAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Lỗi: {ex.Message}";
            _lastHandledPayload = string.Empty;
            System.Diagnostics.Debug.WriteLine($"[QR] Error: {ex}");
        }
        finally
        {
            IsLoading = false;
            _scanGate.Release();
        }
    }

    private static async Task<Location?> TryGetCurrentLocationAsync()
    {
        try
        {
            var last = await Geolocation.GetLastKnownLocationAsync();
            if (last is not null)
                return last;

            return await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QR] Location error: {ex.Message}");
            return null;
        }
    }
}
