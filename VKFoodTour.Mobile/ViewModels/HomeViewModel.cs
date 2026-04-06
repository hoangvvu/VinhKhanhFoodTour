using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VKFoodTour.Mobile.Models;
using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IDataService _dataService;

    // Các biến dùng [ObservableProperty] PHẢI viết thường chữ cái đầu (camelCase)
    // Thư viện sẽ tự tạo ra bản chữ HOA (Pois, NearestPoi, CurrentLang...)

    [ObservableProperty]
    private ObservableCollection<Poi> pois = new();

    [ObservableProperty]
    private Poi? nearestPoi;

    [ObservableProperty]
    private string currentLang = "vi";

    [ObservableProperty]
    private string nowPlayingText = "Sẵn sàng thuyết minh...";

    [ObservableProperty]
    private bool isAutoPlayEnabled = true;

    [ObservableProperty]
    private bool isBusy;

    public HomeViewModel(IDataService dataService)
    {
        _dataService = dataService;

        // Gọi hàm load dữ liệu khi khởi động
        _ = LoadDataAsync();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        // Ngăn chặn việc gọi lại nhiều lần khi đang tải
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Gọi Service lấy dữ liệu từ API hoặc Mock Data
            var result = await _dataService.GetPoisAsync();

            if (result != null && result.Any())
            {
                // 1. Sắp xếp quán NỔI BẬT (Priority cao nhất) lên đầu 
                // và gán vào ObservableCollection để giao diện cập nhật
                var sortedPois = result.OrderByDescending(p => p.Priority).ToList();
                Pois = new ObservableCollection<Poi>(sortedPois);

                // 2. Tìm quán "GẦN NHẤT" hoặc "NHIỀU ĐÁNH GIÁ" nhất để hiện ở Card lớn
                // Tạm thời mình lấy quán đầu tiên trong danh sách đã sắp xếp
                NearestPoi = Pois.FirstOrDefault();

                // 3. Cập nhật trạng thái Audio đang phát (nếu có)
                if (NearestPoi != null)
                {
                    NowPlayingText = $"Đã tìm thấy {Pois.Count} gian hàng quanh bạn.";
                }
            }
            else
            {
                NowPlayingText = "Không tìm thấy gian hàng nào gần đây.";
            }
        }
        catch (Exception ex)
        {
            // Ghi log lỗi và thông báo lên UI
            NowPlayingText = "Lỗi tải dữ liệu: Kiểm tra kết nối mạng.";
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            // Giải phóng trạng thái bận
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenFullMap()
    {
        // Sửa lại Route cho đúng trang bạn muốn hiển thị bản đồ lớn
        // Nếu muốn sang tab Gian hàng: //stalls
        // Nếu muốn về trang có bản đồ (tùy theo Route bạn đặt trong AppShell):
        await Shell.Current.GoToAsync("//fullmap");
    }

    [RelayCommand]
    private void ChangeLanguage()
    {
        // Logic đổi vòng: vi -> en -> zh -> vi
        CurrentLang = CurrentLang switch
        {
            "vi" => "en",
            "en" => "zh",
            _ => "vi"
        };

        // Thông báo cho người dùng biết đã đổi ngôn ngữ
        var langName = CurrentLang == "vi" ? "Tiếng Việt" : (CurrentLang == "en" ? "English" : "中文");
        NowPlayingText = $"Đã chuyển sang: {langName}";
    }

    [RelayCommand]
    private void ToggleAutoPlay()
    {
        IsAutoPlayEnabled = !IsAutoPlayEnabled;
        NowPlayingText = IsAutoPlayEnabled
            ? "Chế độ tự động đang bật"
            : "Chế độ tự động đã tắt";
    }

    [RelayCommand]
    private async Task PlayCommand()
    {
        if (NearestPoi != null)
        {
            NowPlayingText = $"Đang phát thuyết minh: {NearestPoi.Name}";
            // Sau này sẽ gọi Service phát Audio tại đây
        }
    }

    [RelayCommand]
    private void Stop()
    {
        NowPlayingText = string.Empty;
    }
}

// ═══════════════════════════════════════════════════════
//  StallListViewModel — Danh sách quán
// ═══════════════════════════════════════════════════════
public partial class StallListViewModel : ObservableObject
{
    private readonly IDataService _dataService;

    [ObservableProperty]
    private ObservableCollection<Poi> pois = new();

    public StallListViewModel(IDataService dataService)
    {
        _dataService = dataService;
        _ = LoadPoisAsync();
    }

    [RelayCommand]
    private async Task LoadPoisAsync()
    {
        var result = await _dataService.GetPoisAsync();
        Pois = new ObservableCollection<Poi>(result);
    }
}

// ═══════════════════════════════════════════════════════
//  PlayerViewModel — Trình phát nhạc
// ═══════════════════════════════════════════════════════
public partial class PlayerViewModel : ObservableObject
{
    [ObservableProperty]
    private string nowPlayingName = "Chưa phát";

    [ObservableProperty]
    private string nowPlayingText = "Chọn một quán để nghe thuyết minh";

    [ObservableProperty]
    private string selectedLang = "vi";

    [RelayCommand]
    private void Stop()
    {
        NowPlayingName = "Chưa phát";
        NowPlayingText = "Chọn một quán để nghe thuyết minh";
    }
}

// ═══════════════════════════════════════════════════════
//  ProfileViewModel — Hồ sơ
// ═══════════════════════════════════════════════════════
public partial class ProfileViewModel : ObservableObject
{
    [ObservableProperty]
    private int listenCount = 5;

    [ObservableProperty]
    private int favoriteCount = 2;

    [ObservableProperty]
    private string selectedLang = "Tiếng Việt";
}