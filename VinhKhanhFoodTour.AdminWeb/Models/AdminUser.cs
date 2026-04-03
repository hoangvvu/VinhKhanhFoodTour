namespace VinhKhanhFoodTour.AdminWeb.Models;

public class AdminUser
{
    public int UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string Role { get; set; } = "User"; // Admin, ShopManager, User
    public bool IsActive { get; set; } = true;
    public int? ManagedShopId { get; set; } // For shop managers
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }

    public virtual ManagedShop? ManagedShop { get; set; }
}
