using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ADIapp.Services;
using ADIapp.Models;
using ADIapp.Helpers;

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

        // Restore remembered email and remember-me checkbox
        try
        {
            var emailBox = this.FindControl<TextBox>("EmailBox");
            var rememberMeCheck = this.FindControl<CheckBox>("RememberMeCheckBox");
            string? rememberedEmail = SecureStorageHelper.GetRememberedEmail();

            if (!string.IsNullOrWhiteSpace(rememberedEmail))
            {
                if (emailBox != null) emailBox.Text = rememberedEmail;
                if (rememberMeCheck != null) rememberMeCheck.IsChecked = true;
            }
            else if (SecureStorageHelper.HasSavedSession())
            {
                if (rememberMeCheck != null) rememberMeCheck.IsChecked = true;
            }
        }
        catch { }
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
        var rememberMeLbl = this.FindControl<TextBlock>("RememberMeText");
        var btn = this.FindControl<Button>("LoginButton");
        var signUpBtn = this.FindControl<Button>("SignUpButton");
        var emailBox = this.FindControl<TextBox>("EmailBox");
        var passwordBox = this.FindControl<TextBox>("PasswordBox");

        if (subTitleLbl != null) subTitleLbl.Text = LanguageService.Get("Login_Subtitle");
        if (userLbl != null) userLbl.Text = LanguageService.Get("Login_Username");
        if (passLbl != null) passLbl.Text = LanguageService.Get("Login_Password");
        if (rememberMeLbl != null) rememberMeLbl.Text = LanguageService.Get("Login_RememberMe");
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
        var rememberMeCheck = this.FindControl<CheckBox>("RememberMeCheckBox");
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
        bool rememberMe = rememberMeCheck?.IsChecked ?? false;

        if (string.IsNullOrWhiteSpace(email))
        {
            errorLabel.Text = LanguageService.Get("Login_UsernameRequired");
            errorLabel.IsVisible = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            errorLabel.Text = LanguageService.Get("Login_PasswordRequired");
            errorLabel.IsVisible = true;
            return;
        }

        // Set loading UI state
        emailBox.IsEnabled = false;
        passwordBox.IsEnabled = false;
        if (rememberMeCheck != null) rememberMeCheck.IsEnabled = false;
        loginButton.IsEnabled = false;
        loginButton.Content = LanguageService.Get("Login_SigningIn");

        try
        {
            var (success, message, currentUser) = await ApiService.LoginAsync(email, password, rememberMe);

            if (success)
            {
                if (rememberMe)
                {
                    SecureStorageHelper.SaveRememberedEmail(email);
                }
                else
                {
                    SecureStorageHelper.SaveRememberedEmail(string.Empty);
                }

                var window = this.FindAncestorOfType<MainWindow>();
                window?.Navigate(new HomeView());
                if (currentUser != null)
                {
                    try
                    {
                        await WebSocketManager.InitializeAsync(currentUser.Id);
                    }
                    catch (Exception wsEx)
                    {
                        Logger.Error($"WebSocket initialization error: {wsEx.Message}", wsEx);
                    }

                    try
                    {
                        await NotificationService.LoadNotificationsAsync();
                    }
                    catch (Exception notifEx)
                    {
                        Logger.Error($"Notification loading error: {notifEx.Message}", notifEx);
                    }

                    // Check for application updates after login
                    _ = System.Threading.Tasks.Task.Run(() => UpdateService.CheckAndPerformUpdateAsync());
                }
            }
            else
            {
                errorLabel.Text = message;
                errorLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Unexpected error during login: {ex.Message}", ex);
            errorLabel.Text = $"Connection error: {ex.Message}";
            errorLabel.IsVisible = true;
        }
        finally
        {
            // Re-enable controls
            emailBox.IsEnabled = true;
            passwordBox.IsEnabled = true;
            if (rememberMeCheck != null) rememberMeCheck.IsEnabled = true;
            loginButton.IsEnabled = true;
            loginButton.Content = LanguageService.Get("Login_Submit");
        }
    }
}
