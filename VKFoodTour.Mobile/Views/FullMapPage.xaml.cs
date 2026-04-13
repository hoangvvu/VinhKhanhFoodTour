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
            bigMap.Pins.Add(new Pin
            {
                Label = p.Name,
                Address = string.IsNullOrWhiteSpace(p.Address) ? $"{priorityPrefix} {p.Priority}" : p.Address,
                Location = loc,
                Type = PinType.Place,
                BindingContext = p
            });
            bigMap.Pins[^1].MarkerClicked += OnPinClicked;

            var radiusM = Math.Clamp(p.Radius > 0 ? p.Radius : 25, 8, 250);
            bigMap.MapElements.Add(new Circle
            {
                Center = loc,
                Radius = Distance.FromMeters(radiusM),
                StrokeColor = Color.FromRgba(200, 55, 45, 0.9f),
                FillColor = Color.FromRgba(200, 55, 45, 0.14f),
                StrokeWidth = 1
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
        if (sender is Pin pin && pin.BindingContext is Poi poi)
        {
            e.HideInfoWindow = false;
            await Shell.Current.GoToAsync($"{nameof(StallDetailPage)}?poiId={poi.PoiId}");
        }
    }
}
