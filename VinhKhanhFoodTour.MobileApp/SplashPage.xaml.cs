namespace VinhKhanhFoodTour.MobileApp;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        InitializeComponent();
        AnimateSplash();
    }

    private async void AnimateSplash()
    {
        try
        {
            // Scale logo box animation
            if (LogoBox != null)
            {
                await LogoBox.ScaleTo(1.2, 500);
                await LogoBox.ScaleTo(1.0, 300);
            }

            // Fade in title
            if (TitleLabel != null)
                await TitleLabel.FadeTo(1, 500);
            if (SubtitleLabel != null)
                await SubtitleLabel.FadeTo(1, 500);
            if (LoadingDots != null)
                await LoadingDots.FadeTo(1, 500);

            // Animate loading dots
            for (int i = 0; i < 3; i++)
            {
                await AnimateLoadingDots();
            }

            // Navigate to login after delay
            await Task.Delay(1000);
            App.ShowLoginPage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Splash animation error: {ex}");
            App.ShowLoginPage();
        }
    }

    private async Task AnimateLoadingDots()
    {
        if (Dot1 != null)
        {
            await Dot1.FadeTo(0.3, 400);
            await Dot1.FadeTo(1, 400);
        }

        if (Dot2 != null)
        {
            await Dot2.FadeTo(0.3, 400);
            await Dot2.FadeTo(1, 400);
        }

        if (Dot3 != null)
        {
            await Dot3.FadeTo(0.3, 400);
            await Dot3.FadeTo(1, 400);
        }
    }
}
