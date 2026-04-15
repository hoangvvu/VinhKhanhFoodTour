using VKFoodTour.Mobile.ViewModels;
using ZXing.Net.Maui;

namespace VKFoodTour.Mobile.Views;

public partial class TourPlayerPage : ContentPage
{
    private readonly TourPlayerViewModel _viewModel;

    public TourPlayerPage(TourPlayerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Bắt đầu tour từ QR token (được gọi từ QrScanPage).
    /// </summary>
    public async Task StartTourFromQrAsync(string? qrToken)
    {
        await _viewModel.StartTourFromQrCommand.ExecuteAsync(qrToken);
    }

    protected override void OnDisappearing()
    {
        if (BarcodeReader is not null)
            BarcodeReader.IsDetecting = false;
        base.OnDisappearing();
        // Don't stop tour when navigating away - keep playing in background
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.ShowCameraScanner && BarcodeReader is not null)
            BarcodeReader.IsDetecting = true;
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
