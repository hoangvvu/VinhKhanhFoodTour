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
        public async Task<ActionResult<IEnumerable<PoiDto>>> GetAllPois() // <-- Đổi kiểu trả về
        {
            var pois = await _context.Pois
                .Select(p => new PoiDto // <-- Sử dụng DTO dùng chung
                {
                    PoiId = p.PoiId,
                    Name = p.Name,
                    Address = p.Address,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    Radius = p.Radius
                })
                .ToListAsync();

            if (pois == null || pois.Count == 0)
            {
                return NotFound("Chưa có dữ liệu POI nào trong hệ thống.");
            }

            return Ok(pois);
        }
    }
}