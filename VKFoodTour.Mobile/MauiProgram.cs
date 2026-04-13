using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
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
            .AddAudio()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                // Thêm font của bạn nếu đã chép vào Resources/Fonts
                fonts.AddFont("BebasNeue-Regular.ttf", "BebasNeue");
            });

        // Đăng ký Services
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
        builder.Services.AddSingleton<IStallNarrationState, StallNarrationState>();
        builder.Services.AddSingleton<IAuthSessionService, AuthSessionService>();
        builder.Services.AddSingleton<IFavoriteService, FavoriteService>();
        builder.Services.AddHttpClient<IHttpImageService, HttpImageService>((_, client) => client.ConfigureVkMediaClient());
        builder.Services.AddHttpClient<IDataService, DataService>((_, client) => client.ConfigureVkApiClient());
        builder.Services.AddHttpClient<IAudioPlaybackService, AudioPlaybackService>((_, client) => client.ConfigureVkMediaClient());

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<App>();

        // Đăng ký ViewModels
        // Dùng chung cho Home + Bản đồ để danh sách POI và trạng thái tải đồng bộ.
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddTransient<StallListViewModel>();
        builder.Services.AddTransient<PlayerViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<QrScanViewModel>();
        builder.Services.AddTransient<StallDetailViewModel>();

        // Đăng ký Pages (Views)
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<StallListPage>();
        builder.Services.AddTransient<PlayerPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<FullMapPage>();
        builder.Services.AddTransient<QrScanPage>();
        builder.Services.AddTransient<StallDetailPage>();

        return builder.Build();
    }
}