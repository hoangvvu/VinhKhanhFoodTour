using VKFoodTour.Mobile.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace VKFoodTour.Mobile.Views;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var vinhKhanh = new Location(10.758, 106.705);
        miniMap.MoveToRegion(MapSpan.FromCenterAndRadius(vinhKhanh, Distance.FromMeters(500)));

        // Load dữ liệu từ ViewModel
        if (BindingContext is HomeViewModel vm)
        {
            await vm.LoadDataCommand.ExecuteAsync(null);
        }
    }
}