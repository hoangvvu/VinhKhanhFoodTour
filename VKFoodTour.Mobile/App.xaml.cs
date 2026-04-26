using VKFoodTour.Mobile.Services;
using VKFoodTour.Mobile.Views;

namespace VKFoodTour.Mobile;

public partial class App : Application
{
    private const int ForegroundHeartbeatSeconds = 20;
    private readonly IServiceProvider _services;
    private readonly IAuthSessionService _session;
    private readonly ILocalizationService _localization;
    private readonly ISettingsService _settings;
    private System.Threading.Timer? _foregroundHeartbeatTimer;

    public App(IServiceProvider services, IAuthSessionService session, ILocalizationService localization, ISettingsService settings)
    {
        InitializeComponent(); // Colors.xaml / Styles.xaml được merge tại đây
        UserAppTheme = AppTheme.Light;
        _services = services;
        _session = session;
        _localization = localization;
        _settings = settings;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Mỗi lần mở app mới (Cold Start) -> Reset về Tiếng Anh và bắt đầu chọn lại
        _localization.SetLanguageCode("en");
        _settings.HasPickedLanguage = false;

        // Luôn hiển thị WelcomePage khi mở app
        var welcome = _services.GetRequiredService<WelcomePage>();
        var window = new Window(new NavigationPage(welcome));

        void StopForegroundHeartbeat()
        {
            _foregroundHeartbeatTimer?.Dispose();
            _foregroundHeartbeatTimer = null;
        }

        void StartForegroundHeartbeat()
        {
            StopForegroundHeartbeat();
            _foregroundHeartbeatTimer = new System.Threading.Timer(_ =>
            {
                var dataService = _services.GetService<IDataService>();
                if (dataService != null)
                    _ = dataService.TrackEventAsync(poiId: null, eventType: "move");
            }, null, TimeSpan.FromSeconds(ForegroundHeartbeatSeconds), TimeSpan.FromSeconds(ForegroundHeartbeatSeconds));
        }

        // Báo offline khi thoát/ẩn app
        window.Stopped += (s, e) =>
        {
            StopForegroundHeartbeat();
            var dataService = _services.GetService<IDataService>();
            if (dataService != null)
                _ = dataService.TrackEventAsync(poiId: null, eventType: "exit");
        };

        window.Destroying += (s, e) =>
        {
            StopForegroundHeartbeat();
            var dataService = _services.GetService<IDataService>();
            if (dataService != null)
                _ = dataService.TrackEventAsync(poiId: null, eventType: "exit");
        };

        // Báo online lại khi mở app lên
        window.Resumed += (s, e) =>
        {
            StartForegroundHeartbeat();
            var dataService = _services.GetService<IDataService>();
            if (dataService != null)
                _ = dataService.TrackEventAsync(poiId: null, eventType: "move");
        };

        // Bắt đầu heartbeat ngay khi app mở lần đầu.
        StartForegroundHeartbeat();

        return window;
    }
}