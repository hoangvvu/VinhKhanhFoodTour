using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VKFoodTour.Infrastructure.Entities;

[Table("TRACKING_LOGS")]
public class TrackingLog
{
    [Key]
    [Column("log_id")]
    public long LogId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("device_id")]
    public string DeviceId { get; set; } = null!;

    [Column("poi_id")]
    public int? PoiId { get; set; }

    [Column("latitude", TypeName = "decimal(10, 8)")]
    public decimal Latitude { get; set; }

    [Column("longitude", TypeName = "decimal(11, 8)")]
    public decimal Longitude { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("event_type")]
    public string EventType { get; set; } = "move"; // Các giá trị: move, enter, exit, qr_scan, listen_start, listen_end

    [Column("listened_duration_sec")]
    public int? ListenedDurationSec { get; set; }

    [MaxLength(10)]
    [Column("language_code")]
    public string? LanguageCode { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("PoiId")]
    public Poi? Poi { get; set; }
}