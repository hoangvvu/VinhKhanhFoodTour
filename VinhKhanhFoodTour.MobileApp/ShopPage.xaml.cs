using VinhKhanhFoodTour.MobileApp.Models;
using System.Text.Json;

namespace VinhKhanhFoodTour.MobileApp;

public partial class ShopPage : ContentPage
{
	private readonly HttpClient _httpClient = new HttpClient();

	public ShopPage()
	{
		InitializeComponent();
		LoadShopsData();
	}

	private async void LoadShopsData()
	{
		try
		{
			string apiUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5149/api/pois" : "https://localhost:7281/api/pois";
			var response = await _httpClient.GetStringAsync(apiUrl);
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var shops = JsonSerializer.Deserialize<List<Poi>>(response, options) ?? new List<Poi>();
			ShopsCollectionView.ItemsSource = shops;
		}
		catch (Exception ex)
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await DisplayAlert("Lỗi", "Không thể tải danh sách gian hàng: " + ex.Message, "OK");
			});
		}
	}
}