using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using VKFoodTour.Mobile.Services;     // Sửa lại namespace này
using VKFoodTour.Mobile.ViewModels;   // Sửa lại namespace này
using VKFoodTour.Mobile.Views;        // Sửa lại namespace này

namespace VKFoodTour.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiMaps() // <-- Thêm dòng này
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                // Thêm font của bạn nếu đã chép vào Resources/Fonts
                fonts.AddFont("BebasNeue-Regular.ttf", "BebasNeue");
            });

        // Đăng ký Services
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IDataService, DataService>();

        // Đăng ký ViewModels
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<StallListViewModel>();
        builder.Services.AddTransient<PlayerViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();


        // Đăng ký Pages (Views)
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<StallListPage>();
        builder.Services.AddTransient<PlayerPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<FullMapPage>();

        return builder.Build();
    }
}