using SQLite;

namespace VKFoodTour.Mobile.Services.Offline;

/// <summary>POI tóm tắt cho danh sách + bản đồ. JSON là PoiDto đã chuẩn hoá.</summary>
[Table("poi_cache")]
public class PoiCacheRow
{
    [PrimaryKey, Column("Pk")]
    public string Pk { get; set; } = string.Empty; // poiId|lang

    [Indexed]
    public int PoiId { get; set; }

    [Indexed]
    public string LangCode { get; set; } = "vi";

    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Radius { get; set; }
    public string? MembershipTier { get; set; }
    public string? CoverImageUrl { get; set; }

    /// <summary>JSON gốc PoiDto để hồi sinh khi cần.</summary>
    public string DtoJson { get; set; } = string.Empty;

    public long UpdatedAtUnix { get; set; }
}

/// <summary>Detail POI (menu, gallery, audio). JSON = PoiDetailDto.</summary>
[Table("poi_detail_cache")]
public class PoiDetailCacheRow
{
    [PrimaryKey, Column("Pk")]
    public string Pk { get; set; } = string.Empty; // poiId|lang

    [Indexed]
    public int PoiId { get; set; }

    [Indexed]
    public string LangCode { get; set; } = "vi";

    public string DtoJson { get; set; } = string.Empty;
    public long UpdatedAtUnix { get; set; }
}

[Table("language_cache")]
public class LanguageCacheRow
{
    [PrimaryKey]
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? FlagEmoji { get; set; }
    public long UpdatedAtUnix { get; set; }
}

/// <summary>Hàng đợi tracking event khi offline.</summary>
[Table("pending_events")]
public class PendingEventRow
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string DeviceId { get; set; } = string.Empty;
    public int? PoiId { get; set; }
    public string EventType { get; set; } = "move";
    public int? ListenedDurationSec { get; set; }
    public string? LanguageCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public long CreatedAtUnix { get; set; }
    public int Attempts { get; set; }
}

/// <summary>Bảng meta cho timestamp last-sync, schema-version v.v.</summary>
[Table("kv_meta")]
public class KvMetaRow
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
}
