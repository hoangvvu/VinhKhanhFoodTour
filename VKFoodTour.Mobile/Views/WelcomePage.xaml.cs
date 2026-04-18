using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.Views;

public partial class WelcomePage : ContentPage
{
    private readonly AppShell _shell;
    private readonly IAuthSessionService _session;
    private readonly ISettingsService _settings;

    public WelcomePage(AppShell shell, IAuthSessionService session, ISettingsService settings)
    {
        InitializeComponent();
        _shell = shell;
        _session = session;
        _settings = settings;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Điền URL hiện tại vào ô input
        ServerUrlEntry.Text = _settings.ApiBaseUrl;

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
            Application.Current!.Windows[0].Page = _shell;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WelcomePage] OnExploreClicked error: {ex.Message}");
            ExploreButton.IsEnabled = true;
        }
    }

    private void OnServerToggleClicked(object? sender, EventArgs e)
    {
        ServerPanel.IsVisible = !ServerPanel.IsVisible;
        ServerStatusLabel.Text = string.Empty;
    }

    private void OnSaveServerUrlClicked(object? sender, EventArgs e)
    {
        var url = ServerUrlEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            ServerStatusLabel.TextColor = Color.FromArgb("#f87171");
            ServerStatusLabel.Text = "URL không hợp lệ";
            return;
        }

        _settings.ApiBaseUrl = url;
        ServerStatusLabel.TextColor = Color.FromArgb("#4ade80");
        ServerStatusLabel.Text = $"✓ Đã lưu: {_settings.ApiBaseUrl}";
        System.Diagnostics.Debug.WriteLine($"[WelcomePage] ApiBaseUrl updated → {_settings.ApiBaseUrl}");
    }
}
