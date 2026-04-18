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

            // Bọc fire-and-forget trong try/catch để không gây JavaProxyThrowable
            _ = Task.Run(async () =>
            {
                try
                {
                    await vm.LoadLanguageOptionsAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProfilePage] LoadLanguageOptions error: {ex.Message}");
                }
            });
        }
    }

    private void OnLogoutClicked(object? sender, EventArgs e)
    {
        _session.Logout();

        // Sau khi bỏ đăng nhập, quay về WelcomePage thay vì LoginPage
        var welcome = _services.GetRequiredService<WelcomePage>();
        Application.Current!.Windows[0].Page = new NavigationPage(welcome);
    }
}