using Avalonia.Controls;
using ADIapp.Services;
using ADIapp.Models;

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
        ApiService.CurrentUserChanged += OnCurrentUserChanged;
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
        ApiService.CurrentUserChanged -= OnCurrentUserChanged;
        LanguageService.LanguageChanged -= OnLanguageChanged;
    }

    private void OnCurrentUserChanged(UserDto? user)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => PopulateToken());
    }

    private async void OnOrderUpdated()
    {
        await ApiService.FetchProfileAsync();
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

        var availDesc = this.FindControl<TextBlock>("AvailableDescBlock");
        if (availDesc != null) availDesc.Text = LanguageService.Get("Token_AvailableDesc");

        var reservedLabel = this.FindControl<TextBlock>("ReservedBalanceLabelBlock");
        if (reservedLabel != null) reservedLabel.Text = LanguageService.Get("Token_ReservedBalance");

        var reservedDesc = this.FindControl<TextBlock>("ReservedDescBlock");
        if (reservedDesc != null) reservedDesc.Text = LanguageService.Get("Token_ReservedDesc");

        var purchaseBtnText = this.FindControl<TextBlock>("PurchaseCreditsBtnText");
        if (purchaseBtnText != null) purchaseBtnText.Text = LanguageService.Get("Token_PurchaseCredits");

        PopulateToken();
    }

    private void PurchaseCredits_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = $"{ADIapp.Config.AppConfig.BaseHost}pricing",
                UseShellExecute = true
            });
        }
        catch (System.Exception ex)
        {
            Helpers.Logger.Error($"Failed to open pricing URL: {ex.Message}");
        }
    }

    private void PopulateToken()
    {
        if (ApiService.CurrentUser != null)
        {
            string unit = LanguageService.Get("Token_Unit");

            var tokenText = this.FindControl<TextBlock>("TokenText");
            if (tokenText != null)
            {
                tokenText.Text = $"{ApiService.CurrentUser.AvailableCredit} {unit}";
            }

            var reservedTokenText = this.FindControl<TextBlock>("ReservedTokenText");
            if (reservedTokenText != null)
            {
                reservedTokenText.Text = $"{ApiService.CurrentUser.ReservedCredit} {unit}";
            }
        }
    }
}