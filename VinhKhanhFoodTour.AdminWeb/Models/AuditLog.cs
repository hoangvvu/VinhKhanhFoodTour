namespace VinhKhanhFoodTour.AdminWeb.Models;

public class AuditLog
{
    public int AuditId { get; set; }
    public int UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
