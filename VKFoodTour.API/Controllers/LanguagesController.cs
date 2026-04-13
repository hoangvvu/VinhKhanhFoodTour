using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LanguagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LanguagesController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>Ngôn ngữ đang bật — dùng cho app mobile chọn giao diện.</summary>
    [HttpGet]
    public async Task<ActionResult<List<LanguageListItemDto>>> GetActive()
    {
        var list = await _context.Languages
            .AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.Code)
            .Select(l => new LanguageListItemDto { Code = l.Code, Name = l.Name })
            .ToListAsync();

        return Ok(list);
    }
}
