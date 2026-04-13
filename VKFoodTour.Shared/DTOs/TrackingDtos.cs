namespace VKFoodTour.Shared.DTOs;

public class TrackingLogRequestDto
{
    public string DeviceId { get; set; } = string.Empty;
    public int? PoiId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string EventType { get; set; } = "move";
    public int? ListenedDurationSec { get; set; }
    public string? LanguageCode { get; set; }
}
