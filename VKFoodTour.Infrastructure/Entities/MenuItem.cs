using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VKFoodTour.Infrastructure.Entities;

[Table("MENU_ITEMS")]
public class MenuItem
{
    [Key]
    [Column("item_id")]
    public int ItemId { get; set; }

    [Column("poi_id")]
    public int PoiId { get; set; }

    [Required]
    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("category")]
    [StringLength(50)]
    public string? Category { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "AVAILABLE";

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    [Column("image_url")]
    [StringLength(255)]
    public string? ImageUrl { get; set; }

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("audio_url")]
    [StringLength(255)]
    public string? AudioUrl { get; set; }
}