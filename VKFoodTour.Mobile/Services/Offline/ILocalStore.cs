using VKFoodTour.Mobile.Models;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services.Offline;

public interface ILocalStore
{
    Task InitAsync();

    // POI list
    Task<List<Poi>> GetPoisAsync(string langCode);
    Task UpsertPoisAsync(string langCode, IEnumerable<PoiDto> dtos, Func<PoiDto, Poi> mapper, bool replaceLang = true);
    Task DeletePoiAsync(int poiId);

    // POI detail
    Task<PoiDetailDto?> GetPoiDetailAsync(int poiId, string langCode);
    Task UpsertPoiDetailAsync(int poiId, string langCode, PoiDetailDto dto);

    // Languages
    Task<List<LanguageListItemDto>> GetLanguagesAsync();
    Task UpsertLanguagesAsync(IEnumerable<LanguageListItemDto> langs);

    // Pending tracking events
    Task EnqueueEventAsync(PendingEventRow row);
    Task<List<PendingEventRow>> GetPendingEventsAsync(int max = 100);
    Task DeleteEventAsync(int id);
    Task IncrementAttemptAsync(int id);

    // Meta
    Task<string?> GetMetaAsync(string key);
    Task SetMetaAsync(string key, string value);
}
