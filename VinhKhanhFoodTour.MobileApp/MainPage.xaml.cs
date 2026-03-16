using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using System.Text.Json;
using VinhKhanhFoodTour.MobileApp.Models;

namespace VinhKhanhFoodTour.MobileApp
{
    public partial class MainPage : ContentPage
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private List<Poi> _allPois = new List<Poi>();

        public MainPage()
        {
            InitializeComponent();

            // Khởi chạy các dịch vụ ngay khi mở App
            Task.Run(async () => await LoadDataAndStartTracking());
        }

        private async Task LoadDataAndStartTracking()
        {
            try
            {
                // 1. LẤY DỮ LIỆU (Đã sửa IP cho giả lập Android)
                // Lưu ý: Thay cổng 7281 thành cổng HTTP của bạn nếu cần (thường là 5000, 5143...)
                // Nếu dùng HTTP thì nhớ thêm android:usesCleartextTraffic="true" vào AndroidManifest
                string apiUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5000/api/pois" : "https://localhost:7281/api/pois";

                var response = await _httpClient.GetStringAsync(apiUrl);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _allPois = JsonSerializer.Deserialize<List<Poi>>(response, options) ?? new List<Poi>();

                // 2. XIN QUYỀN VÀ THEO DÕI VỊ TRÍ
                MainThread.BeginInvokeOnMainThread(async () => {
                    await StartLocationTracking();
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () => {
                    await DisplayAlert("Lỗi Tải Dữ Liệu", "Chi tiết: " + ex.Message, "OK");
                });
            }
        }

        private async Task StartLocationTracking()
        {
            // XIN QUYỀN VỊ TRÍ NGƯỜI DÙNG CHÍNH THỨC
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Thiếu Quyền", "Bạn phải cấp quyền Vị trí để trải nghiệm thuyết minh tự động.", "Đã hiểu");
                    return; // Dừng nếu khách không cho phép
                }
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));

            while (true)
            {
                try
                {
                    var location = await Geolocation.Default.GetLocationAsync(request);
                    if (location != null)
                    {
                        CheckNearbyShops(location);
                    }
                }
                catch (Exception)
                {
                    // Bỏ qua lỗi nhỏ nếu mất sóng GPS tạm thời
                }

                await Task.Delay(5000);
            }
        }

        private void CheckNearbyShops(Location userLoc)
        {
            var nearbyShop = _allPois.FirstOrDefault(p =>
                userLoc.CalculateDistance(p.Latitude, p.Longitude, DistanceUnits.Kilometers) * 1000 <= 20);

            if (nearbyShop != null)
            {
                // ÉP CẬP NHẬT GIAO DIỆN VỀ LUỒNG CHÍNH ĐỂ TRÁNH CRASH APP
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblShopName.Text = nearbyShop.Name;
                    lblDescription.Text = nearbyShop.Description;

                    if (!string.IsNullOrEmpty(nearbyShop.AudioUrl))
                    {
                        audioPlayer.Source = nearbyShop.AudioUrl;
                    }
                });
            }
        }

        private void OnPlayClicked(object sender, EventArgs e)
        {
            if (audioPlayer.CurrentState == MediaElementState.Playing)
            {
                audioPlayer.Pause();
                btnPlay.Text = "▶ PHÁT THUYẾT MINH";
            }
            else
            {
                audioPlayer.Play();
                btnPlay.Text = "⏸ TẠM DỪNG";
            }
        }
    }
}