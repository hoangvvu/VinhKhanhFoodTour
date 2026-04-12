using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using VKFoodTour.Mobile.Services;
using VKFoodTour.Mobile.ViewModels;
using VKFoodTour.Mobile.Views;
using ZXing.Net.Maui.Controls;

namespace VKFoodTour.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .UseMauiCommunityToolkit()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                // Thêm font của bạn nếu đã chép vào Resources/Fonts
                fonts.AddFont("BebasNeue-Regular.ttf", "BebasNeue");
            });

        // Đăng ký Services
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IStallNarrationState, StallNarrationState>();
        builder.Services.AddHttpClient<IDataService, DataService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(25);
        });

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<App>();

        // Đăng ký ViewModels
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<StallListViewModel>();
        builder.Services.AddTransient<PlayerViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<QrScanViewModel>();

        // Đăng ký Pages (Views)
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<StallListPage>();
        builder.Services.AddTransient<PlayerPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<FullMapPage>();
        builder.Services.AddTransient<QrScanPage>();

        return builder.Build();
    }
}