using VKFoodTour.Mobile.Models;
using VKFoodTour.Mobile.ViewModels;

namespace VKFoodTour.Mobile.Views;

public partial class StallListPage : ContentPage
{
    public StallListPage(StallListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is StallListViewModel vm)
            await vm.LoadPoisCommand.ExecuteAsync(null);
    }

    private async void OnStallSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not CollectionView cv)
            return;

        if (e.CurrentSelection.FirstOrDefault() is not Poi poi)
            return;

        cv.SelectedItem = null;
        await Shell.Current.GoToAsync($"{nameof(StallDetailPage)}?poiId={poi.PoiId}");
    }
}