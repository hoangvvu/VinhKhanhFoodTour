namespace VKFoodTour.Mobile.Views; // Namespace phải khớp 100% với x:Class ở trên

public partial class HomePage : ContentPage
{
    public HomePage(VKFoodTour.Mobile.ViewModels.HomeViewModel viewModel)
    {
        InitializeComponent(); // Dòng này sẽ hết lỗi khi Namespace ở đây khớp với XAML
        BindingContext = viewModel;
    }
}