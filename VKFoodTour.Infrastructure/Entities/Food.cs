using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Net.Mime.MediaTypeNames;

namespace VKFoodTour.Infrastructure.Entities;

[Table("FOODS")]
public class Food
{
    [Key]
    [Column("food_id")]
    public int FoodId { get; set; }

    [Column("poi_id")]
    public int PoiId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("price", TypeName = "decimal(10, 2)")]
    public decimal? Price { get; set; }

    [Column("is_available")]
    public bool IsAvailable { get; set; } = true;

    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

    [ForeignKey("PoiId")]
    public Poi Poi { get; set; } = null!;

    // Navigation properties
    public ICollection<FoodTranslation> Translations { get; set; } = new List<FoodTranslation>();
    public ICollection<Image> Images { get; set; } = new List<Image>();
}