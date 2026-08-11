using Avalonia.Controls;
using ADIapp.Services;

namespace ADIapp.Views;

public partial class TokenView : UserControl
{
    public TokenView()
    {
        InitializeComponent();
    }

    protected override async void OnInitialized()
    {
        base.OnInitialized();
        WebSocketManager.OrderUpdated += OnOrderUpdated;
        UpdateLocalizedText();
        LanguageService.LanguageChanged += OnLanguageChanged;

        // Populate with currently cached profile data immediately
        PopulateToken();

        var (success, _) = await ApiService.FetchProfileAsync();
        if (success)
        {
            PopulateToken();
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        WebSocketManager.OrderUpdated -= OnOrderUpdated;
        LanguageService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnOrderUpdated()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => PopulateToken());
    }

    private void OnLanguageChanged()
    {
        UpdateLocalizedText();
    }

    private void UpdateLocalizedText()
    {
        var title = this.FindControl<TextBlock>("TokenTitleBlock");
        if (title != null) title.Text = LanguageService.Get("Token_Title");

        var subtitle = this.FindControl<TextBlock>("TokenSubtitleBlock");
        if (subtitle != null) subtitle.Text = LanguageService.Get("Token_Subtitle");

        var availLabel = this.FindControl<TextBlock>("AvailableBalanceLabelBlock");
        if (availLabel != null) availLabel.Text = LanguageService.Get("Token_AvailableBalance");
    }

    private void PopulateToken()
    {
        if (ApiService.CurrentUser != null)
        {
            var tokenText = this.FindControl<TextBlock>("TokenText");
            if (tokenText != null)
            {
                tokenText.Text = $"{ApiService.CurrentUser.AvailableCredit} Token";
            }
        }
    }
}