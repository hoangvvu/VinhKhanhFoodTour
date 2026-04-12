using VKFoodTour.Mobile.ViewModels;
using ZXing.Net.Maui;

namespace VKFoodTour.Mobile.Views;

public partial class QrScanPage : ContentPage
{
    private readonly QrScanViewModel _viewModel;

    public QrScanPage(QrScanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.ShowCameraScanner)
            BarcodeReader.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        BarcodeReader.IsDetecting = false;
        base.OnDisappearing();
    }

    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (e.Results is null || e.Results.Length == 0)
            return;

        var text = e.Results[0].Value;
        if (string.IsNullOrWhiteSpace(text))
            return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            BarcodeReader.IsDetecting = false;
            try
            {
                await _viewModel.HandleScanPayloadAsync(text);
            }
            finally
            {
                if (_viewModel.ShowCameraScanner)
                    BarcodeReader.IsDetecting = true;
            }
        });
    }
}
