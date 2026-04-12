using System.Net.Http.Json;
using VKFoodTour.Mobile.Models;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

public class DataService : IDataService
{
    private readonly HttpClient _http;
    private readonly ISettingsService _settings;

    public DataService(HttpClient http, ISettingsService settings)
    {
        _http = http;
        _settings = settings;
    }

    private string ApiRoot => _settings.ApiBaseUrl.Trim().TrimEnd('/');

    public async Task<List<Poi>> GetPoisAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetAsync($"{ApiRoot}/api/Poi", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return FallbackDemo();

            var dtos = await response.Content.ReadFromJsonAsync<List<PoiDto>>(cancellationToken: cancellationToken);
            if (dtos is null)
                return new List<Poi>();

            return dtos.Select(MapToMobile).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API GetPois: {ex.Message}");
            return FallbackDemo();
        }
    }

    public async Task<Poi?> GetPoiByIdAsync(int poiId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetAsync($"{ApiRoot}/api/Poi/{poiId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var dto = await response.Content.ReadFromJsonAsync<PoiDto>(cancellationToken: cancellationToken);
            return dto is null ? null : MapToMobile(dto);
        }
        catch
        {
            return null;
        }
    }

    public async Task<QrResolveDto?> ResolveQrAsync(string scannedPayload, CancellationToken cancellationToken = default)
    {
        var token = ExtractQrToken(scannedPayload);
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var encoded = Uri.EscapeDataString(token);
            var response = await _http.GetAsync($"{ApiRoot}/api/Qr/resolve/{encoded}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<QrResolveDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API ResolveQr: {ex.Message}");
            return null;
        }
    }

    private static string ExtractQrToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var t = raw.Trim();
        if (t.StartsWith("vkfoodtour://", StringComparison.OrdinalIgnoreCase))
            return t["vkfoodtour://".Length..].Trim();

        if (t.StartsWith("vkfoodtour:", StringComparison.OrdinalIgnoreCase))
        {
            var idx = t.IndexOf("//", StringComparison.Ordinal);
            if (idx >= 0)
                return t[(idx + 2)..].Trim();
        }

        return t;
    }

    private static Poi MapToMobile(PoiDto d) =>
        new()
        {
            PoiId = d.PoiId,
            Name = d.Name,
            Address = d.Address ?? string.Empty,
            Latitude = (double)d.Latitude,
            Longitude = (double)d.Longitude,
            Radius = d.Radius,
            Priority = d.Priority,
            CoverEmoji = StallEmoji(d.Name)
        };

    private static string StallEmoji(string name)
    {
        var h = name.Aggregate(0, (a, c) => a + c);
        var emojis = new[] { "🍜", "🦪", "🍢", "🥟", "🍲", "🧋", "🍡", "🥘" };
        return emojis[Math.Abs(h) % emojis.Length];
    }

    private static List<Poi> FallbackDemo() =>
        new()
        {
            new Poi { PoiId = 1, Name = "Ốc Oanh (Demo — không kết nối API)", Address = "534 Vĩnh Khánh", Latitude = 10.758, Longitude = 106.705 },
            new Poi { PoiId = 2, Name = "Ốc Vũ (Demo — không kết nối API)", Address = "37 Vĩnh Khánh", Latitude = 10.759, Longitude = 106.706 }
        };
}
