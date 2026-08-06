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
    }

    private void OnOrderUpdated()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => PopulateToken());
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