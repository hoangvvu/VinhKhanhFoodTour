using VKFoodTour.Mobile.ViewModels;
using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.Views;

public partial class ProfilePage : ContentPage
{
    private readonly IAuthSessionService _session;
    private readonly LoginPage _loginPage;

    public ProfilePage(ProfileViewModel vm, IAuthSessionService session, LoginPage loginPage)
    {
        InitializeComponent();
        BindingContext = vm;
        _session = session;
        _loginPage = loginPage;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ProfileViewModel vm)
            vm.SyncApiUrlFromSettings();
    }

    private void OnLogoutClicked(object? sender, EventArgs e)
    {
        _session.Logout();
        Application.Current!.Windows[0].Page = new NavigationPage(_loginPage);
    }
}