namespace VKFoodTour.Shared.DTOs;

/// <summary>Gói sync cho mobile: bootstrap (since=null) hoặc delta (since=ISO8601 UTC).</summary>
public class SyncBootstrapDto
{
    /// <summary>Thời điểm phục vụ request, mobile lưu lại làm `since` cho lần sync kế tiếp.</summary>
    public DateTime ServerNowUtc { get; set; }

    public List<PoiDto> Pois { get; set; } = new();
    public List<LanguageListItemDto> Languages { get; set; } = new();

    /// <summary>Danh sách POI đã bị xoá/ngừng hoạt động kể từ `since`. Mobile cần xoá khỏi cache.</summary>
    public List<int> RemovedPoiIds { get; set; } = new();
}
