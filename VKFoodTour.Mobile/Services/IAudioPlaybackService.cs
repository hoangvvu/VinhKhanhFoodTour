namespace VKFoodTour.Mobile.Services;

public interface IAudioPlaybackService
{
    /// <summary>Trả về false nếu URL rỗng, HTTP lỗi hoặc không tạo được player.</summary>
    Task<bool> PlayAsync(string? url, CancellationToken cancellationToken = default);
    void Stop();
    bool IsPlaying { get; }
}
