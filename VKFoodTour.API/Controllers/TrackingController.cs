using Microsoft.AspNetCore.Mvc;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Infrastructure.Entities;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TrackingController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TrackingController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("log")]
    public async Task<IActionResult> Log([FromBody] TrackingLogRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DeviceId) || string.IsNullOrWhiteSpace(dto.EventType))
            return BadRequest("Missing deviceId/eventType.");

        var log = new TrackingLog
        {
            DeviceId = dto.DeviceId.Trim(),
            PoiId = dto.PoiId,
            Latitude = (decimal)(dto.Latitude ?? 0),
            Longitude = (decimal)(dto.Longitude ?? 0),
            EventType = dto.EventType.Trim().ToLowerInvariant(),
            ListenedDurationSec = dto.ListenedDurationSec,
            LanguageCode = dto.LanguageCode
        };

        _context.TrackingLogs.Add(log);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
