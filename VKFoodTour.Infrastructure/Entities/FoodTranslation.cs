using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VKFoodTour.Infrastructure.Entities;

[Table("FOOD_TRANSLATIONS")]
public class FoodTranslation
{
    [Key]
    [Column("translation_id")]
    public int TranslationId { get; set; }

    [Column("food_id")]
    public int FoodId { get; set; }

    [Column("language_id")]
    public int LanguageId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [ForeignKey("FoodId")]
    public Food Food { get; set; } = null!;

    [ForeignKey("LanguageId")]
    public Language Language { get; set; } = null!;
}