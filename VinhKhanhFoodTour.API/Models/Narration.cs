using System;
using System.Collections.Generic;

namespace VinhKhanhFoodTour.API.Models;

public partial class Narration
{
    public int NarrationId { get; set; }

    public int? PoiId { get; set; }

    public int? LanguageId { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public string? AudioUrl { get; set; }

    public virtual Language? Language { get; set; }

    public virtual Poi? Poi { get; set; }
}
