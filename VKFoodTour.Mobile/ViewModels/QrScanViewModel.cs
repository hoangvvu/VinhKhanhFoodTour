using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.ViewModels;

public partial class QrScanViewModel : ObservableObject
{
    private readonly IDataService _data;
    private readonly IStallNarrationState _stallState;
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
    private string uiTitle = string.Empty;

    [ObservableProperty]
    private string uiManualLabel = string.Empty;

    [ObservableProperty]
    private string uiManualPlaceholder = string.Empty;

    [ObservableProperty]
    private string uiManualButton = string.Empty;

    public QrScanViewModel(IDataService data, IStallNarrationState stallState, ILocalizationService localization)
    {
        _data = data;
        _stallState = stallState;
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
        if (payload == _lastHandledPayload && (DateTime.UtcNow - _lastHandledAt).TotalSeconds < 2)
            return;

        await _scanGate.WaitAsync();
        try
        {
            StatusMessage = _localization.GetString("Qr_Lookup");
            var dto = await _data.ResolveQrAsync(payload);
            if (dto is null)
            {
                StatusMessage = _localization.GetString("Qr_NotFound");
                _lastHandledPayload = string.Empty;
                return;
            }

            _lastHandledPayload = payload;
            _lastHandledAt = DateTime.UtcNow;

            _stallState.SetFromQr(dto);
            await _data.TrackEventAsync(dto.PoiId, "qr_scan");
            StatusMessage = _localization.GetString("Qr_ReceivedFmt", dto.Name);
            ManualCode = string.Empty;

            // Một lần điều hướng: tab Gian hàng + trang chi tiết (tránh lệch stack giữa hai GoToAsync).
            await Shell.Current.GoToAsync(
                $"//stalls/StallDetailPage?poiId={dto.PoiId}&fromQr=true");
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.GetString("Qr_ErrorFmt", ex.Message);
            _lastHandledPayload = string.Empty;
        }
        finally
        {
            _scanGate.Release();
        }
    }
}
