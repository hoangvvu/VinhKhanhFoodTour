using SQLite;

namespace VKFoodTour.Mobile.Models;

// ── POI — Gian hàng ───────────────────────────────────────────
[Table("POIS")]
public class Poi
{
    [PrimaryKey, AutoIncrement]
    public int PoiId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Radius { get; set; } = 20;
    public int Priority { get; set; } = 1;

    [Ignore] public string CoverEmoji { get; set; } = "🍜";
}

// ── Narration — Nội dung thuyết minh ─────────────────────────
[Table("NARRATIONS")]
public class Narration
{
    [PrimaryKey, AutoIncrement]
    public int NarrationId { get; set; }
    public int PoiId { get; set; }
    public int LanguageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

// ── TrackingLog — Ghi log vị trí ──────────────────────────────
[Table("TRACKING_LOGS")]
public class TrackingLog
{
    [PrimaryKey, AutoIncrement]
    public long LogId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string EventType { get; set; } = "move";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}