using VKFoodTour.Mobile.Services;
using VKFoodTour.Mobile.Views;

namespace VKFoodTour.Mobile;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly IServiceProvider _services;
    private readonly IAuthSessionService _session;

    public App(AppShell shell, IServiceProvider services, IAuthSessionService session)
    {
        InitializeComponent(); // Colors.xaml / Styles.xaml được merge tại đây
        _shell = shell;
        _services = services;
        _session = session;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        if (_session.IsLoggedIn)
            return new Window(_shell);

        // Tạo WelcomePage SAU khi InitializeComponent() đã chạy xong
        // → tránh "StaticResource VKRed not found" crash khi inflate XAML
        var welcome = _services.GetRequiredService<WelcomePage>();
        return new Window(new NavigationPage(welcome));
    }
}