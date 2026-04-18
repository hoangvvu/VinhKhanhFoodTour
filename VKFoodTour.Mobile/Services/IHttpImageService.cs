namespace VKFoodTour.Mobile.Services;

public interface IHttpImageService
{
    /// <summary>Lấy ảnh từ URL, dùng cache in-memory với TTL ngắn.</summary>
    Task<ImageSource?> GetImageSourceAsync(string? url, CancellationToken cancellationToken = default);

    /// <summary>Lấy ảnh, có thể buộc tải lại bỏ qua cache.</summary>
    Task<ImageSource?> GetImageSourceAsync(string? url, bool forceReload, CancellationToken cancellationToken = default);

    /// <summary>Xóa cache cho một URL cụ thể.</summary>
    void Invalidate(string? url);

    /// <summary>Xóa toàn bộ cache ảnh.</summary>
    void ClearAll();
}
