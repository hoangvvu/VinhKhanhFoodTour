using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VKFoodTour.Infrastructure.Entities;

[Table("IMAGES")]
public class Image
{
    [Key]
    [Column("image_id")]
    public int ImageId { get; set; }

    [Column("poi_id")]
    public int? PoiId { get; set; }

    [Column("food_id")]
    public int? FoodId { get; set; }

    [Required]
    [MaxLength(500)]
    [Column("image_url")]
    public string ImageUrl { get; set; } = null!;

    [MaxLength(200)]
    [Column("alt_text")]
    public string? AltText { get; set; }

    [Column("is_cover")]
    public bool IsCover { get; set; } = false;

    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("PoiId")]
    public Poi? Poi { get; set; }

    [ForeignKey("FoodId")]
    public Food? Food { get; set; }
}