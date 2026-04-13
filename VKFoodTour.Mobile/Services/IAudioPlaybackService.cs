namespace VKFoodTour.Mobile.Services;

public interface IAudioPlaybackService
{
    Task PlayAsync(string? url, CancellationToken cancellationToken = default);
    void Stop();
    bool IsPlaying { get; }
}
