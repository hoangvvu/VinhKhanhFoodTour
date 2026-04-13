using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Infrastructure.Entities;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("recent")]
    public async Task<ActionResult<List<ReviewListItemDto>>> GetRecent([FromQuery] int take = 30)
    {
        take = Math.Clamp(take, 1, 100);
        var list = await _context.Reviews
            .AsNoTracking()
            .Join(_context.Pois.AsNoTracking().Where(p => p.IsActive),
                r => r.PoiId,
                p => p.PoiId,
                (r, p) => new { r, p.Name })
            .OrderByDescending(x => x.r.CreatedAt)
            .Take(take)
            .Select(x => new ReviewListItemDto
            {
                ReviewId = x.r.ReviewId,
                PoiId = x.r.PoiId,
                PoiName = x.Name,
                Rating = x.r.Rating,
                Comment = x.r.Comment,
                CreatedAt = x.r.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("poi/{poiId:int}")]
    public async Task<ActionResult<List<ReviewListItemDto>>> GetByPoi(int poiId)
    {
        var exists = await _context.Pois.AsNoTracking().AnyAsync(p => p.PoiId == poiId && p.IsActive);
        if (!exists)
            return NotFound();

        var poiName = await _context.Pois.AsNoTracking()
            .Where(p => p.PoiId == poiId)
            .Select(p => p.Name)
            .FirstAsync();

        var list = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.PoiId == poiId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewListItemDto
            {
                ReviewId = r.ReviewId,
                PoiId = r.PoiId,
                PoiName = poiName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<ReviewListItemDto>> Create([FromBody] CreateReviewDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DeviceId))
            return BadRequest("deviceId required.");

        if (dto.Rating is < 1 or > 5)
            return BadRequest("rating must be 1–5.");

        var poi = await _context.Pois.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PoiId == dto.PoiId && p.IsActive);
        if (poi is null)
            return NotFound();

        var entity = new Review
        {
            DeviceId = dto.DeviceId.Trim(),
            PoiId = dto.PoiId,
            Rating = dto.Rating,
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
            LanguageCode = string.IsNullOrWhiteSpace(dto.LanguageCode) ? null : dto.LanguageCode.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new ReviewListItemDto
        {
            ReviewId = entity.ReviewId,
            PoiId = entity.PoiId,
            PoiName = poi.Name,
            Rating = entity.Rating,
            Comment = entity.Comment,
            CreatedAt = entity.CreatedAt
        });
    }
}
