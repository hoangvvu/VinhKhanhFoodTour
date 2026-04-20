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
    private readonly ILocalizationService _localization;
    private readonly IStallNarrationState _stallNarrationState;
    private readonly SemaphoreSlim _scanGate = new(1, 1);
    private string _lastHandledPayload = string.Empty;
    private DateTime _lastHandledAt = DateTime.MinValue;

    [ObservableProperty]
    private string statusMessage = string.Empty;

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

    [ObservableProperty]
    private string uiCameraHint = string.Empty;

    public QrScanViewModel(
        IDataService data,
        ILocalizationService localization,
        IStallNarrationState stallNarrationState)
    {
        _data = data;
        _localization = localization;
        _stallNarrationState = stallNarrationState;
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
        UiCameraHint = _localization.GetString("Qr_CameraHint");
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

            var resolved = await _data.ResolveQrAsync(payload, _localization.CurrentLanguageCode);

            if (resolved == null)
            {
                StatusMessage = _localization.GetString("Qr_NotFound");
                _lastHandledPayload = string.Empty;
                return;
            }

            _lastHandledPayload = payload;
            _lastHandledAt = DateTime.UtcNow;
            ManualCode = string.Empty;

            if (resolved.IsTour)
            {
                // Nếu là mã Tour tổng: Chuyển sang tab Tour và kích hoạt nạp hàng đợi
                await Shell.Current.GoToAsync($"//tour?qrToken={payload}");
            }
            else
            {
                // Nếu là mã quán lẻ: Chuyển sang trang Chi tiết gian hàng
                if (!string.IsNullOrWhiteSpace(resolved.AudioUrl))
                    _stallNarrationState.PendingAudioUrl = resolved.AudioUrl;

                await Shell.Current.GoToAsync($"stalldetail?poiId={resolved.PoiId}&fromQr=true");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.GetString("Qr_ErrorFmt", ex.Message);
            _lastHandledPayload = string.Empty;
            System.Diagnostics.Debug.WriteLine($"[QR] Error: {ex}");
        }
        finally
        {
            IsLoading = false;
            _scanGate.Release();
        }
    }
}
