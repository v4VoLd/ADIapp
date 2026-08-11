using Avalonia.Controls;
using ADIapp.Services;

namespace ADIapp.Views;

public partial class SettingsView : UserControl
{
    private bool _isInitializing = true;

    public SettingsView()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _isInitializing = true;

        if (LanguageComboBox != null)
        {
            LanguageComboBox.SelectedIndex = LanguageService.CurrentLanguage == "fr" ? 1 : 0;
        }

        _isInitializing = false;
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
        if (TitleText != null) TitleText.Text = LanguageService.Get("Settings_Title");
        if (SettingsSubtitleText != null) SettingsSubtitleText.Text = LanguageService.Get("Settings_Subtitle");
        if (LanguageLabelText != null) LanguageLabelText.Text = LanguageService.Get("Settings_Language");
    }

    private void Language_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (LanguageComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
        {
            LanguageService.SetLanguage(langCode);
        }
    }
}