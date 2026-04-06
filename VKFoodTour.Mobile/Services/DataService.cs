using System.Net.Http.Json;
using VKFoodTour.Mobile.Models;

namespace VKFoodTour.Mobile.Services;

public class DataService : IDataService
{
    private readonly HttpClient _http;
    // Lưu ý: Thay số 5242 bằng cổng HTTP của bạn trong launchSettings.json của Backend
    private const string ApiBase = "http://10.0.2.2:5242/api";

    public DataService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task<List<Poi>> GetPoisAsync()
    {
        try
        {
            var response = await _http.GetFromJsonAsync<List<Poi>>($"{ApiBase}/Poi");
            return response ?? new List<Poi>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi kết nối API: {ex.Message}");
            // Trả về dữ liệu mẫu nếu API lỗi để bạn vẫn thấy giao diện
            return new List<Poi>
            {
                new Poi { PoiId = 1, Name = "Ốc Oanh (Demo)", Address = "534 Vĩnh Khánh" },
                new Poi { PoiId = 2, Name = "Ốc Vũ (Demo)", Address = "37 Vĩnh Khánh" }
            };
        }
    }

    public async Task<Poi?> GetPoiByIdAsync(int poiId)
    {
        try
        {
            return await _http.GetFromJsonAsync<Poi>($"{ApiBase}/Poi/{poiId}");
        }
        catch { return null; }
    }
}