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
        public string MembershipTier { get; set; } = "Standard";
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }
}