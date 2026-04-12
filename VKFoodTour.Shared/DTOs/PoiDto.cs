namespace VKFoodTour.Shared.DTOs
{
    public class PoiDto
    {
        public int PoiId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int Radius { get; set; }
        public int Priority { get; set; } = 1;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }
}