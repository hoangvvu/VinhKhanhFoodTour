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
    private readonly ILocalizationService _localization;

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

    /// <summary>Thông báo lỗi phát audio (QR / danh sách).</summary>
    [ObservableProperty] private string playbackStatus = string.Empty;

    [ObservableProperty] private string uiPageTitle = string.Empty;
    [ObservableProperty] private string uiGallery = string.Empty;
    [ObservableProperty] private string uiMenu = string.Empty;
    [ObservableProperty] private string uiAudio = string.Empty;
    [ObservableProperty] private string uiListen = string.Empty;
    [ObservableProperty] private string uiReviews = string.Empty;
    [ObservableProperty] private string uiAddReview = string.Empty;
    [ObservableProperty] private string uiReviewPlaceholder = string.Empty;
    [ObservableProperty] private string uiSubmitReview = string.Empty;
    [ObservableProperty] private string ratingLabel = string.Empty;

    public string FavoriteIcon => IsFavorite ? "♥" : "♡";

    public StallDetailViewModel(IDataService dataService, IAudioPlaybackService audio, IFavoriteService favorites, ILocalizationService localization)
    {
        _dataService = dataService;
        _audio = audio;
        _favorites = favorites;
        _localization = localization;
        _localization.LanguageChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(RefreshStallDetailUiStrings);
        RefreshStallDetailUiStrings();
    }

    private void RefreshStallDetailUiStrings()
    {
        UiPageTitle = _localization.GetString("StallDetail_PageTitle");
        UiGallery = _localization.GetString("StallDetail_Gallery");
        UiMenu = _localization.GetString("StallDetail_Menu");
        UiAudio = _localization.GetString("StallDetail_Audio");
        UiListen = _localization.GetString("StallDetail_Listen");
        UiReviews = _localization.GetString("StallDetail_Reviews");
        UiAddReview = _localization.GetString("StallDetail_AddReview");
        UiReviewPlaceholder = _localization.GetString("StallDetail_ReviewPlaceholder");
        UiSubmitReview = _localization.GetString("StallDetail_SubmitReview");
        UpdateRatingLabel();
    }

    partial void OnNewRatingChanged(double value) => UpdateRatingLabel();

    private void UpdateRatingLabel() =>
        RatingLabel = _localization.GetString("StallDetail_RatingFmt", NewRating);

    public async Task LoadAsync(int id)
    {
        if (id <= 0)
            return;

        IsLoading = true;
        PlaybackStatus = string.Empty;
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

        ReviewStatus = _localization.GetString("StallDetail_ReviewSending");
        var dto = new CreateReviewDto
        {
            DeviceId = _dataService.DeviceId,
            PoiId = PoiId,
            Rating = stars,
            Comment = string.IsNullOrWhiteSpace(NewComment) ? null : NewComment.Trim(),
            LanguageCode = _localization.CurrentLanguageCode
        };

        var created = await _dataService.PostReviewAsync(dto);
        if (created is null)
        {
            ReviewStatus = _localization.GetString("StallDetail_ReviewFail");
            return;
        }

        PoiReviews.Insert(0, created);
        NewComment = string.Empty;
        ReviewStatus = _localization.GetString("StallDetail_ReviewOk");
    }

    [RelayCommand]
    private async Task OpenAudioAsync(string? url)
    {
        PlaybackStatus = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
            return;
        var ok = await _audio.PlayAsync(url);
        if (ok)
            await _dataService.TrackEventAsync(PoiId, "listen_start");
        else
            PlaybackStatus = _localization.GetString("StallDetail_PlaybackFail");
    }

    public async Task PlayFirstAudioIfAnyAsync()
    {
        PlaybackStatus = string.Empty;
        var first = AudioItems
            .FirstOrDefault(a => string.Equals(a.SourceType, "auto_nearby", StringComparison.OrdinalIgnoreCase))
            ?? AudioItems.FirstOrDefault();
        if (first is null || string.IsNullOrWhiteSpace(first.Url))
        {
            PlaybackStatus = _localization.GetString("StallDetail_NoAudioFile");
            return;
        }

        var ok = await _audio.PlayAsync(first.Url);
        if (ok)
            await _dataService.TrackEventAsync(PoiId, "listen_start");
        else
            PlaybackStatus = _localization.GetString("StallDetail_AutoPlayFail");
    }

    public async Task PlayPreferredAudioFromQrAsync(string? qrAudioUrl)
    {
        PlaybackStatus = string.Empty;
        if (!string.IsNullOrWhiteSpace(qrAudioUrl))
        {
            var ok = await _audio.PlayAsync(qrAudioUrl);
            if (ok)
                await _dataService.TrackEventAsync(PoiId, "listen_start");
            else
                PlaybackStatus = _localization.GetString("StallDetail_QrAudioFail");
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
