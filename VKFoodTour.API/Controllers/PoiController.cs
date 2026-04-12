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
    }
}