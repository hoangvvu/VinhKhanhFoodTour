using System.Globalization;
using System.Net.Http.Json;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services.Offline;

public class SyncService : ISyncService
{
    private const string MetaLastSync = "sync.last_at_utc";
    private readonly HttpClient _http;
    private readonly ISettingsService _settings;
    private readonly ILocalStore _store;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SyncService(HttpClient http, ISettingsService settings, ILocalStore store)
    {
        _http = http;
        _settings = settings;
        _store = store;
    }

    public async Task<DateTime?> GetLastSyncAtAsync()
    {
        var raw = await _store.GetMetaAsync(MetaLastSync);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return DateTime.TryParse(raw, null, DateTimeStyles.RoundtripKind, out var dt) ? dt : null;
    }

    public async Task<bool> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await _gate.WaitAsync(0, cancellationToken)) return false;
        try
        {
            await _store.InitAsync();
            var apiRoot = _settings.ApiBaseUrl.Trim().TrimEnd('/');
            var lang = _settings.SelectedLanguageCode;
            var since = await GetLastSyncAtAsync();
            var sinceQuery = since.HasValue
                ? $"&since={Uri.EscapeDataString(since.Value.ToString("o", CultureInfo.InvariantCulture))}"
                : string.Empty;

            var url = $"{apiRoot}/api/Sync/bootstrap?lang={Uri.EscapeDataString(lang)}{sinceQuery}";
            var resp = await _http.GetAsync(url, cancellationToken);
            if (!resp.IsSuccessStatusCode) return false;

            var dto = await resp.Content.ReadFromJsonAsync<SyncBootstrapDto>(cancellationToken: cancellationToken);
            if (dto is null) return false;

            if (dto.Languages.Count > 0)
                await _store.UpsertLanguagesAsync(dto.Languages);

            if (dto.Pois.Count > 0)
            {
                if (since.HasValue)
                {
                    foreach (var p in dto.Pois)
                    {
                        var single = new[] { p };
                        await _store.UpsertPoisAsync(lang, single, MapToMobile, replaceLang: false);
                    }
                }
                else
                {
                    await _store.UpsertPoisAsync(lang, dto.Pois, MapToMobile);
                }
            }

            foreach (var removedId in dto.RemovedPoiIds)
                await _store.DeletePoiAsync(removedId);

            await _store.SetMetaAsync(MetaLastSync, dto.ServerNowUtc.ToString("o", CultureInfo.InvariantCulture));
            System.Diagnostics.Debug.WriteLine($"[Sync] OK lang={lang} pois={dto.Pois.Count} removed={dto.RemovedPoiIds.Count} since={since:o}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] failed: {ex.Message}");
            return false;
        }
        finally { _gate.Release(); }
    }

    private static Models.Poi MapToMobile(PoiDto d) => new()
    {
        PoiId = d.PoiId,
        Name = d.Name,
        Address = d.Address ?? string.Empty,
        Latitude = (double)d.Latitude,
        Longitude = (double)d.Longitude,
        Radius = d.Radius,
        MembershipTier = d.MembershipTier ?? "Standard",
        CoverImageUrl = d.ImageUrl
    };
}
