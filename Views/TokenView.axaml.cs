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
        
        // Populate with currently cached profile data immediately
        PopulateToken();

        // Fetch fresh profile data from the Laravel API profile controller
        var (success, _) = await ApiService.FetchProfileAsync();
        if (success)
        {
            // Re-populate with updated data from the backend
            PopulateToken();
        }
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