using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.Views;

public partial class WelcomePage : ContentPage
{
    private readonly AppShell _shell;
    private readonly IAuthSessionService _session;

    public WelcomePage(AppShell shell, IAuthSessionService session)
    {
        InitializeComponent();
        _shell = shell;
        _session = session;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Animation nhẹ không async void (để tránh unhandled exception vượt JNI boundary)
        // Dùng Task.Run + ContinueWith thay vì async void OnAppearing
        LogoBorder.Scale = 0.8;
        LogoBorder.Opacity = 0;

        // ViewExtensions này an toàn vì chỉ chạy trên UI thread qua MainThread
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
                // Bỏ qua nếu page bị dispose trước khi animation xong
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
            // Nhấn animation
            await ExploreButton.ScaleTo(0.96, 70);
            await ExploreButton.ScaleTo(1.0, 70);

            // Đặt session ẩn danh rồi chuyển sang Shell
            _session.EnterAnonymous();
            Application.Current!.Windows[0].Page = _shell;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WelcomePage] OnExploreClicked error: {ex.Message}");
            ExploreButton.IsEnabled = true;
        }
    }
}
