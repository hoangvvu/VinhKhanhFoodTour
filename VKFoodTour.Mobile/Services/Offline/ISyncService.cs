namespace VKFoodTour.Mobile.Services.Offline;

public interface ISyncService
{
    /// <summary>Đồng bộ POI/Languages về SQLite. Trả true nếu thành công.</summary>
    Task<bool> SyncAsync(CancellationToken cancellationToken = default);

    /// <summary>Lần sync gần nhất (UTC) hoặc null nếu chưa từng sync.</summary>
    Task<DateTime?> GetLastSyncAtAsync();
}
