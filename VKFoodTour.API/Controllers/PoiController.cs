using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Shared.DTOs; // <-- Thêm dòng này

namespace VKFoodTour.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PoiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PoiDto>>> GetAllPois([FromQuery] string? lang = null)
        {
            var targetCode = NormalizeLanguageCode(lang);

            var pois = await _context.Pois
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .Select(p => new PoiDto
                {
                    PoiId = p.PoiId,
                    Name =
                        _context.Narrations
                            .Where(n => n.PoiId == p.PoiId && n.IsActive && n.Language != null && n.Language.Code == targetCode)
                            .OrderByDescending(n => n.UpdatedAt ?? DateTime.MinValue)
                            .ThenByDescending(n => n.NarrationId)
                            .Select(n => n.Title)
                            .FirstOrDefault()
                        ??
                        _context.Narrations
                            .Where(n => n.PoiId == p.PoiId && n.IsActive && n.Language != null && n.Language.Code == "vi")
                            .OrderByDescending(n => n.UpdatedAt ?? DateTime.MinValue)
                            .ThenByDescending(n => n.NarrationId)
                            .Select(n => n.Title)
                            .FirstOrDefault()
                        ??
                        p.Name,
                    Address = p.Address,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    Radius = p.Radius,
                    Priority = p.Priority,
                    Description =
                        _context.Narrations
                            .Where(n => n.PoiId == p.PoiId && n.IsActive && n.Language != null && n.Language.Code == targetCode)
                            .OrderByDescending(n => n.UpdatedAt ?? DateTime.MinValue)
                            .ThenByDescending(n => n.NarrationId)
                            .Select(n => n.Content)
                            .FirstOrDefault()
                        ??
                        _context.Narrations
                            .Where(n => n.PoiId == p.PoiId && n.IsActive && n.Language != null && n.Language.Code == "vi")
                            .OrderByDescending(n => n.UpdatedAt ?? DateTime.MinValue)
                            .ThenByDescending(n => n.NarrationId)
                            .Select(n => n.Content)
                            .FirstOrDefault()
                        ??
                        p.Description,
                    ImageUrl = p.ImageUrl
                })
                .ToListAsync();

            return Ok(pois);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PoiDto>> GetPoiById(int id, [FromQuery] string? lang = null)
        {
            var targetCode = NormalizeLanguageCode(lang);

            var dto = await _context.Pois
                .AsNoTracking()
                .Where(p => p.PoiId == id && p.IsActive)
                .Select(p => new PoiDto
                {
                    PoiId = p.PoiId,
                    Name =
                        _context.Narrations
                            .Where(n => n.PoiId == p.PoiId && n.IsActive && n.Language != null && n.Language.Code == targetCode)
                            .OrderByDescending(n => n.UpdatedAt ?? DateTime.MinValue)
                            .ThenByDescending(n => n.NarrationId)
                            .Select(n => n.Title)
                            .FirstOrDefault()
                        ??
                        _context.Narrations
                            .Where(n => n.PoiId == p.PoiId && n.IsActive && n.Language != null && n.Language.Code == "vi")
                            .OrderByDescending(n => n.UpdatedAt ?? DateTime.MinValue)
                            .ThenByDescending(n => n.NarrationId)
                            .Select(n => n.Title)
                            .FirstOrDefault()
                        ??
                        p.Name,
                    Address = p.Address,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    Radius = p.Radius,
                    Priority = p.Priority,
                    Description =
                        _context.Narrations
                            .Where(n => n.PoiId == p.PoiId && n.IsActive && n.Language != null && n.Language.Code == targetCode)
                            .OrderByDescending(n => n.UpdatedAt ?? DateTime.MinValue)
                            .ThenByDescending(n => n.NarrationId)
                            .Select(n => n.Content)
                            .FirstOrDefault()
                        ??
                        _context.Narrations
                            .Where(n => n.PoiId == p.PoiId && n.IsActive && n.Language != null && n.Language.Code == "vi")
                            .OrderByDescending(n => n.UpdatedAt ?? DateTime.MinValue)
                            .ThenByDescending(n => n.NarrationId)
                            .Select(n => n.Content)
                            .FirstOrDefault()
                        ??
                        p.Description,
                    ImageUrl = p.ImageUrl
                })
                .FirstOrDefaultAsync();

            if (dto is null)
                return NotFound();

            return Ok(dto);
        }

        [HttpGet("{id:int}/detail")]
        public async Task<ActionResult<PoiDetailDto>> GetPoiDetail(int id, [FromQuery] string? lang = null)
        {
            var targetCode = NormalizeLanguageCode(lang);
            var poi = await _context.Pois
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PoiId == id && p.IsActive);
            if (poi is null)
                return NotFound();

            var gallery = await _context.Images
                .AsNoTracking()
                .Where(i => i.PoiId == id && i.FoodId == null && !i.IsCover)
                .OrderBy(i => i.SortOrder)
                .Select(i => new ImageItemDto
                {
                    ImageId = i.ImageId,
                    Url = i.ImageUrl,
                    AltText = i.AltText
                })
                .ToListAsync();

            var menuItems = await _context.MenuItems
                .AsNoTracking()
                .Where(m => m.PoiId == id)
                .OrderBy(m => m.Category)
                .ThenBy(m => m.Name)
                .Select(m => new MenuItemDto
                {
                    ItemId = m.ItemId,
                    Name = m.Name,
                    Category = m.Category,
                    Price = m.Price,
                    Description = m.Description,
                    ImageUrl = m.ImageUrl,
                    AudioUrl = m.AudioUrl
                })
                .ToListAsync();

            var preferredNarration = await _context.Narrations
                .AsNoTracking()
                .Include(n => n.Language)
                .Where(n => n.PoiId == id && n.IsActive)
                .OrderByDescending(n => n.Language != null && n.Language.Code == targetCode)
                .ThenByDescending(n => n.Language != null && n.Language.Code == "vi")
                .ThenByDescending(n => n.UpdatedAt ?? DateTime.MinValue)
                .ThenByDescending(n => n.NarrationId)
                .FirstOrDefaultAsync();

            var autoNarrations = await _context.Narrations
                .AsNoTracking()
                .Include(n => n.Language)
                .Where(n => n.PoiId == id && n.IsActive &&
                            ((!string.IsNullOrEmpty(n.AudioUrlAuto)) || (!string.IsNullOrEmpty(n.AudioUrl))))
                .OrderBy(n => n.LanguageId)
                .Select(n => new AudioItemDto
                {
                    Title = n.Title,
                    LanguageCode = n.Language != null ? n.Language.Code : null,
                    Url = n.AudioUrlAuto ?? n.AudioUrl!,
                    SourceType = "auto_nearby"
                })
                .ToListAsync();

            var qrNarrations = await _context.Narrations
                .AsNoTracking()
                .Include(n => n.Language)
                .Where(n => n.PoiId == id && n.IsActive && !string.IsNullOrEmpty(n.AudioUrlQr))
                .OrderBy(n => n.LanguageId)
                .Select(n => new AudioItemDto
                {
                    Title = $"{n.Title} (QR)",
                    LanguageCode = n.Language != null ? n.Language.Code : null,
                    Url = n.AudioUrlQr!,
                    SourceType = "qr_scan"
                })
                .ToListAsync();

            var menuAudios = menuItems
                .Where(m => !string.IsNullOrWhiteSpace(m.AudioUrl))
                .Select(m => new AudioItemDto
                {
                    Title = m.Name,
                    LanguageCode = null,
                    Url = m.AudioUrl!,
                    SourceType = "menu"
                });

            var dto = new PoiDetailDto
            {
                PoiId = poi.PoiId,
                Name = preferredNarration?.Title ?? poi.Name,
                Address = poi.Address,
                Description = preferredNarration?.Content ?? poi.Description,
                CoverImageUrl = !string.IsNullOrWhiteSpace(poi.ImageUrl)
                    ? poi.ImageUrl
                    : await _context.Images.AsNoTracking()
                        .Where(i => i.PoiId == id && i.FoodId == null && i.IsCover)
                        .OrderBy(i => i.SortOrder)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefaultAsync(),
                GalleryImages = gallery,
                MenuItems = menuItems,
                AudioItems = autoNarrations.Concat(qrNarrations).Concat(menuAudios).ToList()
            };

            return Ok(dto);
        }

        private static string NormalizeLanguageCode(string? lang)
        {
            if (string.IsNullOrWhiteSpace(lang))
                return "vi";

            var code = lang.Trim().ToLowerInvariant();
            var dash = code.IndexOf('-');
            return dash > 0 ? code[..dash] : code;
        }
    }
}