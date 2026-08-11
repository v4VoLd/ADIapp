using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ADIapp.Services;

namespace ADIapp.Views;

public partial class HowItWorksView : UserControl
{
    public HowItWorksView()
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
        var badge = this.FindControl<TextBlock>("BadgeBlock");
        if (badge != null) badge.Text = LanguageService.Get("Info_Badge");

        var title = this.FindControl<TextBlock>("TitleBlock");
        if (title != null) title.Text = LanguageService.Get("Info_Title");

        var subtitle = this.FindControl<TextBlock>("SubtitleBlock");
        if (subtitle != null) subtitle.Text = LanguageService.Get("Info_Subtitle");

        var s1Title = this.FindControl<TextBlock>("Step1TitleBlock");
        if (s1Title != null) s1Title.Text = LanguageService.Get("Info_Step1_Title");

        var s1Desc = this.FindControl<TextBlock>("Step1DescBlock");
        if (s1Desc != null) s1Desc.Text = LanguageService.Get("Info_Step1_Desc");

        var s2Title = this.FindControl<TextBlock>("Step2TitleBlock");
        if (s2Title != null) s2Title.Text = LanguageService.Get("Info_Step2_Title");

        var s2Desc = this.FindControl<TextBlock>("Step2DescBlock");
        if (s2Desc != null) s2Desc.Text = LanguageService.Get("Info_Step2_Desc");

        var s3Title = this.FindControl<TextBlock>("Step3TitleBlock");
        if (s3Title != null) s3Title.Text = LanguageService.Get("Info_Step3_Title");

        var s3Desc = this.FindControl<TextBlock>("Step3DescBlock");
        if (s3Desc != null) s3Desc.Text = LanguageService.Get("Info_Step3_Desc");

        var s4Title = this.FindControl<TextBlock>("Step4TitleBlock");
        if (s4Title != null) s4Title.Text = LanguageService.Get("Info_Step4_Title");

        var s4Desc = this.FindControl<TextBlock>("Step4DescBlock");
        if (s4Desc != null) s4Desc.Text = LanguageService.Get("Info_Step4_Desc");

        var understandBtn = this.FindControl<Button>("UnderstandBtn");
        if (understandBtn != null) understandBtn.Content = LanguageService.Get("Info_UnderstandBtn");
    }

    private void Understand_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.FindAncestorOfType<MainWindow>();
        window?.Navigate(new HomeView());
    }
}
