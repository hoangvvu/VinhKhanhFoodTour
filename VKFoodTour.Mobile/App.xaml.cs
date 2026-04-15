using VKFoodTour.Mobile.Services;
using VKFoodTour.Mobile.Views;

namespace VKFoodTour.Mobile;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly LoginPage _loginPage;
    private readonly IAuthSessionService _session;

    public App(AppShell shell, LoginPage loginPage, IAuthSessionService session)
    {
        InitializeComponent();
        _shell = shell;
        _loginPage = loginPage;
        _session = session;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        _session.IsLoggedIn
            ? new Window(_shell)
            : new Window(new NavigationPage(_loginPage));
}