namespace VKFoodTour.Shared.DTOs;

/// <summary>Gửi đánh giá ứng dụng từ du khách.</summary>
public class CreateAppFeedbackDto
{
    public string DeviceId { get; set; } = string.Empty;
    public byte Rating { get; set; }          // 1–5
    public string? Comment { get; set; }
    public string? AppVersion { get; set; }
}
