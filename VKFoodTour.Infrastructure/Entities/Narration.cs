using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VKFoodTour.Infrastructure.Entities;

[Table("NARRATIONS")]
public class Narration
{
    [Key]
    [Column("narration_id")]
    public int NarrationId { get; set; }

    [Column("poi_id")]
    public int PoiId { get; set; }

    [Column("language_id")]
    public int LanguageId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = null!;

    [Required]
    [Column("content")]
    public string Content { get; set; } = null!;

    [MaxLength(100)]
    [Column("tts_voice")]
    public string? TtsVoice { get; set; }

    /// <summary>File âm thanh thuyết minh đã lưu (đường dẫn /uploads/...).</summary>
    [MaxLength(500)]
    [Column("audio_url")]
    public string? AudioUrl { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("PoiId")]
    public Poi Poi { get; set; } = null!;

    [ForeignKey("LanguageId")]
    public Language Language { get; set; } = null!;
}