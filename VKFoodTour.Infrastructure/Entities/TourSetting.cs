using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VKFoodTour.Infrastructure.Entities;

/// <summary>
/// Bảng cấu hình tour (key-value). Được dùng để lưu:
/// - audio intro phố (key = "intro_audio_vi", "intro_audio_en", ...)
/// - các cài đặt toàn cục khác của tour
/// </summary>
[Table("TOUR_SETTINGS")]
public class TourSetting
{
    [Key]
    [Column("setting_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SettingId { get; set; }

    /// <summary>Key định danh: VD "intro_audio_vi", "intro_text_vi"</summary>
    [Column("setting_key")]
    [MaxLength(100)]
    public string SettingKey { get; set; } = string.Empty;

    /// <summary>Giá trị: URL file audio, hoặc text nội dung.</summary>
    [Column("setting_value")]
    public string? SettingValue { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
