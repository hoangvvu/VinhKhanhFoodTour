namespace VKFoodTour.Shared.DTOs;

/// <summary>
/// Một item trong hàng đợi audio của tour.
/// Mobile sẽ nhận list này và phát lần lượt.
/// </summary>
public class AudioQueueItemDto
{
    public int PoiId { get; set; }
    public string PoiName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Description { get; set; }
    
    /// <summary>URL audio đã normalize (absolute URL).</summary>
    public string AudioUrl { get; set; } = string.Empty;
    
    /// <summary>Khoảng cách từ vị trí người dùng (meters).</summary>
    public double DistanceMeters { get; set; }
    
    /// <summary>Thời lượng audio ước tính (seconds). 0 nếu chưa biết.</summary>
    public int DurationSeconds { get; set; }
    
    /// <summary>Thứ tự sắp xếp mặc định từ admin (thấp = ưu tiên cao).</summary>
    public int SortOrder { get; set; }
    
    /// <summary>Mã ngôn ngữ của audio (vi, en, zh...).</summary>
    public string LanguageCode { get; set; } = "vi";
    
    /// <summary>Ảnh đại diện của quán.</summary>
    public string? CoverImageUrl { get; set; }
    
    /// <summary>Latitude của POI.</summary>
    public double Latitude { get; set; }
    
    /// <summary>Longitude của POI.</summary>
    public double Longitude { get; set; }
}

/// <summary>
/// Request để bắt đầu tour sau khi quét QR.
/// </summary>
public class StartTourRequestDto
{
    /// <summary>Token từ mã QR (có thể là mã quán cụ thể hoặc mã chung cả phố).</summary>
    public string? QrToken { get; set; }
    
    /// <summary>Vị trí hiện tại của người dùng.</summary>
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    /// <summary>Device ID để tracking.</summary>
    public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>Ngôn ngữ ưu tiên (vi, en, zh...).</summary>
    public string LanguageCode { get; set; } = "vi";
}

/// <summary>
/// Response khi bắt đầu tour - chứa toàn bộ audio queue.
/// </summary>
public class StartTourResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    
    /// <summary>ID session để tracking analytics.</summary>
    public string? TourSessionId { get; set; }
    
    /// <summary>Danh sách audio đã sắp xếp theo khoảng cách.</summary>
    public List<AudioQueueItemDto> AudioQueue { get; set; } = new();
    
    /// <summary>Tổng số quán trong tour.</summary>
    public int TotalStalls { get; set; }
    
    /// <summary>Tổng thời lượng ước tính (seconds).</summary>
    public int EstimatedDurationSeconds { get; set; }
    
    /// <summary>Tên quán bắt đầu (quán gần nhất hoặc quán được quét QR).</summary>
    public string? StartingPoiName { get; set; }
}

/// <summary>
/// Request lấy audio queue theo vị trí (không cần QR).
/// </summary>
public class GetAudioQueueRequestDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string LanguageCode { get; set; } = "vi";
    
    /// <summary>Giới hạn số lượng POI trả về (mặc định: tất cả).</summary>
    public int? Limit { get; set; }
}
