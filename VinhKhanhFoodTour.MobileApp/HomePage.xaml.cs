using VinhKhanhFoodTour.MobileApp.Models;
using System.Text.Json;

namespace VinhKhanhFoodTour.MobileApp;

public partial class HomePage : ContentPage
{
	private readonly HttpClient _httpClient = new HttpClient();
	private List<Poi> _allPois = new List<Poi>();

	public HomePage()
	{
		InitializeComponent();
		LoadPoisData();
	}

	private async void LoadPoisData()
	{
		try
		{
			string apiUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5149/api/pois" : "https://localhost:7281/api/pois";
			var response = await _httpClient.GetStringAsync(apiUrl);
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			_allPois = JsonSerializer.Deserialize<List<Poi>>(response, options) ?? new List<Poi>();
			PoiCollectionView.ItemsSource = _allPois;
		}
		catch (Exception ex)
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await DisplayAlert("Lỗi", "Không thể tải dữ liệu: " + ex.Message, "OK");
			});
		}
	}

	private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
	{
		string searchText = e.NewTextValue?.ToLower() ?? string.Empty;
		if (string.IsNullOrEmpty(searchText))
		{
			PoiCollectionView.ItemsSource = _allPois;
		}
		else
		{
			var filtered = _allPois.Where(p => p.Name.ToLower().Contains(searchText) || p.Address.ToLower().Contains(searchText)).ToList();
			PoiCollectionView.ItemsSource = filtered;
		}
	}
}