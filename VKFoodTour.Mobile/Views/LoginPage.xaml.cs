using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly IDataService _dataService;
    private readonly IAuthSessionService _session;
    private readonly ISettingsService _settings;
    private readonly AppShell _shell;
    private bool _isRegisterMode;

    public LoginPage(IDataService dataService, IAuthSessionService session, ISettingsService settings, AppShell shell)
    {
        InitializeComponent();
        _dataService = dataService;
        _session = session;
        _settings = settings;
        _shell = shell;
        ApiUrlEntry.Text = _settings.ApiBaseUrl;
    }

    private void OnToggleMode(object? sender, EventArgs e)
    {
        _isRegisterMode = !_isRegisterMode;
        NameEntry.IsVisible = _isRegisterMode;
        ConfirmPasswordEntry.IsVisible = _isRegisterMode;
        SubmitButton.Text = _isRegisterMode ? "Đăng ký" : "Đăng nhập";
        ModeLabel.Text = _isRegisterMode ? "Tạo tài khoản mới để sử dụng ứng dụng" : "Đăng nhập để sử dụng ứng dụng";
        ToggleButton.Text = _isRegisterMode ? "Đã có tài khoản? Đăng nhập" : "Chưa có tài khoản? Đăng ký";
        StatusLabel.Text = string.Empty;
    }

    private async void OnSubmit(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;
        var apiUrl = ApiUrlEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            StatusLabel.Text = "Vui lòng nhập địa chỉ API.";
            return;
        }

        _settings.ApiBaseUrl = apiUrl;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            StatusLabel.Text = "Vui lòng nhập email và mật khẩu.";
            return;
        }

        SubmitButton.IsEnabled = false;
        StatusLabel.Text = "Đang xử lý...";
        try
        {
            VKFoodTour.Shared.DTOs.AuthResponseDto? response;
            if (_isRegisterMode)
            {
                var name = NameEntry.Text?.Trim() ?? string.Empty;
                var confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name))
                {
                    StatusLabel.Text = "Vui lòng nhập họ tên.";
                    return;
                }

                if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
                {
                    StatusLabel.Text = "Mật khẩu xác nhận không khớp.";
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

            StatusLabel.Text = response?.Message ?? "Không kết nối được máy chủ.";
        }
        finally
        {
            SubmitButton.IsEnabled = true;
        }
    }
}
