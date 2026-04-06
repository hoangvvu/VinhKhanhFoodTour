using Microsoft.Maui.Maps;

namespace VKFoodTour.Mobile.Views;

public partial class FullMapPage : ContentPage
{
    public FullMapPage(ViewModels.HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Tự động di chuyển đến khu vực Vĩnh Khánh khi mở trang
        var vinhKhanh = new Location(10.758, 106.705);
        bigMap.MoveToRegion(MapSpan.FromCenterAndRadius(vinhKhanh, Distance.FromMeters(500)));
    }
}