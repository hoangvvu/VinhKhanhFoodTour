using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly IDataService _dataService;
    private readonly IAuthSessionService _session;
    private readonly AppShell _shell;
    private readonly ILocalizationService _localization;
    private bool _isRegisterMode;

    public LoginPage(IDataService dataService, IAuthSessionService session, AppShell shell, ILocalizationService localization)
    {
        InitializeComponent();
        _dataService = dataService;
        _session = session;
        _shell = shell;
        _localization = localization;
        _localization.LanguageChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(ApplyUiStrings);
        ApplyUiStrings();
    }

    private void ApplyUiStrings()
    {
        BrandLabel.Text = _localization.GetString("Login_Brand");
        NameEntry.Placeholder = _localization.GetString("Login_NamePh");
        EmailEntry.Placeholder = _localization.GetString("Login_EmailPh");
        PasswordEntry.Placeholder = _localization.GetString("Login_PasswordPh");
        ConfirmPasswordEntry.Placeholder = _localization.GetString("Login_ConfirmPh");
        if (!_isRegisterMode)
        {
            ModeLabel.Text = _localization.GetString("Login_ModeLogin");
            SubmitButton.Text = _localization.GetString("Login_SubmitLogin");
            ToggleButton.Text = _localization.GetString("Login_ToggleToRegister");
            GuestButton.IsVisible = true;
            GuestButton.Text = _localization.GetString("Login_GuestContinue");
        }
        else
        {
            ModeLabel.Text = _localization.GetString("Login_ModeRegister");
            SubmitButton.Text = _localization.GetString("Login_SubmitRegister");
            ToggleButton.Text = _localization.GetString("Login_ToggleToLogin");
            GuestButton.IsVisible = false;
        }
    }

    private void OnToggleMode(object? sender, EventArgs e)
    {
        _isRegisterMode = !_isRegisterMode;
        NameEntry.IsVisible = _isRegisterMode;
        ConfirmPasswordEntry.IsVisible = _isRegisterMode;
        ApplyUiStrings();
        StatusLabel.Text = string.Empty;
    }

    private async void OnSubmit(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            StatusLabel.Text = _localization.GetString("Login_ErrEmailPassword");
            return;
        }

        SubmitButton.IsEnabled = false;
        StatusLabel.Text = _localization.GetString("Login_Processing");
        try
        {
            VKFoodTour.Shared.DTOs.AuthResponseDto? response;
            if (_isRegisterMode)
            {
                var name = NameEntry.Text?.Trim() ?? string.Empty;
                var confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name))
                {
                    StatusLabel.Text = _localization.GetString("Login_ErrName");
                    return;
                }

                if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
                {
                    StatusLabel.Text = _localization.GetString("Login_ErrPasswordMatch");
                    return;
                }

                response = await _dataService.RegisterAsync(name, email, password);
            }
            else
            {
                response = await _dataService.LoginAsync(email, password);
            }

            if (response?.Success == true && response.User is not null)
            {
                _session.SetUser(response.User);
                Application.Current!.Windows[0].Page = _shell;
                return;
            }

            StatusLabel.Text = response?.Message ?? _localization.GetString("Login_ServerError");
        }
        finally
        {
            SubmitButton.IsEnabled = true;
        }
    }

    private async void OnContinueAsGuest(object? sender, EventArgs e)
    {
        GuestButton.IsEnabled = false;
        StatusLabel.Text = _localization.GetString("Login_GuestEntering");
        try
        {
            _session.EnterAnonymous();
            await _dataService.TrackEventAsync(
                poiId: null,
                eventType: "move",
                languageCode: $"anon:{_localization.CurrentLanguageCode}");
            Application.Current!.Windows[0].Page = _shell;
        }
        finally
        {
            GuestButton.IsEnabled = true;
        }
    }
}
