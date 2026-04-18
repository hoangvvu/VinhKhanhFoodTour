namespace VKFoodTour.Mobile.Services;

public interface IAudioPlaybackService
{
    /// <summary>Trả về false nếu URL rỗng, HTTP lỗi hoặc không tạo được player.</summary>
    Task<bool> PlayAsync(string? url, CancellationToken cancellationToken = default);
    void Stop();
    bool IsPlaying { get; }
    /// <summary>Tiến độ phát hiện tại [0.0 – 1.0]. 0 nếu chưa phát hoặc plugin không hỗ trợ.</summary>
    double GetProgress();
}
