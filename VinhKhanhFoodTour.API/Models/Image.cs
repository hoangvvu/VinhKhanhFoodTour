using System;
using System.Collections.Generic;

namespace VinhKhanhFoodTour.API.Models;

public partial class Image
{
    public int ImageId { get; set; }

    public int? PoiId { get; set; }

    public int? FoodId { get; set; }

    public string? ImageUrl { get; set; }

    public virtual Food? Food { get; set; }

    public virtual Poi? Poi { get; set; }
}
