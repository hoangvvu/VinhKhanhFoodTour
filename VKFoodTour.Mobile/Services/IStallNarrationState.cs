using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

/// <summary>Chuyển nội dung sau quét QR sang tab Nghe (tránh URL quá dài).</summary>
public interface IStallNarrationState
{
    void SetFromQr(QrResolveDto dto);
    /// <summary>Đọc mà không xóa — cho phép nhiều consumer đọc cùng state.</summary>
    QrResolveDto? Peek();
    /// <summary>Đọc và xóa — chỉ consumer cuối cùng mới gọi.</summary>
    QrResolveDto? Consume();
    string? PendingAudioUrl { get; set; }
}
