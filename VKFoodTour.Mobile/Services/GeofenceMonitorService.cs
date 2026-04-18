using Microsoft.Maui.Devices.Sensors;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Services;

/// <summary>
/// Chạy nền, theo dõi GPS mỗi 3 giây và fire sự kiện khi du khách vào/rời geofence của POI.
/// Đặc điểm:
///   - Dwell threshold 8s: phải ở trong zone 8s liên tục mới fire PoiEntered
///   - Exit debounce 10s: phải ở ngoài zone 10s liên tục mới fire PoiExited
///   - Không re-trigger POI đã trong played set
/// </summary>
public class GeofenceMonitorService : IAsyncDisposable
{
    // ── Tunable constants ─────────────────────────────────────────
    private const int    PollIntervalMs      = 3_000;   // 3s poll
    private const double DwellThresholdSec   = 8;       // vào zone 8s mới trigger
    private const double ExitDebounceMs      = 10_000;  // 10s ngoài zone mới confirm exit
    private const double GpsBufferMeters     = 10;      // nới thêm 10m ngoài radius
    private const int    GpsTimeoutSec       = 6;

    // ── State ─────────────────────────────────────────────────────
    private List<AudioQueueItemDto> _monitoredPois = new();
    private readonly Dictionary<int, DateTime> _pendingEnter  = new(); // poi đang "trong zone nhưng chưa đủ dwell"
    private readonly Dictionary<int, DateTime> _pendingExit   = new(); // poi "ngoài zone nhưng chưa confirm exit"
    private readonly HashSet<int>              _confirmedIn   = new(); // poi đã confirm enter (đang trong zone)
    private readonly HashSet<int>              _playedPois    = new(); // poi đã được phát xong

    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private DateTime _lastHeartbeat = DateTime.MinValue;
    private const double HeartbeatIntervalSec = 30;

    private readonly IDataService _dataService;

    // ── Events ────────────────────────────────────────────────────
    public event EventHandler<int>? PoiEntered;   // fire khi dwell >= threshold
    public event EventHandler<int>? PoiExited;    // fire khi exit confirmed

    // ── Public API ────────────────────────────────────────────────

    public GeofenceMonitorService(IDataService dataService)
    {
        _dataService = dataService;
    }

    public void StartMonitoring(List<AudioQueueItemDto> pois)
    {
        _monitoredPois = pois ?? new List<AudioQueueItemDto>();
        _pendingEnter.Clear();
        _pendingExit.Clear();
        _confirmedIn.Clear();
        // _playedPois không clear — giữ lịch sử suốt tour

        if (_isRunning) return;
        _isRunning = true;
        _cts = new CancellationTokenSource();
        _ = RunLoopAsync(_cts.Token);
    }

    public void StopMonitoring()
    {
        _cts?.Cancel();
        _isRunning = false;
    }

    /// <summary>Đánh dấu POI đã được phát xong để không trigger lại.</summary>
    public void MarkPoiPlayed(int poiId) => _playedPois.Add(poiId);

    /// <summary>Cập nhật danh sách POI cần theo dõi (khi queue thay đổi).</summary>
    public void UpdateMonitoredPois(List<AudioQueueItemDto> pois)
    {
        _monitoredPois = pois ?? new List<AudioQueueItemDto>();
    }

    /// <summary>Reset toàn bộ state (dùng khi bắt đầu tour mới).</summary>
    public void ResetAll()
    {
        _playedPois.Clear();
        _pendingEnter.Clear();
        _pendingExit.Clear();
        _confirmedIn.Clear();
    }

    // ── Internal polling loop ─────────────────────────────────────

    private async Task RunLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var location = await TryGetLocationAsync(ct);
                if (location is not null)
                    ProcessLocation(location.Latitude, location.Longitude);

                await Task.Delay(PollIntervalMs, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GeofenceMonitor] loop error: {ex.Message}");
        }
        finally
        {
            _isRunning = false;
        }
    }

    private async void ProcessLocation(double userLat, double userLon)
    {
        var now = DateTime.UtcNow;

        if ((now - _lastHeartbeat).TotalSeconds >= HeartbeatIntervalSec)
        {
            _lastHeartbeat = now;
            await _dataService.TrackEventAsync(poiId: null, eventType: "move", latitude: userLat, longitude: userLon);
        }

        foreach (var poi in _monitoredPois)
        {
            if (_playedPois.Contains(poi.PoiId)) continue;

            var threshold = Math.Clamp(poi.PoiRadiusMeters, 5, 200) + GpsBufferMeters;
            var dist = CalculateDistanceMeters(userLat, userLon, poi.Latitude, poi.Longitude);
            var insideZone = dist <= threshold;

            if (insideZone)
            {
                // Nếu đang trong pending exit → reset exit timer
                _pendingExit.Remove(poi.PoiId);

                if (_confirmedIn.Contains(poi.PoiId)) continue; // đã confirmed, bỏ qua

                if (!_pendingEnter.TryGetValue(poi.PoiId, out var enterTime))
                {
                    _pendingEnter[poi.PoiId] = now; // bắt đầu đếm dwell
                }
                else if ((now - enterTime).TotalSeconds >= DwellThresholdSec)
                {
                    // Đủ dwell → confirm enter
                    _confirmedIn.Add(poi.PoiId);
                    _pendingEnter.Remove(poi.PoiId);
                    FirePoiEntered(poi.PoiId);
                }
            }
            else
            {
                // Ngoài zone → reset pending enter
                _pendingEnter.Remove(poi.PoiId);

                if (!_confirmedIn.Contains(poi.PoiId)) continue; // chưa confirm enter, bỏ qua

                if (!_pendingExit.TryGetValue(poi.PoiId, out var exitStart))
                {
                    _pendingExit[poi.PoiId] = now; // bắt đầu đếm exit debounce
                }
                else if ((now - exitStart).TotalMilliseconds >= ExitDebounceMs)
                {
                    // Confirm exit
                    _confirmedIn.Remove(poi.PoiId);
                    _pendingExit.Remove(poi.PoiId);
                    FirePoiExited(poi.PoiId);
                }
            }
        }
    }

    private void FirePoiEntered(int poiId)
    {
        System.Diagnostics.Debug.WriteLine($"[GeofenceMonitor] PoiEntered: {poiId}");
        MainThread.BeginInvokeOnMainThread(() => PoiEntered?.Invoke(this, poiId));
    }

    private void FirePoiExited(int poiId)
    {
        System.Diagnostics.Debug.WriteLine($"[GeofenceMonitor] PoiExited: {poiId}");
        MainThread.BeginInvokeOnMainThread(() => PoiExited?.Invoke(this, poiId));
    }

    private static async Task<Location?> TryGetLocationAsync(CancellationToken ct)
    {
        try
        {
            var last = await Geolocation.GetLastKnownLocationAsync();
            if (last is not null) return last;

            return await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(GpsTimeoutSec)),
                ct);
        }
        catch { return null; }
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6_371_000;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180d;

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
