using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VKFoodTour.Mobile.Models;
using VKFoodTour.Mobile.Services;
using VKFoodTour.Mobile.Views;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IFavoriteService _favorites;

    // Các biến dùng [ObservableProperty] PHẢI viết thường chữ cái đầu (camelCase)
    // Thư viện sẽ tự tạo ra bản chữ HOA (Pois, NearestPoi, CurrentLang...)

    [ObservableProperty]
    private ObservableCollection<Poi> pois = new();

    [ObservableProperty]
    private ObservableCollection<Poi> favoritePois = new();

    [ObservableProperty]
    private bool hasFavoritePois;

    [ObservableProperty]
    private ObservableCollection<ReviewListItemDto> recentReviews = new();

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

    public HomeViewModel(IDataService dataService, IFavoriteService favorites)
    {
        _dataService = dataService;
        _favorites = favorites;

        // Gọi hàm load dữ liệu khi khởi động
        _ = LoadDataAsync();
    }

    [RelayCommand]
    public async Task LoadDataAsync(CancellationToken cancellationToken = default)
    {
        // Ngăn chặn việc gọi lại nhiều lần khi đang tải
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Gọi Service lấy dữ liệu từ API hoặc Mock Data
            var result = await _dataService.GetPoisAsync(cancellationToken);

            if (result != null && result.Any())
            {
                // 1. Sắp xếp quán NỔI BẬT (Priority cao nhất) lên đầu 
                // và gán vào ObservableCollection để giao diện cập nhật
                var sortedPois = result.OrderByDescending(p => p.Priority).ToList();
                foreach (var p in sortedPois)
                    p.IsFavorite = _favorites.IsFavorite(p.PoiId);

                Pois = new ObservableCollection<Poi>(sortedPois);

                // 2. Tìm quán "GẦN NHẤT" hoặc "NHIỀU ĐÁNH GIÁ" nhất để hiện ở Card lớn
                // Tạm thời mình lấy quán đầu tiên trong danh sách đã sắp xếp
                NearestPoi = Pois.FirstOrDefault();

                RefreshFavoritePois();

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

            var reviews = await _dataService.GetRecentReviewsAsync(25, cancellationToken);
            RecentReviews = new ObservableCollection<ReviewListItemDto>(reviews);
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

    private void RefreshFavoritePois()
    {
        var fav = Pois.Where(p => _favorites.IsFavorite(p.PoiId)).ToList();
        FavoritePois = new ObservableCollection<Poi>(fav);
        HasFavoritePois = FavoritePois.Count > 0;
    }

    [RelayCommand]
    private void ToggleFavorite(Poi? poi)
    {
        if (poi is null)
            return;

        _favorites.Toggle(poi.PoiId);
        poi.IsFavorite = _favorites.IsFavorite(poi.PoiId);
        RefreshFavoritePois();
    }

    [RelayCommand]
    private async Task OpenStallAsync(Poi? poi)
    {
        if (poi is null)
            return;

        await Shell.Current.GoToAsync($"{nameof(StallDetailPage)}?poiId={poi.PoiId}");
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
    private readonly IFavoriteService _favorites;

    [ObservableProperty]
    private ObservableCollection<Poi> pois = new();

    public StallListViewModel(IDataService dataService, IFavoriteService favorites)
    {
        _dataService = dataService;
        _favorites = favorites;
        _ = LoadPoisAsync();
    }

    [RelayCommand]
    private async Task LoadPoisAsync()
    {
        var result = await _dataService.GetPoisAsync();
        foreach (var p in result)
            p.IsFavorite = _favorites.IsFavorite(p.PoiId);
        Pois = new ObservableCollection<Poi>(result);
    }

    [RelayCommand]
    private void ToggleFavorite(Poi? poi)
    {
        if (poi is null)
            return;
        _favorites.Toggle(poi.PoiId);
        poi.IsFavorite = _favorites.IsFavorite(poi.PoiId);
    }
}

// ═══════════════════════════════════════════════════════
//  PlayerViewModel — Trình phát nhạc
// ═══════════════════════════════════════════════════════
public partial class PlayerViewModel : ObservableObject
{
    private readonly IAudioPlaybackService _audio;

    [ObservableProperty]
    private string nowPlayingName = "Chưa phát";

    [ObservableProperty]
    private string nowPlayingText = "Chọn một quán để nghe thuyết minh";

    [ObservableProperty]
    private string selectedLang = "vi";

    [ObservableProperty]
    private string? audioUrl;

    [ObservableProperty]
    private bool hasAudio;

    [ObservableProperty]
    private string audioHint = "Chưa có file âm thanh cho gian hàng này.";

    public PlayerViewModel(IAudioPlaybackService audio)
    {
        _audio = audio;
    }

    public void ApplyStall(string name)
    {
        NowPlayingName = name;
        NowPlayingText = "Thuyết minh sẽ lấy từ dữ liệu Admin/API khi có file âm thanh.";
        AudioUrl = null;
        HasAudio = false;
        AudioHint = "Chưa có file âm thanh cho gian hàng này.";
    }

    public void ApplyFromQr(QrResolveDto dto)
    {
        NowPlayingName = string.IsNullOrWhiteSpace(dto.NarrationTitle)
            ? dto.Name
            : $"{dto.Name} — {dto.NarrationTitle}";

        var body = !string.IsNullOrWhiteSpace(dto.NarrationContent)
            ? dto.NarrationContent
            : dto.Description;

        AudioUrl = dto.AudioUrl;
        HasAudio = !string.IsNullOrWhiteSpace(dto.AudioUrl);
        AudioHint = HasAudio
            ? "Nhấn NGHE ÂM THANH để phát trong ứng dụng."
            : "Chưa có file âm thanh cho gian hàng này.";
        NowPlayingText = string.IsNullOrWhiteSpace(body)
            ? "Chưa có nội dung mô tả cho quán này trong hệ thống."
            : body;
    }

    [RelayCommand]
    private async Task OpenAudio()
    {
        if (string.IsNullOrWhiteSpace(AudioUrl))
            return;

        try
        {
            await _audio.PlayAsync(AudioUrl);
            AudioHint = "Đang phát trong ứng dụng.";
        }
        catch
        {
            AudioHint = "Không phát được âm thanh. Kiểm tra lại API URL và mạng.";
        }
    }

    [RelayCommand]
    private void Stop()
    {
        _audio.Stop();
        NowPlayingName = "Chưa phát";
        NowPlayingText = "Chọn một quán để nghe thuyết minh";
        AudioUrl = null;
        HasAudio = false;
        AudioHint = "Chưa có file âm thanh cho gian hàng này.";
    }
}

// ═══════════════════════════════════════════════════════
//  ProfileViewModel — Hồ sơ
// ═══════════════════════════════════════════════════════
public partial class ProfileViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IDataService _data;
    private readonly IFavoriteService _favorites;

    [ObservableProperty]
    private int listenCount = 5;

    [ObservableProperty]
    private int favoriteCount;

    [ObservableProperty]
    private string selectedLang = "Tiếng Việt";

    [ObservableProperty]
    private string apiBaseUrl = string.Empty;

    [ObservableProperty]
    private string connectionStatus = string.Empty;

    public ProfileViewModel(ISettingsService settings, IDataService data, IFavoriteService favorites)
    {
        _settings = settings;
        _data = data;
        _favorites = favorites;
        ApiBaseUrl = _settings.ApiBaseUrl;
        FavoriteCount = _favorites.Count;
    }

    public void SyncApiUrlFromSettings()
    {
        ApiBaseUrl = _settings.ApiBaseUrl;
        FavoriteCount = _favorites.Count;
    }

    [RelayCommand]
    private void SaveApiUrl()
    {
        _settings.ApiBaseUrl = ApiBaseUrl;
        ConnectionStatus = "Đã lưu địa chỉ API.";
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        _settings.ApiBaseUrl = ApiBaseUrl;
        try
        {
            var list = await _data.GetPoisAsync();
            ConnectionStatus = $"Kết nối OK — nhận được {list.Count} gian hàng.";
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Lỗi: {ex.Message}";
        }
    }
}