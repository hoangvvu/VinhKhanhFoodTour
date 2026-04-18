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
        // Luôn hiển thị WelcomePage khi mở app
        var welcome = _services.GetRequiredService<WelcomePage>();
        return new Window(new NavigationPage(welcome));
    }
}