using VKFoodTour.Mobile.Models;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

public interface IDataService
{
    string DeviceId { get; }

    Task<List<Poi>> GetPoisAsync(CancellationToken cancellationToken = default);
    Task<Poi?> GetPoiByIdAsync(int poiId, CancellationToken cancellationToken = default);
    Task<PoiDetailDto?> GetPoiDetailAsync(int poiId, CancellationToken cancellationToken = default);
    Task<List<ReviewListItemDto>> GetRecentReviewsAsync(int take = 30, CancellationToken cancellationToken = default);
    Task<List<ReviewListItemDto>> GetPoiReviewsAsync(int poiId, CancellationToken cancellationToken = default);
    Task<ReviewListItemDto?> PostReviewAsync(CreateReviewDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AuthResponseDto?> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default);
    Task TrackEventAsync(int? poiId, string eventType, int? listenedDurationSec = null, string? languageCode = null, CancellationToken cancellationToken = default);

    /// <summary>Chuỗi quét được (vd vkfoodtour://VK-XXX hoặc chỉ token).</summary>
    Task<QrResolveDto?> ResolveQrAsync(string scannedPayload, CancellationToken cancellationToken = default);

    /// <summary>Ngôn ngữ đang bật trên máy chủ (picker giao diện).</summary>
    Task<List<LanguageListItemDto>> GetLanguagesAsync(CancellationToken cancellationToken = default);
}