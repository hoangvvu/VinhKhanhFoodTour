using VKFoodTour.Mobile.ViewModels;

namespace VKFoodTour.Mobile.Views;

[QueryProperty(nameof(PoiId), "poiId")]
public partial class StallDetailPage : ContentPage
{
    private readonly StallDetailViewModel _vm;
    private int _poiId;

    public StallDetailPage(StallDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public string PoiId
    {
        set => int.TryParse(value, out _poiId);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_poiId > 0)
            await _vm.LoadAsync(_poiId);
    }
}
