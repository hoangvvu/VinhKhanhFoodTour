using Microsoft.AspNetCore.Mvc;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Infrastructure.Entities;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FeedbackController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FeedbackController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>Du khách gửi đánh giá ứng dụng (số sao + góp ý).</summary>
    [HttpPost("app")]
    public async Task<IActionResult> SubmitAppFeedback([FromBody] CreateAppFeedbackDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DeviceId))
            return BadRequest("deviceId required.");

        if (dto.Rating is < 1 or > 5)
            return BadRequest("rating must be 1–5.");

        var entity = new AppFeedback
        {
            DeviceId    = dto.DeviceId.Trim(),
            Rating      = dto.Rating,
            Comment     = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
            AppVersion  = string.IsNullOrWhiteSpace(dto.AppVersion) ? null : dto.AppVersion.Trim(),
            CreatedAt   = DateTime.UtcNow
        };

        _context.AppFeedbacks.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new { entity.FeedbackId, message = "Thank you!" });
    }
}
