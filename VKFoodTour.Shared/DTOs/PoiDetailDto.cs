namespace VKFoodTour.Shared.DTOs;

public class PoiDetailDto
{
    public int PoiId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public List<ImageItemDto> GalleryImages { get; set; } = new();
    public List<MenuItemDto> MenuItems { get; set; } = new();
    public List<AudioItemDto> AudioItems { get; set; } = new();
}

public class ImageItemDto
{
    public int ImageId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
}

public class MenuItemDto
{
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
}

public class AudioItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? LanguageCode { get; set; }
    public string Url { get; set; } = string.Empty;
    public string SourceType { get; set; } = "narration";
}
