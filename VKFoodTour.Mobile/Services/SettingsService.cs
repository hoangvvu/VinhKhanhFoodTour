namespace VKFoodTour.Mobile.Services;

public interface ISettingsService
{
    string SelectedLanguageCode { get; set; }
    bool AutoPlayEnabled { get; set; }

    /// <summary>Gốc API không có / cuối, ví dụ http://10.0.2.2:5242 (Android emulator) hoặc http://192.168.1.5:5242 (máy thật).</summary>
    string ApiBaseUrl { get; set; }
}

public class SettingsService : ISettingsService
{
    private const string LegacyLanApi = "http://192.168.1.8:5242";

    private static string DefaultApiBase() =>
#if ANDROID
        "https://cleaver-distress-shush.ngrok-free.dev";
#else
        "http://localhost:5242";
#endif

    public string SelectedLanguageCode
    {
        get => Preferences.Default.Get(nameof(SelectedLanguageCode), "vi");
        set => Preferences.Default.Set(nameof(SelectedLanguageCode), value);
    }

    public bool AutoPlayEnabled
    {
        get => Preferences.Default.Get(nameof(AutoPlayEnabled), true);
        set => Preferences.Default.Set(nameof(AutoPlayEnabled), value);
    }

    public string ApiBaseUrl
    {
        get
        {
            var saved = Preferences.Default.Get(nameof(ApiBaseUrl), DefaultApiBase()).Trim().TrimEnd('/');
            if (string.Equals(saved, LegacyLanApi, StringComparison.OrdinalIgnoreCase))
            {
                var fallback = DefaultApiBase();
                Preferences.Default.Set(nameof(ApiBaseUrl), fallback);
                return fallback;
            }

            return saved;
        }
        set => Preferences.Default.Set(nameof(ApiBaseUrl), value.Trim().TrimEnd('/'));
    }
}