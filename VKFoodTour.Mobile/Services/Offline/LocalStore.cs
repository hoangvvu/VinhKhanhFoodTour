using System.Text.Json;
using SQLite;
using VKFoodTour.Mobile.Models;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services.Offline;

public class LocalStore : ILocalStore
{
    private const string DbFileName = "vkfoodtour.offline.db3";
    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.Create |
        SQLiteOpenFlags.SharedCache |
        SQLiteOpenFlags.FullMutex;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _db;

    private string DbPath => Path.Combine(FileSystem.AppDataDirectory, DbFileName);

    public async Task InitAsync()
    {
        if (_db is not null) return;
        await _initLock.WaitAsync();
        try
        {
            if (_db is not null) return;
            var conn = new SQLiteAsyncConnection(DbPath, Flags);
            await conn.CreateTableAsync<PoiCacheRow>();
            await conn.CreateTableAsync<PoiDetailCacheRow>();
            await conn.CreateTableAsync<LanguageCacheRow>();
            await conn.CreateTableAsync<PendingEventRow>();
            await conn.CreateTableAsync<KvMetaRow>();
            _db = conn;
        }
        finally { _initLock.Release(); }
    }

    private async Task<SQLiteAsyncConnection> ConnAsync()
    {
        if (_db is null) await InitAsync();
        return _db!;
    }

    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public async Task<List<Poi>> GetPoisAsync(string langCode)
    {
        var db = await ConnAsync();
        var rows = await db.Table<PoiCacheRow>()
            .Where(r => r.LangCode == langCode)
            .ToListAsync();
        return rows.Select(r => new Poi
        {
            PoiId = r.PoiId,
            Name = r.Name,
            Address = r.Address ?? string.Empty,
            Latitude = r.Latitude,
            Longitude = r.Longitude,
            Radius = r.Radius,
            MembershipTier = r.MembershipTier ?? "Standard",
            CoverImageUrl = r.CoverImageUrl
        }).ToList();
    }

    public async Task UpsertPoisAsync(string langCode, IEnumerable<PoiDto> dtos, Func<PoiDto, Poi> mapper, bool replaceLang = true)
    {
        var db = await ConnAsync();
        var now = Now();
        var rows = dtos.Select(d =>
        {
            var m = mapper(d);
            return new PoiCacheRow
            {
                Pk = $"{d.PoiId}|{langCode}",
                PoiId = d.PoiId,
                LangCode = langCode,
                Name = m.Name,
                Address = m.Address,
                Latitude = m.Latitude,
                Longitude = m.Longitude,
                Radius = m.Radius,
                MembershipTier = m.MembershipTier,
                CoverImageUrl = m.CoverImageUrl,
                DtoJson = JsonSerializer.Serialize(d, JsonOpts),
                UpdatedAtUnix = now
            };
        }).ToList();

        await db.RunInTransactionAsync(c =>
        {
            if (replaceLang)
                c.Execute("DELETE FROM poi_cache WHERE LangCode = ?", langCode);
            foreach (var r in rows) c.InsertOrReplace(r);
        });
    }

    public async Task DeletePoiAsync(int poiId)
    {
        var db = await ConnAsync();
        await db.ExecuteAsync("DELETE FROM poi_cache WHERE PoiId = ?", poiId);
        await db.ExecuteAsync("DELETE FROM poi_detail_cache WHERE PoiId = ?", poiId);
    }

    public async Task<PoiDetailDto?> GetPoiDetailAsync(int poiId, string langCode)
    {
        var db = await ConnAsync();
        var pk = $"{poiId}|{langCode}";
        var row = await db.FindAsync<PoiDetailCacheRow>(pk);
        if (row is null || string.IsNullOrEmpty(row.DtoJson)) return null;
        try { return JsonSerializer.Deserialize<PoiDetailDto>(row.DtoJson, JsonOpts); }
        catch { return null; }
    }

    public async Task UpsertPoiDetailAsync(int poiId, string langCode, PoiDetailDto dto)
    {
        var db = await ConnAsync();
        await db.InsertOrReplaceAsync(new PoiDetailCacheRow
        {
            Pk = $"{poiId}|{langCode}",
            PoiId = poiId,
            LangCode = langCode,
            DtoJson = JsonSerializer.Serialize(dto, JsonOpts),
            UpdatedAtUnix = Now()
        });
    }

    public async Task<List<LanguageListItemDto>> GetLanguagesAsync()
    {
        var db = await ConnAsync();
        var rows = await db.Table<LanguageCacheRow>().ToListAsync();
        return rows.Select(r => new LanguageListItemDto
        {
            Code = r.Code,
            Name = r.Name
        }).ToList();
    }

    public async Task UpsertLanguagesAsync(IEnumerable<LanguageListItemDto> langs)
    {
        var db = await ConnAsync();
        var now = Now();
        var rows = langs.Select(l => new LanguageCacheRow
        {
            Code = l.Code,
            Name = l.Name,
            UpdatedAtUnix = now
        }).ToList();

        await db.RunInTransactionAsync(c =>
        {
            c.Execute("DELETE FROM language_cache");
            foreach (var r in rows) c.InsertOrReplace(r);
        });
    }

    public async Task EnqueueEventAsync(PendingEventRow row)
    {
        var db = await ConnAsync();
        row.CreatedAtUnix = Now();
        await db.InsertAsync(row);
    }

    public async Task<List<PendingEventRow>> GetPendingEventsAsync(int max = 100)
    {
        var db = await ConnAsync();
        return await db.Table<PendingEventRow>()
            .OrderBy(r => r.Id)
            .Take(max)
            .ToListAsync();
    }

    public async Task DeleteEventAsync(int id)
    {
        var db = await ConnAsync();
        await db.DeleteAsync<PendingEventRow>(id);
    }

    public async Task IncrementAttemptAsync(int id)
    {
        var db = await ConnAsync();
        await db.ExecuteAsync("UPDATE pending_events SET Attempts = Attempts + 1 WHERE Id = ?", id);
    }

    public async Task<string?> GetMetaAsync(string key)
    {
        var db = await ConnAsync();
        var row = await db.FindAsync<KvMetaRow>(key);
        return row?.Value;
    }

    public async Task SetMetaAsync(string key, string value)
    {
        var db = await ConnAsync();
        await db.InsertOrReplaceAsync(new KvMetaRow { Key = key, Value = value });
    }
}
