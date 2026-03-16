namespace VinhKhanhFoodTour.MobileApp.Models
{
    public class Poi
    {
        public int PoiId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string AudioUrl { get; set; } 
    }
}