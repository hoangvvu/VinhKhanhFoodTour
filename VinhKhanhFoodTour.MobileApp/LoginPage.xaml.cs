namespace VinhKhanhFoodTour.MobileApp;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}

	private async void OnLoginClicked(object sender, EventArgs e)
	{
		string email = EntryEmail.Text;
		string password = EntryPassword.Text;

		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
		{
			await DisplayAlert("Lỗi", "Vui lòng nhập email và mật khẩu", "OK");
			return;
		}

		// Simulate login validation
		if (email.Contains("@") && password.Length >= 3)
		{
			await DisplayAlert("Thành công", "Đăng nhập thành công", "OK");

			// Store login state and show main app
			App.ShowMainApp();
		}
		else
		{
			await DisplayAlert("Lỗi", "Email hoặc mật khẩu không hợp lệ", "OK");
		}
	}
}