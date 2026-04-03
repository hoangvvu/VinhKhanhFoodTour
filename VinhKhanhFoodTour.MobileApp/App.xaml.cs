using Microsoft.Extensions.DependencyInjection;

namespace VinhKhanhFoodTour.MobileApp
{
    public partial class App : Application
    {
        private static bool _isLoggedIn = false;

        public static bool IsLoggedIn
        {
            get => _isLoggedIn;
            set => _isLoggedIn = value;
        }

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Show splash page first, then navigate based on login state
            return new Window(new SplashPage());
        }

        public static void ShowLoginPage()
        {
            Current.MainPage = new LoginShell();
        }

        public static void ShowMainApp()
        {
            _isLoggedIn = true;
            Current.MainPage = new AppShell();
        }
    }
}