namespace VKFoodTour.Shared.DTOs;

/// <summary>
/// Kết quả đếm số thiết bị đang online (có heartbeat trong N phút gần nhất).
/// </summary>
public class OnlineCountDto
{
    public int OnlineCount { get; set; }
    public int WindowMinutes { get; set; }
    public DateTime ServerTimeUtc { get; set; }
}

/// <summary>
/// Điểm heatmap: lat/lng + cường độ (weight) để plugin leaflet.heat render.
/// </summary>
public class HeatmapPointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Weight { get; set; }
}

/// <summary>
/// Phản hồi endpoint /api/Tracking/heatmap.
/// </summary>
public class HeatmapResponseDto
{
    public int Hours { get; set; }
    public string? EventType { get; set; }
    public int TotalPoints { get; set; }
    public List<HeatmapPointDto> Points { get; set; } = new();
}
