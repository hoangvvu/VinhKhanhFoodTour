using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.ViewModels;

public partial class QrScanViewModel : ObservableObject
{
    private readonly IDataService _data;
    private readonly IStallNarrationState _stallState;
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

    public QrScanViewModel(IDataService data, IStallNarrationState stallState)
    {
        _data = data;
        _stallState = stallState;
        ShowManualEntry = DeviceInfo.Platform == DevicePlatform.WinUI;
        ShowCameraScanner = !ShowManualEntry;
        if (ShowManualEntry)
            StatusMessage = "Windows: nhập nội dung mã QR (vd: vkfoodtour://VK-XXXX hoặc chỉ mã VK-XXXX).";
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

        try
        {
            StatusMessage = "Đang tra cứu…";
            var dto = await _data.ResolveQrAsync(payload);
            if (dto is null)
            {
                StatusMessage = "Không tìm thấy quán hoặc mã QR đã tắt.";
                _lastHandledPayload = string.Empty;
                return;
            }

            _lastHandledPayload = payload;
            _lastHandledAt = DateTime.UtcNow;

            _stallState.SetFromQr(dto);
            StatusMessage = $"Đã nhận: {dto.Name}";
            ManualCode = string.Empty;
            await Shell.Current.GoToAsync($"//stalls");
            await Shell.Current.GoToAsync($"StallDetailPage?poiId={dto.PoiId}&fromQr=true");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            _lastHandledPayload = string.Empty;
        }
    }
}
