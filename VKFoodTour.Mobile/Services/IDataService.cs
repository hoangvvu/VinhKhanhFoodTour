using VKFoodTour.Mobile.Models;

namespace VKFoodTour.Mobile.Services;

public interface IDataService
{
    Task<List<Poi>> GetPoisAsync();
    Task<Poi?> GetPoiByIdAsync(int poiId);
    // Bạn có thể thêm các hàm Sync, GetNarration... sau này
}