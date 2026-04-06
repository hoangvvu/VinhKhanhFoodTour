using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VKFoodTour.Infrastructure.Entities;

[Table("QRCODES")]
public class QrCode
{
    [Key]
    [Column("qr_id")]
    public int QrId { get; set; }

    [Column("poi_id")]
    public int PoiId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("qr_token")]
    public string QrToken { get; set; } = null!;

    [MaxLength(200)]
    [Column("location_note")]
    public string? LocationNote { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("PoiId")]
    public Poi Poi { get; set; } = null!;
}