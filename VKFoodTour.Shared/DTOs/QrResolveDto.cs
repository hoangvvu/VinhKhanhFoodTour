namespace VKFoodTour.Shared.DTOs;

/// <summary>Kết quả tra cứu QR (token trong mã trùng bảng QRCODES, nội dung từ POI + NARRATIONS).</summary>
public class QrResolveDto
{
    public int PoiId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Description { get; set; }
    public string? NarrationTitle { get; set; }
    public string? NarrationContent { get; set; }
    public string? AudioUrl { get; set; }
    public string? LanguageCode { get; set; }
    public bool IsTour { get; set; }
}
