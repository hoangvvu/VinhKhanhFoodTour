namespace VinhKhanhFoodTour.MobileApp;

public partial class ProfilePage : ContentPage
{
	public ProfilePage()
	{
		InitializeComponent();
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		bool result = await DisplayAlert("Đăng xuất", "Bạn có chắc muốn đăng xuất?", "Có", "Không");

		if (result)
		{
			App.IsLoggedIn = false;
			App.ShowLoginPage();
		}
	}
}