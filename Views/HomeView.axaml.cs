using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ADIapp.Services;

namespace ADIapp.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        UpdateLocalizedText();
        LanguageService.LanguageChanged += OnLanguageChanged;
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        LanguageService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        UpdateLocalizedText();
    }

    private void UpdateLocalizedText()
    {
        if (WelcomeTitleText != null) WelcomeTitleText.Text = LanguageService.Get("Home_Welcome");
        if (WelcomeDescText != null) WelcomeDescText.Text = LanguageService.Get("Home_Desc");
        if (GetStartedBtn != null) GetStartedBtn.Content = LanguageService.Get("Home_GetStarted");
        if (LearnMoreBtn != null) LearnMoreBtn.Content = LanguageService.Get("Home_LearnMore");
    }

    private void GetStarted_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.FindAncestorOfType<MainWindow>();
        window?.Navigate(new TuneView());
    }

    private void Info_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.FindAncestorOfType<MainWindow>();
        window?.Navigate(new InfoView());
    }
}
