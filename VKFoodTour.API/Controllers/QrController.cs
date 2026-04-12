using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QrController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public QrController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>Tra cứu mã QR: token thuần (VD: VK-ABC12345) hoặc chuỗi quét được dạng vkfoodtour://TOKEN.</summary>
    [HttpGet("resolve/{token}")]
    public async Task<ActionResult<QrResolveDto>> Resolve(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest();

        var normalized = Uri.UnescapeDataString(token).Trim();
        if (normalized.StartsWith("vkfoodtour://", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["vkfoodtour://".Length..].Trim();
        else if (normalized.StartsWith("vkfoodtour:", StringComparison.OrdinalIgnoreCase))
        {
            var idx = normalized.IndexOf("//", StringComparison.Ordinal);
            if (idx >= 0)
                normalized = normalized[(idx + 2)..].Trim();
        }

        if (string.IsNullOrEmpty(normalized))
            return BadRequest();

        var qr = await _context.QrCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.QrToken == normalized && q.IsActive);

        if (qr is null)
            return NotFound();

        var poi = await _context.Pois
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PoiId == qr.PoiId && p.IsActive);

        if (poi is null)
            return NotFound();

        var narrations = await _context.Narrations
            .AsNoTracking()
            .Include(n => n.Language)
            .Where(n => n.PoiId == poi.PoiId && n.IsActive)
            .ToListAsync();

        var pick = narrations
            .OrderByDescending(n => string.Equals(n.Language?.Code, "vi", StringComparison.OrdinalIgnoreCase))
            .ThenBy(n => n.LanguageId)
            .FirstOrDefault();

        return Ok(new QrResolveDto
        {
            PoiId = poi.PoiId,
            Name = poi.Name,
            Address = poi.Address,
            Description = poi.Description,
            NarrationTitle = pick?.Title,
            NarrationContent = pick?.Content,
            LanguageCode = pick?.Language?.Code
        });
    }
}
