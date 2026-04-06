using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Admin.Models
{
    /// <summary>
    /// Gian hàng / Điểm tham quan (Point of Interest)
    /// Map với bảng [POIS] trong SQL Server
    /// </summary>
    [Table("POIS")]  // Chỉ định rõ tên bảng trong DB
    public class Poi
    {
        // ── Khóa chính ──────────────────────────────────

        [Key]
        [Column("poi_id")]
        public int PoiId { get; set; }

        // ── Quan hệ: Chủ gian hàng ─────────────────────

        [Column("owner_id")]
        public int? OwnerId { get; set; }

        // ── Thông tin cơ bản ────────────────────────────

        [Required(ErrorMessage = "Tên gian hàng không được để trống")]
        [StringLength(200)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("address")]
        public string? Address { get; set; }

        [StringLength(20)]
        [Column("phone")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        // ── Tọa độ GPS ─────────────────────────────────

        [Required(ErrorMessage = "Vĩ độ không được để trống")]
        [Column("latitude", TypeName = "decimal(10,8)")]
        [Range(-90, 90, ErrorMessage = "Vĩ độ phải từ -90 đến 90")]
        public decimal Latitude { get; set; }

        [Required(ErrorMessage = "Kinh độ không được để trống")]
        [Column("longitude", TypeName = "decimal(11,8)")]
        [Range(-180, 180, ErrorMessage = "Kinh độ phải từ -180 đến 180")]
        public decimal Longitude { get; set; }

        // ── Geofence ────────────────────────────────────

        [Column("radius")]
        [Range(5, 200, ErrorMessage = "Bán kính phải từ 5 đến 200 mét")]
        public int Radius { get; set; } = 20;

        [Column("priority")]
        [Range(1, 5, ErrorMessage = "Mức ưu tiên phải từ 1 đến 5")]
        public int Priority { get; set; } = 1;

        // ── Trạng thái ─────────────────────────────────

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}