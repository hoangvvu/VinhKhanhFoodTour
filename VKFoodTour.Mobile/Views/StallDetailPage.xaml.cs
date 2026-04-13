using VKFoodTour.Mobile.ViewModels;
using VKFoodTour.Mobile.Services;

namespace VKFoodTour.Mobile.Views;

[QueryProperty(nameof(PoiId), "poiId")]
[QueryProperty(nameof(FromQr), "fromQr")]
public partial class StallDetailPage : ContentPage
{
    private readonly StallDetailViewModel _vm;
    private readonly IStallNarrationState _stallState;
    private int _poiId;
    private bool _fromQr;

    public StallDetailPage(StallDetailViewModel vm, IStallNarrationState stallState)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _stallState = stallState;
    }

    public string PoiId
    {
        set => int.TryParse(value, out _poiId);
    }

    public string FromQr
    {
        set => _fromQr = bool.TryParse(value, out var b) && b;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_poiId > 0)
            await _vm.LoadAsync(_poiId);

        if (_fromQr)
        {
            _fromQr = false;
            var fromQr = _stallState.Consume();
            var qrAudio = fromQr?.PoiId == _poiId ? fromQr.AudioUrl : null;
            await _vm.PlayPreferredAudioFromQrAsync(qrAudio);
        }
    }
}
