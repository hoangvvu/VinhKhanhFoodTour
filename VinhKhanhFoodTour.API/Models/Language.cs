using System;
using System.Collections.Generic;

namespace VinhKhanhFoodTour.API.Models;

public partial class Language
{
    public int LanguageId { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<Narration> Narrations { get; set; } = new List<Narration>();
}
