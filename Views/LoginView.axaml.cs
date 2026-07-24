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
        var subTitleLbl = this.FindControl<TextBlock>("WelcomeSubtitleText");
        var userLbl = this.FindControl<TextBlock>("UsernameLabelText");
        var passLbl = this.FindControl<TextBlock>("PasswordLabelText");
        var btn = this.FindControl<Button>("LoginButton");
        var signUpBtn = this.FindControl<Button>("SignUpButton");
        var emailBox = this.FindControl<TextBox>("EmailBox");
        var passwordBox = this.FindControl<TextBox>("PasswordBox");

        if (subTitleLbl != null) subTitleLbl.Text = LanguageService.Get("Login_Subtitle");
        if (userLbl != null) userLbl.Text = LanguageService.Get("Login_Username");
        if (passLbl != null) passLbl.Text = LanguageService.Get("Login_Password");
        if (btn != null) btn.Content = LanguageService.Get("Login_Submit");
        if (signUpBtn != null) signUpBtn.Content = LanguageService.Get("Login_NoAccount");
        if (emailBox != null) emailBox.Watermark = LanguageService.Get("Login_UsernamePlaceholder");
        if (passwordBox != null) passwordBox.Watermark = LanguageService.Get("Login_PasswordPlaceholder");
    }

    private void SignUp_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://adi-performance.com/register",
                UseShellExecute = true
            });
        }
        catch { }
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
            errorLabel.Text = "Please enter your username or email.";
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
        loginButton.Content = LanguageService.Get("Login_SigningIn");

        try
        {
            var (success, message, currentUser) = await ApiService.LoginAsync(email, password);

            if (success)
            {
                var window = this.FindAncestorOfType<MainWindow>();
                window?.Navigate(new HomeView());
                if (currentUser != null)
                {
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
            loginButton.Content = LanguageService.Get("Login_Submit");
        }
    }
}
