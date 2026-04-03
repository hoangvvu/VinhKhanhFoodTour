namespace VinhKhanhFoodTour.AdminWeb.Models;

public class ManagedShop
{
    public int ShopId { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? Radius { get; set; }
    public int? OwnerId { get; set; }
    public bool IsVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int TotalOrders { get; set; } = 0;
    public decimal AverageRating { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AdminUser> Managers { get; set; } = new List<AdminUser>();
}
