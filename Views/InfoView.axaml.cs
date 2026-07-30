
using Avalonia.Controls;
using ADIapp.Services;

namespace ADIapp.Views;

public partial class InfoView : UserControl
{
    public InfoView()
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
        var title = this.FindControl<TextBlock>("InfoTitleBlock");
        if (title != null) title.Text = LanguageService.Get("Info_Title");

        var subtitle = this.FindControl<TextBlock>("InfoSubtitleBlock");
        if (subtitle != null) subtitle.Text = LanguageService.Get("Info_Subtitle");

        var website = this.FindControl<TextBlock>("WebsiteTileBlock");
        if (website != null) website.Text = LanguageService.Get("Info_Website");

        var updates = this.FindControl<TextBlock>("UpdatesTileBlock");
        if (updates != null) updates.Text = LanguageService.Get("Info_Updates");

        var news = this.FindControl<TextBlock>("NewsTileBlock");
        if (news != null) news.Text = LanguageService.Get("Info_News");

        var phone = this.FindControl<TextBlock>("PhoneTileBlock");
        if (phone != null) phone.Text = LanguageService.Get("Info_Phone");
    }
}