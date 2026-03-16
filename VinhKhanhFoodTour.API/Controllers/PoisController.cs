using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFoodTour.API.Models; // Sửa lại đúng Namespace thư mục Models của bạn

namespace VinhKhanhFoodTour.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoisController : ControllerBase
    {
        private readonly VinhkhanhFoodtourContext _context;

        // Constructor: Yêu cầu hệ thống cấp cho DbContext để chọc vào Database
        public PoisController(VinhkhanhFoodtourContext context)
        {
            _context = context;
        }

        // GET: api/pois
        // API này trả về danh sách toàn bộ các quán ăn (POIs)
        [HttpGet]
        public async Task<IActionResult> GetAllPois()
        {
            // Lấy toàn bộ dữ liệu từ bảng POIS
            var pois = await _context.Pois.ToListAsync();

            // Trả về mã 200 (OK) kèm theo dữ liệu JSON
            return Ok(pois);
        }
    }
}