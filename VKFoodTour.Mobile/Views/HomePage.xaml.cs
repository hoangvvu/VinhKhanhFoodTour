using VKFoodTour.Mobile.ViewModels;

namespace VKFoodTour.Mobile.Views;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is HomeViewModel vm)
            await vm.LoadDataCommand.ExecuteAsync(null);
    }
}
