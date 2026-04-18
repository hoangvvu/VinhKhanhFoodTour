using System.ComponentModel;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VKFoodTour.Mobile.Models;
using VKFoodTour.Mobile.Services;
using VKFoodTour.Mobile.ViewModels;

namespace VKFoodTour.Mobile.Views;

public partial class FullMapPage : ContentPage
{
    private static readonly Location DefaultCenter = new(10.758, 106.705);
    private HomeViewModel? _boundVm;
    private readonly ILocalizationService _localization;

    public FullMapPage(HomeViewModel vm, ILocalizationService localization)
    {
        InitializeComponent();
        BindingContext = vm;
        _localization = localization;
        _localization.LanguageChanged += OnLocalizationLanguageChanged;
    }

    private void OnLocalizationLanguageChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(RefreshMapFromPois);

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is HomeViewModel vm)
        {
            _boundVm = vm;
            vm.PropertyChanged += OnHomeViewModelPropertyChanged;
            await vm.LoadDataCommand.ExecuteAsync(null);
            RefreshMapFromPois();
        }
        else
        {
            bigMap.MoveToRegion(MapSpan.FromCenterAndRadius(DefaultCenter, Distance.FromMeters(500)));
        }
    }

    protected override void OnDisappearing()
    {
        if (_boundVm is not null)
        {
            _boundVm.PropertyChanged -= OnHomeViewModelPropertyChanged;
            _boundVm = null;
        }

        base.OnDisappearing();
    }

    private void OnHomeViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(HomeViewModel.Pois))
            return;

        MainThread.BeginInvokeOnMainThread(RefreshMapFromPois);
    }

    private void RefreshMapFromPois()
    {
        ApplyPinsAndCircles();
        FitMapToPois();
    }

    private void OnCenterOnStreet(object? sender, EventArgs e)
    {
        FitMapToPois();
        if (BindingContext is HomeViewModel vm && vm.Pois.Count == 0)
            bigMap.MoveToRegion(MapSpan.FromCenterAndRadius(DefaultCenter, Distance.FromMeters(500)));
    }

    private void ApplyPinsAndCircles()
    {
        bigMap.Pins.Clear();
        bigMap.MapElements.Clear();

        if (BindingContext is not HomeViewModel vm)
            return;

        foreach (var p in vm.Pois)
        {
            if (Math.Abs(p.Latitude) < double.Epsilon && Math.Abs(p.Longitude) < double.Epsilon)
                continue;

            var loc = new Location(p.Latitude, p.Longitude);

            var priorityPrefix = _localization.GetString("StallList_Priority");
            var pin = new Pin
            {
                Label = p.Name,
                Address = string.IsNullOrWhiteSpace(p.Address)
                    ? $"{priorityPrefix} {p.Priority}"
                    : p.Address,
                Location = loc,
                Type = PinType.Place,
                BindingContext = p
            };
            pin.MarkerClicked += OnPinClicked;
            bigMap.Pins.Add(pin);

            // Vùng Geofence — cam đậm, khớp với GeofenceMonitorService (radius + 10m buffer)
            var baseRadius = Math.Clamp(p.Radius > 0 ? p.Radius : 20, 5, 200);
            var geofenceRadius = baseRadius + 10;
            bigMap.MapElements.Add(new Circle
            {
                Center = loc,
                Radius = Distance.FromMeters(geofenceRadius),
                StrokeColor = Color.FromRgba(255, 120, 0, 0.85f),   // cam đậm
                FillColor   = Color.FromRgba(255, 140, 0, 0.22f),   // cam nhạt fill
                StrokeWidth = 2.5f
            });
        }
    }

    private void FitMapToPois()
    {
        if (BindingContext is not HomeViewModel vm || vm.Pois.Count == 0)
            return;

        var valid = vm.Pois
            .Where(p => Math.Abs(p.Latitude) > double.Epsilon || Math.Abs(p.Longitude) > double.Epsilon)
            .ToList();

        if (valid.Count == 0)
            return;

        if (valid.Count == 1)
        {
            var p = valid[0];
            var r = Math.Max((p.Radius > 0 ? p.Radius : 25) * 2.5, 120);
            bigMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(p.Latitude, p.Longitude), Distance.FromMeters(r)));
            return;
        }

        var minLat = valid.Min(x => x.Latitude);
        var maxLat = valid.Max(x => x.Latitude);
        var minLng = valid.Min(x => x.Longitude);
        var maxLng = valid.Max(x => x.Longitude);
        var center = new Location((minLat + maxLat) / 2, (minLng + maxLng) / 2);

        // Độ rộng bao phủ (ước lượng mét, có hệ số đệm)
        const double metersPerDegLat = 111_320;
        var latMidRad = center.Latitude * Math.PI / 180;
        var metersPerDegLng = metersPerDegLat * Math.Cos(latMidRad);
        var latSpanM = Math.Max(1, (maxLat - minLat) * metersPerDegLat);
        var lngSpanM = Math.Max(1, (maxLng - minLng) * metersPerDegLng);
        var radiusM = Math.Max(latSpanM, lngSpanM) * 0.65 + 80;
        radiusM = Math.Clamp(radiusM, 180, 8000);

        bigMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromMeters(radiusM)));
    }

    private async void OnPinClicked(object? sender, PinClickedEventArgs e)
    {
        e.HideInfoWindow = true; // Ẩn callout mặc định của Maps, ta dùng ActionSheet riêng

        if (sender is not Pin pin || pin.BindingContext is not Poi poi)
            return;

        // Tính khoảng cách từ vị trí hiện tại
        var distStr = "";
        try
        {
            var loc = await Geolocation.GetLastKnownLocationAsync();
            if (loc is not null)
            {
                var dist = CalculateDistanceMeters(loc.Latitude, loc.Longitude, poi.Latitude, poi.Longitude);
                distStr = dist < 1000 ? $"{dist:F0}m" : $"{dist / 1000:F1}km";
            }
        }
        catch { /* bỏ qua */ }

        var geofenceRadius = Math.Clamp(poi.Radius > 0 ? poi.Radius : 20, 5, 200) + 10;

        // Header info
        var info = new System.Text.StringBuilder();
        info.AppendLine($"📍 {poi.Address ?? "Không có địa chỉ"}");
        if (!string.IsNullOrEmpty(distStr))
            info.AppendLine($"📏 Cách bạn: {distStr}");
        if (poi.Rating > 0)
            info.AppendLine($"⭐ Đánh giá: {poi.Rating:F1} ({poi.ReviewCount} lượt)");
        info.AppendLine($"🔔 Vùng geofence: {geofenceRadius}m");

        var action = await DisplayActionSheet(
            poi.Name,
            "❌ Đóng",
            null,
            "🎵 Xem chi tiết gian hàng",
            "🗺️ Chỉ đường"
        );

        switch (action)
        {
            case "🎵 Xem chi tiết gian hàng":
                await Shell.Current.GoToAsync($"{nameof(StallDetailPage)}?poiId={poi.PoiId}");
                break;
            case "🗺️ Chỉ đường":
                var mapLoc = new Microsoft.Maui.Devices.Sensors.Location(poi.Latitude, poi.Longitude);
                var options = new MapLaunchOptions { Name = poi.Name };
                await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(mapLoc, options);
                break;
        }
    }

    /// <summary>Tính khoảng cách Haversine (meters).</summary>
    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6_371_000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
