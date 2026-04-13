using VKFoodTour.Mobile.Views;

namespace VKFoodTour.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(StallDetailPage), typeof(StallDetailPage));
        }
    }
}
