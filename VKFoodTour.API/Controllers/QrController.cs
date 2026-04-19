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
    public async Task<ActionResult<QrResolveDto>> Resolve(string token, [FromQuery] string? lang = null)
    {
        var normalized = NormalizeToken(token);

        if (string.IsNullOrEmpty(normalized))
            return BadRequest();

        var qr = await _context.QrCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.QrToken == normalized && q.IsActive);

        if (qr is null)
            return NotFound();

        var poi = await _context.Pois
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PoiId == qr.PoiId && p.IsActive && p.OwnerId != null);

        if (poi is null)
            return NotFound();

        var narrations = await _context.Narrations
            .AsNoTracking()
            .Include(n => n.Language)
            .Where(n => n.PoiId == poi.PoiId && n.IsActive)
            .ToListAsync();

        // Ưu tiên: đúng ngôn ngữ yêu cầu -> có AudioUrlQr -> có AudioUrl hoặc AudioUrlAuto -> vi -> bất kỳ
        var pick = narrations
            .OrderByDescending(n => !string.IsNullOrEmpty(lang) 
                && string.Equals(n.Language?.Code, lang, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(n => !string.IsNullOrWhiteSpace(n.AudioUrlQr))
            .ThenByDescending(n => !string.IsNullOrWhiteSpace(n.AudioUrl ?? n.AudioUrlAuto))
            .ThenByDescending(n => string.Equals(n.Language?.Code, "vi", StringComparison.OrdinalIgnoreCase))
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
            AudioUrl = pick?.AudioUrlQr ?? pick?.AudioUrl ?? pick?.AudioUrlAuto,
            LanguageCode = pick?.Language?.Code
        });
    }

    private static string NormalizeToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var normalized = Uri.UnescapeDataString(raw).Trim();

        // Allow users to pass a QR image URL from qrserver.com (contains `data=...`)
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && !string.IsNullOrWhiteSpace(uri.Query))
        {
            var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in query)
            {
                var pieces = part.Split('=', 2);
                if (pieces.Length == 2 && pieces[0].Equals("data", StringComparison.OrdinalIgnoreCase))
                {
                    normalized = Uri.UnescapeDataString(pieces[1]).Trim();
                    break;
                }
            }
        }

        if (normalized.StartsWith("vkfoodtour://", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["vkfoodtour://".Length..].Trim();
        else if (normalized.StartsWith("vkfoodtour:", StringComparison.OrdinalIgnoreCase))
        {
            var idx = normalized.IndexOf("//", StringComparison.Ordinal);
            if (idx >= 0)
                normalized = normalized[(idx + 2)..].Trim();
        }

        return normalized;
    }
}
