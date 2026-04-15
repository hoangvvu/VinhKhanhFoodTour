using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VKFoodTour.Mobile.Localization;
using VKFoodTour.Mobile.Models;
using VKFoodTour.Mobile.Services;
using VKFoodTour.Mobile.Views;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IAudioPlaybackService _audio;
    private readonly IFavoriteService _favorites;
    private readonly ISettingsService _settings;
    private readonly ILocalizationService _localization;

    // Các biến dùng [ObservableProperty] PHẢI viết thường chữ cái đầu (camelCase)
    // Thư viện sẽ tự tạo ra bản chữ HOA (Pois, NearestPoi, UiTitle...)

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
    private string uiGreeting = string.Empty;

    [ObservableProperty]
    private string uiTitle = string.Empty;

    [ObservableProperty]
    private string uiAutoNarration = string.Empty;

    [ObservableProperty]
    private string uiFavoritesSection = string.Empty;

    [ObservableProperty]
    private string uiFeaturedSection = string.Empty;

    [ObservableProperty]
    private string uiRecentReviews = string.Empty;

    [ObservableProperty]
    private string langFlagEmoji = "🇻🇳";

    [ObservableProperty]
    private string mapPageTitle = string.Empty;

    [ObservableProperty]
    private string nowPlayingText = string.Empty;

    [ObservableProperty]
    private bool isAutoPlayEnabled = true;

    [ObservableProperty]
    private bool isBusy;

    public HomeViewModel(IDataService dataService, IAudioPlaybackService audio, IFavoriteService favorites, ISettingsService settings, ILocalizationService localization)
    {
        _dataService = dataService;
        _audio = audio;
        _favorites = favorites;
        _settings = settings;
        _localization = localization;

        IsAutoPlayEnabled = _settings.AutoPlayEnabled;

        _localization.LanguageChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(RefreshHomeUiStrings);

        RefreshHomeUiStrings();
        NowPlayingText = _localization.GetString("Home_AutoNarrationFallback");

        _ = LoadDataAsync();
    }

    private void RefreshHomeUiStrings()
    {
        UiGreeting = _localization.GetString("Home_Greeting");
        UiTitle = _localization.GetString("Home_Title");
        UiAutoNarration = _localization.GetString("Home_AutoNarration");
        UiFavoritesSection = _localization.GetString("Home_Favorites");
        UiFeaturedSection = _localization.GetString("Home_Featured");
        UiRecentReviews = _localization.GetString("Home_RecentReviews");
        MapPageTitle = _localization.GetString("Map_Title");
        LangFlagEmoji = _localization.CurrentLanguageCode.ToLowerInvariant() switch
        {
            "en" => "🇬🇧",
            "zh" or "zh-cn" or "zh-tw" => "🇨🇳",
            "ja" => "🇯🇵",
            "ko" => "🇰🇷",
            _ => "🇻🇳"
        };
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
                    NowPlayingText = _localization.GetString("Home_LoadedStallsFmt", Pois.Count);
            }
            else
            {
                NowPlayingText = _localization.GetString("Home_NoStalls");
            }

            var reviews = await _dataService.GetRecentReviewsAsync(25, cancellationToken);
            RecentReviews = new ObservableCollection<ReviewListItemDto>(reviews);
        }
        catch (Exception ex)
        {
            // Ghi log lỗi và thông báo lên UI
            NowPlayingText = _localization.GetString("Home_LoadError");
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
    private void ToggleAutoPlay()
    {
        IsAutoPlayEnabled = !IsAutoPlayEnabled;
        _settings.AutoPlayEnabled = IsAutoPlayEnabled;
        NowPlayingText = IsAutoPlayEnabled
            ? _localization.GetString("Home_AutoOn")
            : _localization.GetString("Home_AutoOff");
    }

    [RelayCommand]
    private async Task PlayCommand()
    {
        if (NearestPoi is null)
            return;

        NowPlayingText = $"{_localization.GetString("Player_Play")} - {NearestPoi.Name}";

        var detail = await _dataService.GetPoiDetailAsync(NearestPoi.PoiId);
        var audioUrl = detail?.AudioItems
            .FirstOrDefault(a => string.Equals(a.SourceType, "auto_nearby", StringComparison.OrdinalIgnoreCase))?.Url
            ?? detail?.AudioItems.FirstOrDefault()?.Url;

        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            NowPlayingText = _localization.GetString("StallDetail_NoAudioFile");
            return;
        }

        var ok = await _audio.PlayAsync(audioUrl);
        if (!ok)
            NowPlayingText = _localization.GetString("StallDetail_PlaybackFail");
    }

    [RelayCommand]
    private void Stop()
    {
        _audio.Stop();
        NowPlayingText = _localization.GetString("Home_AutoNarrationFallback");
    }
}

// ═══════════════════════════════════════════════════════
//  StallListViewModel — Danh sách quán
// ═══════════════════════════════════════════════════════
public partial class StallListViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IFavoriteService _favorites;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private ObservableCollection<Poi> pois = new();

    [ObservableProperty]
    private string uiPageTitle = string.Empty;

    [ObservableProperty]
    private string uiPriorityLabel = string.Empty;

    public StallListViewModel(IDataService dataService, IFavoriteService favorites, ILocalizationService localization)
    {
        _dataService = dataService;
        _favorites = favorites;
        _localization = localization;
        _localization.LanguageChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(RefreshStallListUiStrings);
        RefreshStallListUiStrings();
        _ = LoadPoisAsync();
    }

    private void RefreshStallListUiStrings()
    {
        UiPageTitle = _localization.GetString("StallList_Title");
        UiPriorityLabel = _localization.GetString("StallList_Priority");
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
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private string nowPlayingName = string.Empty;

    [ObservableProperty]
    private string nowPlayingText = string.Empty;

    [ObservableProperty]
    private string? audioUrl;

    [ObservableProperty]
    private bool hasAudio;

    [ObservableProperty]
    private string audioHint = string.Empty;

    [ObservableProperty]
    private string uiPlayAudio = string.Empty;

    [ObservableProperty]
    private string uiStop = string.Empty;

    public PlayerViewModel(IAudioPlaybackService audio, ILocalizationService localization)
    {
        _audio = audio;
        _localization = localization;
        _localization.LanguageChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(RefreshPlayerUiStrings);
        RefreshPlayerUiStrings();
        ApplyIdleState();
    }

    private void RefreshPlayerUiStrings()
    {
        UiPlayAudio = _localization.GetString("Player_Play");
        UiStop = _localization.GetString("Player_Stop");
    }

    private void ApplyIdleState()
    {
        NowPlayingName = _localization.GetString("Player_DefaultName");
        NowPlayingText = _localization.GetString("Player_DefaultBody");
        AudioUrl = null;
        HasAudio = false;
        AudioHint = _localization.GetString("Player_NoAudioHint");
    }

    public void ApplyStall(string name)
    {
        NowPlayingName = name;
        NowPlayingText = _localization.GetString("Player_ApplyStallBody");
        AudioUrl = null;
        HasAudio = false;
        AudioHint = _localization.GetString("Player_NoAudioHint");
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
            ? _localization.GetString("Player_PlayHint")
            : _localization.GetString("Player_NoAudioHint");
        NowPlayingText = string.IsNullOrWhiteSpace(body)
            ? _localization.GetString("Player_NoDesc")
            : body;
    }

    [RelayCommand]
    private async Task OpenAudio()
    {
        if (string.IsNullOrWhiteSpace(AudioUrl))
            return;

        var ok = await _audio.PlayAsync(AudioUrl);
        AudioHint = ok
            ? _localization.GetString("Player_PlayingOk")
            : _localization.GetString("Player_PlayFail");
    }

    [RelayCommand]
    private void Stop()
    {
        _audio.Stop();
        ApplyIdleState();
    }
}

// ═══════════════════════════════════════════════════════
//  ProfileViewModel — Hồ sơ
// ═══════════════════════════════════════════════════════
public partial class ProfileViewModel : ObservableObject
{
    private readonly IDataService _data;
    private readonly IFavoriteService _favorites;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private int listenCount = 5;

    [ObservableProperty]
    private int favoriteCount;

    [ObservableProperty]
    private ObservableCollection<LanguagePickerItem> languageOptions = new();

    [ObservableProperty]
    private LanguagePickerItem? selectedLanguageItem;

    [ObservableProperty]
    private string uiTitle = string.Empty;

    [ObservableProperty]
    private string uiTourist = string.Empty;

    [ObservableProperty]
    private string uiIdLabel = string.Empty;

    [ObservableProperty]
    private string uiListened = string.Empty;

    [ObservableProperty]
    private string uiFavorites = string.Empty;

    [ObservableProperty]
    private string uiLogout = string.Empty;

    [ObservableProperty]
    private string uiLanguageLabel = string.Empty;

    public ProfileViewModel(IDataService data, IFavoriteService favorites, ILocalizationService localization)
    {
        _data = data;
        _favorites = favorites;
        _localization = localization;
        _localization.LanguageChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(RefreshProfileUiStrings);
        FavoriteCount = _favorites.Count;
        RefreshProfileUiStrings();
    }

    partial void OnSelectedLanguageItemChanged(LanguagePickerItem? value)
    {
        if (value is null)
            return;
        _localization.SetLanguageCode(value.Code);
    }

    public async Task LoadLanguageOptionsAsync(CancellationToken cancellationToken = default)
    {
        var list = await _data.GetLanguagesAsync(cancellationToken);
        var items = list
            .Where(l => TranslationStrings.SupportsLanguage(l.Code))
            .Select(l => new LanguagePickerItem(l.Code, string.IsNullOrWhiteSpace(l.Name) ? l.Code : l.Name))
            .ToList();

        var cur = _localization.CurrentLanguageCode;
        if (!items.Any(i => i.Code.Equals(cur, StringComparison.OrdinalIgnoreCase)))
            items.Insert(0, new LanguagePickerItem("vi", "Tiếng Việt"));

        LanguageOptions = new ObservableCollection<LanguagePickerItem>(items);

        SelectedLanguageItem = items.FirstOrDefault(i => i.Code.Equals(cur, StringComparison.OrdinalIgnoreCase))
                               ?? items[0];
    }

    public void SyncApiUrlFromSettings()
    {
        FavoriteCount = _favorites.Count;
    }

    private void RefreshProfileUiStrings()
    {
        UiTitle = _localization.GetString("Profile_Title");
        UiTourist = _localization.GetString("Profile_Tourist");
        UiIdLabel = _localization.GetString("Profile_IdLabel");
        UiListened = _localization.GetString("Profile_Listened");
        UiFavorites = _localization.GetString("Profile_Favorites");
        UiLogout = _localization.GetString("Profile_Logout");
        UiLanguageLabel = _localization.GetString("Profile_Language");
    }
}

/// <summary>Mục trong Picker chọn ngôn ngữ giao diện.</summary>
public sealed class LanguagePickerItem
{
    public LanguagePickerItem(string code, string displayName)
    {
        Code = code;
        DisplayName = displayName;
    }

    public string Code { get; }
    public string DisplayName { get; }
}