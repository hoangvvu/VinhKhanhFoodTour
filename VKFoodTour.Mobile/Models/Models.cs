using System.ComponentModel;
using SQLite;

namespace VKFoodTour.Mobile.Models;

// ── POI — Gian hàng ───────────────────────────────────────────
[Table("POIS")]
public class Poi : INotifyPropertyChanged
{
    [PrimaryKey, AutoIncrement]
    public int PoiId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Radius { get; set; } = 20;
    public int Priority { get; set; } = 1;
    public double Rating { get; set; } = 5.0; // Điểm sao (ví dụ: 4.8)
    public int ReviewCount { get; set; } = 0; // Số lượng đánh giá

    [Ignore] public string CoverEmoji { get; set; } = "🍜";

    /// <summary>Ảnh bìa từ API (đã là URL tuyệt đối sau khi map).</summary>
    [Ignore] public string? CoverImageUrl { get; set; }

    [Ignore] public bool HasCoverImage => !string.IsNullOrWhiteSpace(CoverImageUrl);

    [Ignore] public bool ShowEmojiFallback => string.IsNullOrWhiteSpace(CoverImageUrl);

    private bool _isFavorite;
    [Ignore]
    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (_isFavorite == value)
                return;
            _isFavorite = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFavorite)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FavoriteIcon)));
        }
    }

    [Ignore] public string FavoriteIcon => IsFavorite ? "♥" : "♡";

    public event PropertyChangedEventHandler? PropertyChanged;
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