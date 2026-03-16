using System;
using System.Collections.Generic;

namespace VinhKhanhFoodTour.API.Models;

public partial class TrackingLog
{
    public long LogId { get; set; }

    public string? DeviceId { get; set; }

    public int? PoiId { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Poi? Poi { get; set; }
}
