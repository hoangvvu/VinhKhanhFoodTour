using System;
using System.Collections.Generic;

namespace VinhKhanhFoodTour.API.Models;

public partial class User
{
    public int UserId { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Role { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Poi> Pois { get; set; } = new List<Poi>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
