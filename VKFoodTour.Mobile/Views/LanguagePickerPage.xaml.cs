using Microsoft.Maui.Controls.Shapes;
using VKFoodTour.Mobile.Services;
using VKFoodTour.Shared.DTOs;

namespace VKFoodTour.Mobile.Views;

public partial class LanguagePickerPage : ContentPage
{
    private readonly IDataService _dataService;
    private readonly ILocalizationService _localization;
    private readonly ISettingsService _settings;
    private readonly AppShell _shell;
    private string? _selectedCode;

    public LanguagePickerPage(IDataService dataService, ILocalizationService localization, ISettingsService settings, AppShell shell)
    {
        InitializeComponent();
        _dataService = dataService;
        _localization = localization;
        _settings = settings;
        _shell = shell;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLanguagesAsync();
    }

    private async Task LoadLanguagesAsync()
    {
        Loader.IsVisible = true;
        Loader.IsRunning = true;
        LanguageListContainer.Clear();

        try
        {
            var langs = await _dataService.GetLanguagesAsync();
            if (langs == null || langs.Count == 0)
            {
                langs = GetFallbackLanguages();
            }

            foreach (var lang in langs)
            {
                var btn = CreateLanguageButton(lang);
                LanguageListContainer.Add(btn);
            }
        }
        catch
        {
            var langs = GetFallbackLanguages();
            foreach (var lang in langs)
            {
                var btn = CreateLanguageButton(lang);
                LanguageListContainer.Add(btn);
            }
        }
        finally
        {
            Loader.IsVisible = false;
            Loader.IsRunning = false;
        }
    }

    private View CreateLanguageButton(LanguageListItemDto lang)
    {
        var border = new Border
        {
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(Color.FromArgb("#2A1A1A")),
            BackgroundColor = Color.FromArgb("#1E1212"),
            Padding = new Thickness(20, 15),
            StrokeShape = new RoundRectangle { CornerRadius = 12 }
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 15 };
        
        var flagLabel = new Label { Text = GetFlag(lang.Code), FontSize = 24, VerticalOptions = LayoutOptions.Center };
        var nameLabel = new Label { Text = lang.Name, TextColor = Color.FromArgb("#F0EBE3"), FontSize = 16, VerticalOptions = LayoutOptions.Center };
        var checkLabel = new Label { Text = "✓", TextColor = Colors.Transparent, FontSize = 20, VerticalOptions = LayoutOptions.Center };

        grid.Add(flagLabel, 0);
        grid.Add(nameLabel, 1);
        grid.Add(checkLabel, 2);

        border.Content = grid;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) => {
            _selectedCode = lang.Code;
            UpdateSelectionUI(border);
            ContinueButton.IsEnabled = true;
        };
        border.GestureRecognizers.Add(tap);

        return border;
    }

    private void UpdateSelectionUI(Border selectedBorder)
    {
        foreach (var child in LanguageListContainer.Children)
        {
            if (child is Border b)
            {
                var g = (Grid)b.Content;
                var check = (Label)g.Children[2];
                
                if (b == selectedBorder)
                {
                    b.Stroke = new SolidColorBrush(Color.FromArgb("#C8372D"));
                    b.BackgroundColor = Color.FromArgb("#2D1515");
                    check.TextColor = Color.FromArgb("#C8372D");
                }
                else
                {
                    b.Stroke = new SolidColorBrush(Color.FromArgb("#2A1A1A"));
                    b.BackgroundColor = Color.FromArgb("#1E1212");
                    check.TextColor = Colors.Transparent;
                }
            }
        }
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedCode)) return;

        ContinueButton.IsEnabled = false;
        _localization.SetLanguageCode(_selectedCode);
        _settings.HasPickedLanguage = true;

        // Transition to main app
        Application.Current!.Windows[0].Page = _shell;
    }

    private static string GetFlag(string code)
    {
        return code.ToLower() switch
        {
            "vi" => "🇻🇳",
            "en" => "🇬🇧",
            "ja" => "🇯🇵",
            "ko" => "🇰🇷",
            "zh" => "🇨🇳",
            _ => "🌐"
        };
    }

    private List<LanguageListItemDto> GetFallbackLanguages()
    {
        return new List<LanguageListItemDto>
        {
            new() { Code = "en", Name = "English" },
            new() { Code = "vi", Name = "Tiếng Việt" },
            new() { Code = "ja", Name = "日本語" },
            new() { Code = "ko", Name = "한국어" },
            new() { Code = "zh", Name = "中文 (简体)" }
        };
    }
}
