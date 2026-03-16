using System;
using System.Collections.Generic;

namespace VinhKhanhFoodTour.API.Models;

public partial class Qrcode
{
    public int QrId { get; set; }

    public int? PoiId { get; set; }

    public string? QrCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Poi? Poi { get; set; }
}
