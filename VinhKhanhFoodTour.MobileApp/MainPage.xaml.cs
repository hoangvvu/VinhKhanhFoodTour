using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using System.Text.Json;
using System.Linq;
using Microsoft.Maui.Controls.Maps;
using VinhKhanhFoodTour.MobileApp.Models;

namespace VinhKhanhFoodTour.MobileApp
{
    public partial class MainPage : ContentPage
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private List<Poi> _allPois = new List<Poi>();
        private CancellationTokenSource _ttsCts;

        public MainPage()
        {
            InitializeComponent();

            // Khởi chạy các dịch vụ ngay khi mở App
            Task.Run(async () => await LoadDataAndStartTracking());
        }

        private void PopulateMapPins(IEnumerable<Poi> listPois)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    mainMap.Pins.Clear();

                    foreach (var poi in listPois)
                    {
                        var pin = new Pin
                        {
                            Label = poi.Name,
                            Address = poi.Address,
                            Type = PinType.Place,
                            Location = new Location((double)poi.Latitude, (double)poi.Longitude)
                        };

                        mainMap.Pins.Add(pin);
                    }

                    // Do not attempt to programmatically move the map here to avoid type/assembly issues
                    // The user can pan/zoom to view pins if the runtime lacks certain Map APIs.
                }
                catch (Exception)
                {
                    // Ignore map add errors to avoid crashing the app
                }
            });
        }

        private async Task LoadDataAndStartTracking()
        {
            try
            {
                // 1. LẤY DỮ LIỆU (Đã sửa IP cho giả lập Android)
                // Lưu ý: Android emulator -> use 10.0.2.2 to reach host machine. Use the HTTP port the API is listening on (launchSettings.json shows http://localhost:5149).
                // Nếu dùng HTTP thì nhớ thêm android:usesCleartextTraffic="true" vào AndroidManifest
                string apiUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5149/api/pois" : "https://localhost:7281/api/pois";

                var response = await _httpClient.GetStringAsync(apiUrl);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _allPois = JsonSerializer.Deserialize<List<Poi>>(response, options) ?? new List<Poi>();

                // Populate map with POI pins
                PopulateMapPins(_allPois);

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
            // Tìm quán ăn nằm trong bán kính 20 mét
            // Lưu ý: Ép kiểu (double) cho tọa độ vì C# yêu cầu tham số double cho hàm CalculateDistance
            var nearbyShop = _allPois.FirstOrDefault(p =>
                userLoc.CalculateDistance(new Location((double)p.Latitude, (double)p.Longitude), DistanceUnits.Kilometers) * 1000 <= 20);

            if (nearbyShop != null)
            {
                // Tính toán khoảng cách thực tế để hiển thị lên màn hình (đổi ra mét)
                double distanceInMeters = userLoc.CalculateDistance(new Location((double)nearbyShop.Latitude, (double)nearbyShop.Longitude), DistanceUnits.Kilometers) * 1000;

                // ÉP CẬP NHẬT GIAO DIỆN VỀ LUỒNG CHÍNH ĐỂ TRÁNH CRASH APP
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblShopName.Text = nearbyShop.Name;
                    lblDescription.Text = nearbyShop.Description;
                    lblDistance.Text = $"{Math.Round(distanceInMeters, 0)} m"; // Hiển thị khoảng cách

                    // Đã xóa phần audioPlayer ở đây.
                    // Nếu bạn muốn TỰ ĐỘNG phát TTS khi tới gần quán, bạn có thể gọi hàm OnPlayClicked(null, null) ở đây.
                    // Tuy nhiên tạm thời cứ để người dùng bấm nút "Phát Thuyết Minh" để dễ test.
                });
            }
        }

        private async void OnPlayClicked(object sender, EventArgs e)
        {
            // Tạm thời hardcode nội dung, sau này sẽ lấy từ SQLite
            string descriptionText = "Chào mừng bạn đến với Ốc Oanh. Quán ốc nổi tiếng nhất nhì khu Vĩnh Khánh với món ốc hương rang muối ớt siêu ngon.";

            // Hủy giọng đọc hiện tại nếu người dùng bấm phát bài mới (chống lặp tiếng)
            CancelCurrentSpeech();
            _ttsCts = new CancellationTokenSource();

            try
            {
                // Cập nhật giao diện nút bấm thành nút Dừng
                btnPlay.Text = "⏹ DỪNG THUYẾT MINH";
                btnPlay.BackgroundColor = Colors.DarkGray;

                // Lấy danh sách giọng đọc trên máy
                var locales = await TextToSpeech.Default.GetLocalesAsync();

                // Giả sử mã ngôn ngữ lấy từ DB là "vi" (Tiếng Việt)
                string langCodeFromDb = "vi";
                var targetLocale = locales.FirstOrDefault(l => l.Language == langCodeFromDb || l.Country == "VN");

                var options = new SpeechOptions()
                {
                    Volume = 1.0f,
                    Pitch = 1.0f,
                    Locale = targetLocale
                };

                // Bắt đầu phát TTS
                await TextToSpeech.Default.SpeakAsync(descriptionText, options, cancelToken: _ttsCts.Token);

                // Khôi phục nút bấm khi đọc xong
                ResetPlayButton();
            }
            catch (TaskCanceledException)
            {
                // Xử lý khi người dùng chủ động bấm dừng
                ResetPlayButton();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi Phát Âm", $"Thiết bị không hỗ trợ hoặc có lỗi: {ex.Message}", "OK");
                ResetPlayButton();
            }
        }

        // Hàm hỗ trợ dừng đọc
        private void CancelCurrentSpeech()
        {
            if (_ttsCts?.IsCancellationRequested == false)
            {
                _ttsCts.Cancel();
            }
        }

        // Hàm hỗ trợ khôi phục UI nút bấm
        private void ResetPlayButton()
        {
            btnPlay.Text = "▶ PHÁT THUYẾT MINH";
            btnPlay.BackgroundColor = Color.FromArgb("#D32F2F");
        }
    }
}
