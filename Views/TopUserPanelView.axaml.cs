using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using ADIapp.Services;

namespace ADIapp.Views;

public partial class TopUserPanelView : UserControl
{
    public TopUserPanelView()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (ApiService.CurrentUser != null)
        {
            var nameBlock = this.FindControl<TextBlock>("UserNameBlock");
            if (nameBlock != null)
            {
                nameBlock.Text = ApiService.CurrentUser.Name;
            }
            var tokenBlock = this.FindControl<TextBlock>("UserTokenBlock");
            if (tokenBlock != null)
            {
                tokenBlock.Text = $"Token: {ApiService.CurrentUser.AvailableCredit}";
            }
        }
    }

    private MainWindow? Window =>
        this.FindAncestorOfType<MainWindow>();

    // 🔔 Bell click → dummy message
    private async void Bell_Click(object? sender, PointerPressedEventArgs e)
    {
        if (Window != null)
        {
            await MessageBox(Window, "Your credit has been recharged");
        }
    }

    // 👤 User card click → expand / collapse menu
    private void User_Click(object? sender, PointerPressedEventArgs e)
    {
        Menu.IsVisible = !Menu.IsVisible;
    }

    // 👉 Account
    private void Account_Click(object? sender, RoutedEventArgs e)
    {
        Menu.IsVisible = false;
        Window?.Navigate(new AccountView());
    }

    private void Token_Click(object? sender, RoutedEventArgs e)
    {
        Menu.IsVisible = false;
        Window?.Navigate(new TokenView());
    }

    private void Logout_Click(object? sender, RoutedEventArgs e)
    {
        Menu.IsVisible = false;
        Window?.Navigate(new LoginView());
    }


    // Simple message dialog
    private async System.Threading.Tasks.Task MessageBox(Window window, string message)
    {
        var dialog = new Window
        {
            Width = 280,
            Height = 120,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Padding = new Thickness(20),
                Background = Avalonia.Media.Brushes.Black,
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = Avalonia.Media.Brushes.White,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            }
        };

        await dialog.ShowDialog(window);
    }
}
