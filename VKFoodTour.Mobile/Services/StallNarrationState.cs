using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

public class StallNarrationState : IStallNarrationState
{
    private readonly object _lock = new();
    private QrResolveDto? _pending;

    public void SetFromQr(QrResolveDto dto)
    {
        lock (_lock)
        {
            _pending = dto;
        }
    }

    public QrResolveDto? Peek()
    {
        lock (_lock)
        {
            return _pending;
        }
    }

    public QrResolveDto? Consume()
    {
        lock (_lock)
        {
            var x = _pending;
            _pending = null;
            return x;
        }
    }

    public string? PendingAudioUrl { get; set; }
}
