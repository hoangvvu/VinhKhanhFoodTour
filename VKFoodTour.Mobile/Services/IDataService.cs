using VKFoodTour.Mobile.Models;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

public interface IDataService
{
    Task<List<Poi>> GetPoisAsync(CancellationToken cancellationToken = default);
    Task<Poi?> GetPoiByIdAsync(int poiId, CancellationToken cancellationToken = default);
    Task<PoiDetailDto?> GetPoiDetailAsync(int poiId, CancellationToken cancellationToken = default);
    Task<AuthResponseDto?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AuthResponseDto?> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Chuỗi quét được (vd vkfoodtour://VK-XXX hoặc chỉ token).</summary>
    Task<QrResolveDto?> ResolveQrAsync(string scannedPayload, CancellationToken cancellationToken = default);
}