using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using VKFoodTour.Mobile.Services;
using VKFoodTour.Mobile.ViewModels;
using VKFoodTour.Mobile.Views;
using ZXing.Net.Maui.Controls;
#if ANDROID
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls.Handlers.Items;
#endif

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

        // Đăng ký Services cơ bản
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
        builder.Services.AddSingleton<IStallNarrationState, StallNarrationState>();
        builder.Services.AddSingleton<IAuthSessionService, AuthSessionService>();
        builder.Services.AddSingleton<IFavoriteService, FavoriteService>();
        builder.Services.AddHttpClient<IHttpImageService, HttpImageService>((_, client) => client.ConfigureVkMediaClient());
        builder.Services.AddHttpClient<IDataService, DataService>((_, client) => client.ConfigureVkApiClient());

        // Tour Service - gọi API tour
        builder.Services.AddHttpClient<ITourService, TourService>((_, client) => client.ConfigureVkApiClient());

        // Audio Services
        builder.Services.AddHttpClient("VkAudio", (_, client) => client.ConfigureVkMediaClient());
        builder.Services.AddSingleton<IAudioPlaybackService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var http = factory.CreateClient("VkAudio");
            return new AudioPlaybackService(
                sp.GetRequiredService<IAudioManager>(),
                http,
                sp.GetRequiredService<ISettingsService>());
        });

        // Audio Queue Service - quản lý hàng đợi audio
        builder.Services.AddSingleton<IAudioQueueService>(sp =>
        {
            return new AudioQueueService(
                sp.GetRequiredService<IAudioPlaybackService>(),
                sp.GetRequiredService<IDataService>(),
                sp.GetRequiredService<ILocalizationService>());
        });

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
        builder.Services.AddSingleton<TourPlayerViewModel>(); // Singleton để giữ trạng thái tour

        // Đăng ký Pages (Views)
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<StallListPage>();
        builder.Services.AddTransient<PlayerPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<FullMapPage>();
        builder.Services.AddTransient<QrScanPage>();
        builder.Services.AddTransient<StallDetailPage>();
        builder.Services.AddSingleton<TourPlayerPage>(); // Singleton để giữ UI tour

#if ANDROID
        // CollectionView → RecyclerView trong ScrollView: tắt nested scroll (tránh crash JNI).
        CollectionViewHandler.Mapper.AppendToMapping(
            "VkDisableRecyclerNestedScrolling",
            static (handler, _) =>
            {
                if (handler.PlatformView is RecyclerView rv)
                    rv.NestedScrollingEnabled = false;
            });
#endif

        return builder.Build();
    }
}