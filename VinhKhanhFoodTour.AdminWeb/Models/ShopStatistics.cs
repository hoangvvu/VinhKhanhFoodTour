namespace VinhKhanhFoodTour.AdminWeb.Models;

public class ShopStatistics
{
    public int ShopId { get; set; }
    public string? ShopName { get; set; }
    public int TotalVisits { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime StatisticsDate { get; set; }
}
