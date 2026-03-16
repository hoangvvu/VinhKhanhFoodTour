using System;
using System.Collections.Generic;

namespace VinhKhanhFoodTour.API.Models;

public partial class Poi
{
    public int PoiId { get; set; }

    public int? OwnerId { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Description { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public int? Radius { get; set; }

    public virtual ICollection<Food> Foods { get; set; } = new List<Food>();

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public virtual ICollection<Narration> Narrations { get; set; } = new List<Narration>();

    public virtual User? Owner { get; set; }

    public virtual ICollection<Qrcode> Qrcodes { get; set; } = new List<Qrcode>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<TrackingLog> TrackingLogs { get; set; } = new List<TrackingLog>();
}
