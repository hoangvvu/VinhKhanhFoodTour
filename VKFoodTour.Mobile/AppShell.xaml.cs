using VKFoodTour.Mobile.Services;
using VKFoodTour.Mobile.Views;

namespace VKFoodTour.Mobile
{
    public partial class AppShell : Shell
    {
        private readonly ILocalizationService _localization;

        public AppShell(ILocalizationService localization)
        {
            InitializeComponent();
            _localization = localization;
            ApplyTabTitles();
            _localization.LanguageChanged += (_, _) =>
                MainThread.BeginInvokeOnMainThread(ApplyTabTitles);
            Routing.RegisterRoute(nameof(StallDetailPage), typeof(StallDetailPage));
        }

        private void ApplyTabTitles()
        {
            TabHome.Title = _localization.GetString("Shell_Home");
            TabMap.Title = _localization.GetString("Shell_Map");
            TabStalls.Title = _localization.GetString("Shell_Stalls");
            TabProfile.Title = _localization.GetString("Shell_Profile");
        }
    }
}
