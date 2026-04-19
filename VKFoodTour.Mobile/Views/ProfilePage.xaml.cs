using VKFoodTour.Mobile.ViewModels;
using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.Views;

public partial class ProfilePage : ContentPage
{
    private readonly IAuthSessionService _session;
    private readonly IServiceProvider _services;

    public ProfilePage(ProfileViewModel vm, IAuthSessionService session, IServiceProvider services)
    {
        InitializeComponent();
        BindingContext = vm;
        _session = session;
        _services = services;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ProfileViewModel vm)
        {
            vm.SyncApiUrlFromSettings();
            vm.OnFeedbackCompleted = LogOutAndExitApp;
        }
    }

    private void OnLogoutClicked(object? sender, EventArgs e)
    {
        if (BindingContext is ProfileViewModel vm)
        {
            vm.IsFeedbackPopupVisible = true;
        }
    }

    private void OnClosePopupClicked(object? sender, EventArgs e)
    {
        LogOutAndExitApp();
    }

    private void LogOutAndExitApp()
    {
        // 1. Đăng xuất
        _session.Logout();
        
        // 2. Thoát ứng dụng
        Application.Current?.Quit();
    }
}