using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ADIapp.Services;
using ADIapp.Models;

namespace ADIapp.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private async void Login_Click(object? sender, RoutedEventArgs e)
    {
        var emailBox = this.FindControl<TextBox>("EmailBox");
        var passwordBox = this.FindControl<TextBox>("PasswordBox");
        var errorLabel = this.FindControl<TextBlock>("ErrorLabel");
        var loginButton = this.FindControl<Button>("LoginButton");

        if (emailBox == null || passwordBox == null || errorLabel == null || loginButton == null)
        {
            var window = this.FindAncestorOfType<MainWindow>();
            window?.Navigate(new HomeView());
            return;
        }

        errorLabel.IsVisible = false;

        string email = emailBox.Text ?? string.Empty;
        string password = passwordBox.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            errorLabel.Text = "Please enter your username/email.";
            errorLabel.IsVisible = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            errorLabel.Text = "Please enter your password.";
            errorLabel.IsVisible = true;
            return;
        }

        // Set loading UI state
        emailBox.IsEnabled = false;
        passwordBox.IsEnabled = false;
        loginButton.IsEnabled = false;
        loginButton.Content = "Logging in...";

        try
        {
            var (success, message, currentUser) = await ApiService.LoginAsync(email, password);

            if (success)
            {
                var window = this.FindAncestorOfType<MainWindow>();
                window?.Navigate(new HomeView());
                if(currentUser != null) {
                    await WebSocketManager.InitializeAsync(currentUser.Id);
                    await NotificationService.LoadNotificationsAsync();
                }
                
            }
            else
            {
                errorLabel.Text = message;
                errorLabel.IsVisible = true;
            }
        }
        finally
        {
            // Re-enable controls
            emailBox.IsEnabled = true;
            passwordBox.IsEnabled = true;
            loginButton.IsEnabled = true;
            loginButton.Content = "Login";
        }
    }
}


