namespace VKFoodTour.Mobile;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly Views.LoginPage _loginPage;
    private readonly Services.IAuthSessionService _session;

    public App(AppShell shell, Views.LoginPage loginPage, Services.IAuthSessionService session)
    {
        InitializeComponent();
        _shell = shell;
        _loginPage = loginPage;
        _session = session;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new Window(_session.IsLoggedIn ? _shell : new NavigationPage(_loginPage));
}