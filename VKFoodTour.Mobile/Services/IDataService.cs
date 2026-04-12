using VKFoodTour.Mobile.Models;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

public interface IDataService
{
    Task<List<Poi>> GetPoisAsync(CancellationToken cancellationToken = default);
    Task<Poi?> GetPoiByIdAsync(int poiId, CancellationToken cancellationToken = default);

    /// <summary>Chuỗi quét được (vd vkfoodtour://VK-XXX hoặc chỉ token).</summary>
    Task<QrResolveDto?> ResolveQrAsync(string scannedPayload, CancellationToken cancellationToken = default);
}