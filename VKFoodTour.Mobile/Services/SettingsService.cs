namespace VKFoodTour.Mobile.Services;

public interface ISettingsService
{
    string SelectedLanguageCode { get; set; }
    bool AutoPlayEnabled { get; set; }
}

public class SettingsService : ISettingsService
{
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
}