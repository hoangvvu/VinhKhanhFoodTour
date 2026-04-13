using VKFoodTour.Mobile.Localization;

namespace VKFoodTour.Mobile.Services;

public sealed class LocalizationService : ILocalizationService
{
    private readonly ISettingsService _settings;

    public LocalizationService(ISettingsService settings)
    {
        _settings = settings;
    }

    public string CurrentLanguageCode => _settings.SelectedLanguageCode;

    public event EventHandler? LanguageChanged;

    public string GetString(string key, params object[]? formatArgs)
    {
        var s = TranslationStrings.Get(key, CurrentLanguageCode);
        if (formatArgs is { Length: > 0 })
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, s, formatArgs);
        return s;
    }

    public void SetLanguageCode(string code)
    {
        var n = string.IsNullOrWhiteSpace(code) ? "vi" : code.Trim().ToLowerInvariant();
        if (string.Equals(_settings.SelectedLanguageCode, n, StringComparison.OrdinalIgnoreCase))
            return;

        _settings.SelectedLanguageCode = n;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
