using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VKFoodTour.Mobile.Models;
using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.ViewModels;

// ═══════════════════════════════════════════════════════
//  HomeViewModel — Logic Trang chủ
// ═══════════════════════════════════════════════════════
public partial class HomeViewModel : ObservableObject
{
    private readonly IDataService _dataService;

    [ObservableProperty] private ObservableCollection<Poi> pois = new();
    [ObservableProperty] private Poi? nearestPoi;
    [ObservableProperty] private bool isTracking = false;
    [ObservableProperty] private string nowPlayingText = string.Empty;

    public HomeViewModel(IDataService dataService)
    {
        _dataService = dataService;
        LoadDataCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        var result = await _dataService.GetPoisAsync();
        Pois = new ObservableCollection<Poi>(result);
    }

    [RelayCommand]
    private void Stop() => NowPlayingText = string.Empty;
}

// ═══════════════════════════════════════════════════════
//  StallListViewModel — Danh sách quán
// ═══════════════════════════════════════════════════════
public partial class StallListViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    [ObservableProperty] private ObservableCollection<Poi> pois = new();

    public StallListViewModel(IDataService dataService)
    {
        _dataService = dataService;
        LoadPoisCommand.Execute(null);
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
    [ObservableProperty] private string nowPlayingName = "Chưa phát";
    [ObservableProperty] private string nowPlayingText = "Chọn một quán để nghe thuyết minh";
    [ObservableProperty] private string selectedLang = "vi";

    [RelayCommand]
    private void Stop() { /* Logic dừng audio */ }
}

// ═══════════════════════════════════════════════════════
//  ProfileViewModel — Hồ sơ
// ═══════════════════════════════════════════════════════
public partial class ProfileViewModel : ObservableObject
{
    [ObservableProperty] private int listenCount = 5;
    [ObservableProperty] private int favoriteCount = 2;
    [ObservableProperty] private string selectedLang = "Tiếng Việt";
}