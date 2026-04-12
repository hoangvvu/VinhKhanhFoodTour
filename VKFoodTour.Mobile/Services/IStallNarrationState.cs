using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

/// <summary>Chuyển nội dung sau quét QR sang tab Nghe (tránh URL quá dài).</summary>
public interface IStallNarrationState
{
    void SetFromQr(QrResolveDto dto);
    QrResolveDto? Consume();
}
