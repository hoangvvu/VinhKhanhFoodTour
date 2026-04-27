using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SyncController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SyncController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Trả về snapshot dữ liệu cho mobile để đồng bộ offline.
    /// - since=null/empty → bootstrap toàn bộ.
    /// - since=ISO8601 UTC → chỉ POI có Poi.UpdatedAt &gt; since hoặc có narration mới hơn since.
    /// </summary>
    [HttpGet("bootstrap")]
    public async Task<ActionResult<SyncBootstrapDto>> Bootstrap(
        [FromQuery] string? since = null,
        [FromQuery] string? lang = null)
    {
        var serverNow = DateTime.UtcNow;
        var targetCode = NormalizeLanguageCode(lang);
        DateTime? sinceUtc = null;
        if (!string.IsNullOrWhiteSpace(since)
            && DateTime.TryParse(since, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
        {
            sinceUtc = parsed.ToUniversalTime();
        }

        var poiQuery = _context.Pois.AsNoTracking().Where(p => p.IsActive);
        if (sinceUtc.HasValue)
        {
            var sinceLocal = sinceUtc.Value;
            var changedPoiIds = await _context.Narrations.AsNoTracking()
                .Where(n => n.UpdatedAt != null && n.UpdatedAt > sinceLocal)
                .Select(n => n.PoiId)
                .Distinct()
                .ToListAsync();
            poiQuery = poiQuery.Where(p =>
                (p.UpdatedAt != null && p.UpdatedAt > sinceLocal)
                || changedPoiIds.Contains(p.PoiId));
        }

        var pois = await poiQuery
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
                MembershipTier = p.OwnerId.HasValue
                    ? _context.Users.Where(u => u.UserId == p.OwnerId.Value).Select(u => u.MembershipTier ?? "Standard").FirstOrDefault() ?? "Standard"
                    : "Standard",
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

        var languages = await _context.Languages.AsNoTracking()
            .Where(l => l.IsActive)
            .Select(l => new LanguageListItemDto
            {
                Code = l.Code,
                Name = l.Name
            })
            .ToListAsync();

        var removed = new List<int>();
        if (sinceUtc.HasValue)
        {
            removed = await _context.Pois.AsNoTracking()
                .Where(p => !p.IsActive && p.UpdatedAt != null && p.UpdatedAt > sinceUtc.Value)
                .Select(p => p.PoiId)
                .ToListAsync();
        }

        return Ok(new SyncBootstrapDto
        {
            ServerNowUtc = serverNow,
            Pois = pois,
            Languages = languages,
            RemovedPoiIds = removed
        });
    }

    private static string NormalizeLanguageCode(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return "vi";
        var code = lang.Trim().ToLowerInvariant();
        var dash = code.IndexOf('-');
        return dash > 0 ? code[..dash] : code;
    }
}
