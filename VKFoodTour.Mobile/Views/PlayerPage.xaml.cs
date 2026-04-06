using VKFoodTour.Mobile.ViewModels;

namespace VKFoodTour.Mobile.Views;

public partial class PlayerPage : ContentPage
{
    public PlayerPage(PlayerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}