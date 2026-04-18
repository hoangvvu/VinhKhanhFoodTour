using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.Views;

public partial class WelcomePage : ContentPage
{
    private readonly AppShell _shell;
    private readonly IAuthSessionService _session;
    private readonly ISettingsService _settings;
    private readonly IDataService _dataService;

    public WelcomePage(AppShell shell, IAuthSessionService session, ISettingsService settings, IDataService dataService)
    {
        InitializeComponent();
        _shell = shell;
        _session = session;
        _settings = settings;
        _dataService = dataService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Animation nhẹ không async void
        LogoBorder.Scale = 0.8;
        LogoBorder.Opacity = 0;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await Task.WhenAll(
                    LogoBorder.ScaleTo(1.0, 450, Easing.SpringOut),
                    LogoBorder.FadeTo(1.0, 350));
            }
            catch
            {
                LogoBorder.Scale = 1.0;
                LogoBorder.Opacity = 1.0;
            }
        });
    }

    private async void OnExploreClicked(object? sender, EventArgs e)
    {
        ExploreButton.IsEnabled = false;

        try
        {
            await ExploreButton.ScaleTo(0.96, 70);
            await ExploreButton.ScaleTo(1.0, 70);

            _session.EnterAnonymous();
            
            // Ghi nhận log active khi vùa vào app
            await _dataService.TrackEventAsync(poiId: null, eventType: "move");

            Application.Current!.Windows[0].Page = _shell;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WelcomePage] OnExploreClicked error: {ex.Message}");
            ExploreButton.IsEnabled = true;
        }
    }
}
