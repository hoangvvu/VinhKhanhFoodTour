using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VKFoodTour.Infrastructure.Entities;

[Table("POIS")]
public class Poi
{
    [Key]
    [Column("poi_id")]
    public int PoiId { get; set; }

    [Column("owner_id")]
    public int? OwnerId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = null!;

    [MaxLength(255)]
    [Column("address")]
    public string? Address { get; set; }

    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [Column("latitude", TypeName = "decimal(10, 8)")]
    public decimal Latitude { get; set; }

    [Column("longitude", TypeName = "decimal(11, 8)")]
    public decimal Longitude { get; set; }

    [Column("radius")]
    [Range(5, 200)]
    public int Radius { get; set; } = 20;

    [Column("priority")]
    [Range(1, 5)]
    public int Priority { get; set; } = 1;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // --- 2 TRƯỜNG MỚI THÊM VÀO ---
    [Column("description")]
    public string? Description { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    [Column("rejection_note")]
    public string? RejectionNote { get; set; }

    // Navigation properties
    public ICollection<Narration> Narrations { get; set; } = new List<Narration>();
    public ICollection<QrCode> QrCodes { get; set; } = new List<QrCode>();
    public ICollection<Image> Images { get; set; } = new List<Image>();
}