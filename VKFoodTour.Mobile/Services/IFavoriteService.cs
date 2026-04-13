namespace VKFoodTour.Mobile.Services;

public interface IFavoriteService
{
    bool IsFavorite(int poiId);
    void SetFavorite(int poiId, bool value);
    void Toggle(int poiId);
    IReadOnlyList<int> AllIds { get; }
    int Count { get; }
}
