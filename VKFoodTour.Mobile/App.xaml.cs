using VKFoodTour.Mobile.Services;
using VKFoodTour.Mobile.Views;

namespace VKFoodTour.Mobile;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly IServiceProvider _services;
    private readonly IAuthSessionService _session;

    // ✅ Không inject LoginPage trực tiếp nữa
    public App(AppShell shell, IServiceProvider services, IAuthSessionService session)
    {
        InitializeComponent(); // ← Colors.xaml được merge TẠI ĐÂY
        _shell = shell;
        _services = services;
        _session = session;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        if (_session.IsLoggedIn)
            return new Window(_shell);

        // ✅ LoginPage chỉ được tạo SAU InitializeComponent()
        var loginPage = _services.GetRequiredService<LoginPage>();
        return new Window(new NavigationPage(loginPage));
    }
}