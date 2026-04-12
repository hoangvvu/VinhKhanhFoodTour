using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VKFoodTour.Mobile.Models;
using VKFoodTour.Mobile.ViewModels;

namespace VKFoodTour.Mobile.Views;

public partial class FullMapPage : ContentPage
{
    public FullMapPage(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var vinhKhanh = new Location(10.758, 106.705);
        bigMap.MoveToRegion(MapSpan.FromCenterAndRadius(vinhKhanh, Distance.FromMeters(500)));

        if (BindingContext is HomeViewModel vm)
            await vm.LoadDataCommand.ExecuteAsync(null);

        ApplyPinsFromViewModel();
    }

    private void OnCenterOnStreet(object? sender, EventArgs e)
    {
        var vinhKhanh = new Location(10.758, 106.705);
        bigMap.MoveToRegion(MapSpan.FromCenterAndRadius(vinhKhanh, Distance.FromMeters(500)));
    }

    private void ApplyPinsFromViewModel()
    {
        bigMap.Pins.Clear();
        if (BindingContext is not HomeViewModel vm)
            return;

        foreach (var p in vm.Pois)
        {
            if (Math.Abs(p.Latitude) < double.Epsilon && Math.Abs(p.Longitude) < double.Epsilon)
                continue;

            bigMap.Pins.Add(new Pin
            {
                Label = p.Name,
                Address = p.Address,
                Location = new Location(p.Latitude, p.Longitude)
            });
        }
    }
}
