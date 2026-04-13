using System.Text.Json;

namespace VKFoodTour.Mobile.Services;

public sealed class FavoriteService : IFavoriteService
{
    private const string Key = "FavoritePoiIds";
    private HashSet<int> _ids = new();

    public FavoriteService()
    {
        Load();
    }

    public IReadOnlyList<int> AllIds => _ids.OrderBy(x => x).ToList();

    public int Count => _ids.Count;

    public bool IsFavorite(int poiId) => _ids.Contains(poiId);

    public void SetFavorite(int poiId, bool value)
    {
        if (value)
            _ids.Add(poiId);
        else
            _ids.Remove(poiId);
        Save();
    }

    public void Toggle(int poiId)
    {
        if (_ids.Contains(poiId))
            _ids.Remove(poiId);
        else
            _ids.Add(poiId);
        Save();
    }

    private void Load()
    {
        try
        {
            var json = Preferences.Default.Get(Key, "[]");
            var list = JsonSerializer.Deserialize<List<int>>(json);
            _ids = list is null ? new HashSet<int>() : new HashSet<int>(list);
        }
        catch
        {
            _ids = new HashSet<int>();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_ids.OrderBy(x => x).ToList());
        Preferences.Default.Set(Key, json);
    }
}
