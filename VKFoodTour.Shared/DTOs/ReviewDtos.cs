namespace VKFoodTour.Shared.DTOs;

public class ReviewListItemDto
{
    public int ReviewId { get; set; }
    public int PoiId { get; set; }
    public string PoiName { get; set; } = string.Empty;
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewDto
{
    public string DeviceId { get; set; } = string.Empty;
    public int PoiId { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public string? LanguageCode { get; set; }
}
