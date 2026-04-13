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
        public async Task<ActionResult<IEnumerable<PoiDto>>> GetAllPois()
        {
            var pois = await _context.Pois
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .Select(p => new PoiDto
                {
                    PoiId = p.PoiId,
                    Name = p.Name,
                    Address = p.Address,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    Radius = p.Radius,
                    Priority = p.Priority,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl
                })
                .ToListAsync();

            return Ok(pois);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PoiDto>> GetPoiById(int id)
        {
            var dto = await _context.Pois
                .AsNoTracking()
                .Where(p => p.PoiId == id && p.IsActive)
                .Select(p => new PoiDto
                {
                    PoiId = p.PoiId,
                    Name = p.Name,
                    Address = p.Address,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    Radius = p.Radius,
                    Priority = p.Priority,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl
                })
                .FirstOrDefaultAsync();

            if (dto is null)
                return NotFound();

            return Ok(dto);
        }

        [HttpGet("{id:int}/detail")]
        public async Task<ActionResult<PoiDetailDto>> GetPoiDetail(int id)
        {
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
                .Where(m => m.PoiId == id && m.Status == "AVAILABLE")
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

            var narrations = await _context.Narrations
                .AsNoTracking()
                .Include(n => n.Language)
                .Where(n => n.PoiId == id && n.IsActive && n.AudioUrl != null && n.AudioUrl != "")
                .OrderBy(n => n.LanguageId)
                .Select(n => new AudioItemDto
                {
                    Title = n.Title,
                    LanguageCode = n.Language != null ? n.Language.Code : null,
                    Url = n.AudioUrl!,
                    SourceType = "narration"
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
                Name = poi.Name,
                Address = poi.Address,
                Description = poi.Description,
                CoverImageUrl = poi.ImageUrl,
                GalleryImages = gallery,
                MenuItems = menuItems,
                AudioItems = narrations.Concat(menuAudios).ToList()
            };

            return Ok(dto);
        }
    }
}