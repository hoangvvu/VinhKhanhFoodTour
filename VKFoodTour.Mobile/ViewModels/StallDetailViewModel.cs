using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VKFoodTour.Mobile.Services;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.ViewModels;

public partial class StallDetailViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IAudioPlaybackService _audio;
    private readonly IFavoriteService _favorites;

    [ObservableProperty] private int poiId;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string address = string.Empty;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string? coverImageUrl;
    [ObservableProperty] private string coverEmoji = "🍜";
    [ObservableProperty] private List<ImageItemDto> galleryImages = new();
    [ObservableProperty] private List<MenuItemDto> menuItems = new();
    [ObservableProperty] private List<AudioItemDto> audioItems = new();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteIcon))]
    private bool isFavorite;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private ObservableCollection<ReviewListItemDto> poiReviews = new();
    [ObservableProperty] private double newRating = 5;
    [ObservableProperty] private string newComment = string.Empty;
    [ObservableProperty] private string reviewStatus = string.Empty;

    public string FavoriteIcon => IsFavorite ? "♥" : "♡";

    public StallDetailViewModel(IDataService dataService, IAudioPlaybackService audio, IFavoriteService favorites)
    {
        _dataService = dataService;
        _audio = audio;
        _favorites = favorites;
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
            CoverEmoji = StallEmoji(detail.Name);
            GalleryImages = detail.GalleryImages;
            MenuItems = detail.MenuItems;
            AudioItems = detail.AudioItems;
            IsFavorite = _favorites.IsFavorite(PoiId);

            var reviews = await _dataService.GetPoiReviewsAsync(id);
            PoiReviews = new ObservableCollection<ReviewListItemDto>(reviews);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleFavorite()
    {
        _favorites.Toggle(PoiId);
        IsFavorite = _favorites.IsFavorite(PoiId);
    }

    [RelayCommand]
    private async Task SubmitReviewAsync()
    {
        if (PoiId <= 0)
            return;

        var stars = (byte)Math.Clamp((int)Math.Round(NewRating), 1, 5);

        ReviewStatus = "Đang gửi…";
        var dto = new CreateReviewDto
        {
            DeviceId = _dataService.DeviceId,
            PoiId = PoiId,
            Rating = stars,
            Comment = string.IsNullOrWhiteSpace(NewComment) ? null : NewComment.Trim(),
            LanguageCode = "vi"
        };

        var created = await _dataService.PostReviewAsync(dto);
        if (created is null)
        {
            ReviewStatus = "Không gửi được. Kiểm tra API.";
            return;
        }

        PoiReviews.Insert(0, created);
        NewComment = string.Empty;
        ReviewStatus = "Đã đăng bình luận.";
    }

    [RelayCommand]
    private async Task OpenAudioAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;
        await _dataService.TrackEventAsync(PoiId, "listen_start");
        await _audio.PlayAsync(url);
    }

    public async Task PlayFirstAudioIfAnyAsync()
    {
        var first = AudioItems
            .FirstOrDefault(a => string.Equals(a.SourceType, "auto_nearby", StringComparison.OrdinalIgnoreCase))
            ?? AudioItems.FirstOrDefault();
        if (first is null || string.IsNullOrWhiteSpace(first.Url))
            return;
        await _dataService.TrackEventAsync(PoiId, "listen_start");
        await _audio.PlayAsync(first.Url);
    }

    public async Task PlayPreferredAudioFromQrAsync(string? qrAudioUrl)
    {
        if (!string.IsNullOrWhiteSpace(qrAudioUrl))
        {
            await _dataService.TrackEventAsync(PoiId, "listen_start");
            await _audio.PlayAsync(qrAudioUrl);
            return;
        }

        await PlayFirstAudioIfAnyAsync();
    }

    private static string StallEmoji(string name)
    {
        var h = name.Aggregate(0, (a, c) => a + c);
        var emojis = new[] { "🍜", "🦪", "🍢", "🥟", "🍲", "🧋", "🍡", "🥘" };
        return emojis[Math.Abs(h) % emojis.Length];
    }
}
