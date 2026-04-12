using Microsoft.Maui.Controls;
using VKFoodTour.Mobile.Services;
using VKFoodTour.Mobile.ViewModels;

namespace VKFoodTour.Mobile.Views;

[QueryProperty(nameof(StallName), "name")]
public partial class PlayerPage : ContentPage
{
    private readonly IStallNarrationState _stallState;

    public PlayerPage(PlayerViewModel vm, IStallNarrationState stallState)
    {
        _stallState = stallState;
        InitializeComponent();
        BindingContext = vm;
    }

    public string StallName
    {
        set
        {
            if (BindingContext is PlayerViewModel vm && !string.IsNullOrWhiteSpace(value))
                vm.ApplyStall(Uri.UnescapeDataString(value));
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var fromQr = _stallState.Consume();
        if (fromQr is not null && BindingContext is PlayerViewModel vm)
            vm.ApplyFromQr(fromQr);
    }
}