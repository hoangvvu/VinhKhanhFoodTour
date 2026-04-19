using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VKFoodTour.Infrastructure.Entities;

[Table("APP_FEEDBACK")]
public class AppFeedback
{
    [Key]
    [Column("feedback_id")]
    public int FeedbackId { get; set; }

    [Required]
    [Column("device_id")]
    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;

    [Column("rating")]
    public byte Rating { get; set; }   // 1–5

    [Column("comment")]
    [MaxLength(1000)]
    public string? Comment { get; set; }

    [Column("app_version")]
    [MaxLength(50)]
    public string? AppVersion { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
