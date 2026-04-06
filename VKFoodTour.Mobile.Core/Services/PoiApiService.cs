using System.Net.Http.Json;
using VKFoodTour.Shared.DTOs;
using VKFoodTour.Mobile.Core.Constants;

namespace VKFoodTour.Mobile.Core.Services
{
    public class PoiApiService
    {
        private readonly HttpClient _httpClient;

        public PoiApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiConstants.BaseApiUrl)
            };
            // Tối ưu: Đặt timeout để App không bị treo nếu không gọi được API
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        // Hàm lấy danh sách POI
        public async Task<List<PoiDto>> GetPoisAsync()
        {
            try
            {
                // Gọi API GET /api/Poi
                var response = await _httpClient.GetAsync("/api/Poi");

                if (response.IsSuccessStatusCode)
                {
                    // Tự động chuyển đổi JSON trả về thành List<PoiDto>
                    var pois = await response.Content.ReadFromJsonAsync<List<PoiDto>>();
                    return pois ?? new List<PoiDto>();
                }

                // (Tùy chọn) Ghi log lỗi nếu StatusCode không phải là 200 OK
                Console.WriteLine($"Lỗi API: {response.StatusCode}");
                return new List<PoiDto>();
            }
            catch (Exception ex)
            {
                // Bắt lỗi mất mạng, sai IP, hoặc API chưa chạy
                Console.WriteLine($"Lỗi kết nối: {ex.Message}");
                return new List<PoiDto>();
            }
        }
    }
}