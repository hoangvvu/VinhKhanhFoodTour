using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VKFoodTour.Infrastructure.Entities;

[Table("REVIEWS")]
public class Review
{
    [Key]
    [Column("review_id")]
    public int ReviewId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("device_id")]
    public string DeviceId { get; set; } = null!;

    [Column("poi_id")]
    public int PoiId { get; set; }

    [Required]
    [Column("rating")]
    [Range(1, 5)] // Ràng buộc theo Check constraint (1-5)
    public byte Rating { get; set; }

    [MaxLength(1000)]
    [Column("comment")]
    public string? Comment { get; set; }

    [MaxLength(10)]
    [Column("language_code")]
    public string? LanguageCode { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("PoiId")]
    public Poi Poi { get; set; } = null!;
}