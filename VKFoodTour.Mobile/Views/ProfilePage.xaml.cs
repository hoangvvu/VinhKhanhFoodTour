using VKFoodTour.Mobile.ViewModels;

namespace VKFoodTour.Mobile.Views;

public partial class ProfilePage : ContentPage
{
    public ProfilePage(ProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}