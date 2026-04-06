using Admin.Data;
using Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace Admin.Services
{
    public class PoiService
    {
        private readonly ApplicationDbContext _db;

        // ── Constructor: Inject DbContext ────────────────
        // Blazor Server dùng Scoped lifetime, mỗi circuit (session)
        // sẽ có 1 instance riêng của PoiService + DbContext
        public PoiService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ═════════════════════════════════════════════════
        //  1. LẤY DANH SÁCH (READ)
        // ═════════════════════════════════════════════════

        /// <summary>
        /// Lấy tất cả POI, sắp xếp theo mức ưu tiên (1 = cao nhất) rồi theo tên.
        /// </summary>
        public async Task<List<Poi>> GetAllAsync()
        {
            return await _db.Pois
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .AsNoTracking()       // Chỉ đọc, không cần EF track thay đổi → nhanh hơn
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách POI đang hoạt động (is_active = true).
        /// Dùng cho dropdown, bản đồ, v.v.
        /// </summary>
        public async Task<List<Poi>> GetActiveAsync()
        {
            return await _db.Pois
                .Where(p => p.IsActive)
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Lấy 1 POI theo ID. Trả về null nếu không tìm thấy.
        /// </summary>
        public async Task<Poi?> GetByIdAsync(int id)
        {
            return await _db.Pois
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PoiId == id);
        }

        // ═════════════════════════════════════════════════
        //  2. THÊM MỚI (CREATE)
        // ═════════════════════════════════════════════════

        /// <summary>
        /// Thêm POI mới vào database.
        /// CreatedAt sẽ do SQL Server tự gán (GETDATE()).
        /// Trả về POI đã được gán ID.
        /// </summary>
        public async Task<Poi> CreateAsync(Poi poi)
        {
            // Không gán CreatedAt ở đây — để SQL Server xử lý bằng GETDATE()
            // Đảm bảo UpdatedAt là null khi tạo mới
            poi.UpdatedAt = null;

            _db.Pois.Add(poi);
            await _db.SaveChangesAsync();

            // Sau SaveChanges, EF tự động gán PoiId (IDENTITY) vào object
            return poi;
        }

        // ═════════════════════════════════════════════════
        //  3. CẬP NHẬT (UPDATE)
        // ═════════════════════════════════════════════════

        /// <summary>
        /// Cập nhật thông tin POI.
        /// Chỉ update những field được phép thay đổi.
        /// Trả về true nếu thành công, false nếu không tìm thấy POI.
        /// </summary>
        public async Task<bool> UpdateAsync(Poi poi)
        {
            // Tìm entity đang được EF track trong DB
            var existing = await _db.Pois.FindAsync(poi.PoiId);

            if (existing is null)
                return false;

            // Cập nhật từng field — KHÔNG dùng _db.Update(poi)
            // vì cách đó sẽ ghi đè toàn bộ, kể cả CreatedAt
            existing.Name = poi.Name;
            existing.Address = poi.Address;
            existing.Phone = poi.Phone;
            existing.Latitude = poi.Latitude;
            existing.Longitude = poi.Longitude;
            existing.Radius = poi.Radius;
            existing.Priority = poi.Priority;
            existing.OwnerId = poi.OwnerId;
            existing.IsActive = poi.IsActive;
            existing.UpdatedAt = DateTime.Now;  // Đánh dấu thời điểm sửa

            await _db.SaveChangesAsync();
            return true;
        }

        // ═════════════════════════════════════════════════
        //  4. XÓA (DELETE)
        // ═════════════════════════════════════════════════

        /// <summary>
        /// Xóa POI khỏi database.
        /// Lưu ý: Do schema có ON DELETE CASCADE, khi xóa POI thì
        /// Narrations, Foods, Images, QRCodes, Reviews liên quan cũng bị xóa.
        /// Trả về true nếu xóa thành công, false nếu không tìm thấy.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var poi = await _db.Pois.FindAsync(id);

            if (poi is null)
                return false;

            _db.Pois.Remove(poi);
            await _db.SaveChangesAsync();
            return true;
        }

        // ═════════════════════════════════════════════════
        //  5. TIỆN ÍCH (UTILITY)
        // ═════════════════════════════════════════════════

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động của POI.
        /// Tiện cho nút toggle trên giao diện admin.
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var poi = await _db.Pois.FindAsync(id);

            if (poi is null)
                return false;

            poi.IsActive = !poi.IsActive;
            poi.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Đếm tổng số POI (dùng cho dashboard thống kê).
        /// </summary>
        public async Task<int> CountAsync()
        {
            return await _db.Pois.CountAsync();
        }

        /// <summary>
        /// Đếm số POI đang hoạt động.
        /// </summary>
        public async Task<int> CountActiveAsync()
        {
            return await _db.Pois.CountAsync(p => p.IsActive);
        }

        /// <summary>
        /// Kiểm tra tên POI đã tồn tại chưa (tránh trùng lặp).
        /// excludeId: bỏ qua POI đang được chỉnh sửa.
        /// </summary>
        public async Task<bool> IsNameExistsAsync(string name, int excludeId = 0)
        {
            return await _db.Pois
                .AnyAsync(p => p.Name == name && p.PoiId != excludeId);
        }
    }
}