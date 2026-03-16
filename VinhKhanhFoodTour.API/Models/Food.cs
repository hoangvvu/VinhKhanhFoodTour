using System;
using System.Collections.Generic;

namespace VinhKhanhFoodTour.API.Models;

public partial class Food
{
    public int FoodId { get; set; }

    public int? PoiId { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public virtual Poi? Poi { get; set; }
}
