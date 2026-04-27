namespace VKFoodTour.Mobile.Services.Offline;

public interface IMediaCacheService
{
    /// <summary>Trả về đường dẫn file local nếu URL đã có trong cache, ngược lại null.</summary>
    string? TryGetLocalPath(string url);

    /// <summary>Đảm bảo URL đã được cache; trả về đường dẫn local. Null nếu offline + chưa cache.</summary>
    Task<string?> EnsureCachedAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>Lưu một byte[] đã tải về vào cache, trả về path.</summary>
    Task<string> SaveBytesAsync(string url, byte[] data, CancellationToken cancellationToken = default);
}
