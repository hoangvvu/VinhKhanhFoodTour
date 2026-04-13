using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VKFoodTour.Mobile.Services;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.ViewModels;

public partial class StallDetailViewModel : ObservableObject
{
    private readonly IDataService _dataService;

    [ObservableProperty] private int poiId;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string address = string.Empty;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string? coverImageUrl;
    [ObservableProperty] private List<ImageItemDto> galleryImages = new();
    [ObservableProperty] private List<MenuItemDto> menuItems = new();
    [ObservableProperty] private List<AudioItemDto> audioItems = new();
    [ObservableProperty] private bool isLoading;

    public StallDetailViewModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task LoadAsync(int id)
    {
        if (id <= 0)
            return;

        IsLoading = true;
        try
        {
            var detail = await _dataService.GetPoiDetailAsync(id);
            if (detail is null)
                return;

            PoiId = detail.PoiId;
            Name = detail.Name;
            Address = detail.Address ?? string.Empty;
            Description = detail.Description ?? string.Empty;
            CoverImageUrl = detail.CoverImageUrl;
            GalleryImages = detail.GalleryImages;
            MenuItems = detail.MenuItems;
            AudioItems = detail.AudioItems;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenAudioAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;
        await Launcher.OpenAsync(url);
    }
}
