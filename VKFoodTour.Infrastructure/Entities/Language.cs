using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VKFoodTour.Infrastructure.Entities;

[Table("LANGUAGES")]
public class Language
{
    [Key]
    [Column("language_id")]
    public int LanguageId { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("code")]
    public string Code { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    [Column("tts_voice")]
    public string? TtsVoice { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<Narration> Narrations { get; set; } = new List<Narration>();
}