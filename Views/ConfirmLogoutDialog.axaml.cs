using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ADIapp.Services;

namespace ADIapp.Views;

public partial class ConfirmLogoutDialog : Window
{
    public ConfirmLogoutDialog()
    {
        InitializeComponent();
        UpdateLocalizedText();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        UpdateLocalizedText();
    }

    private void UpdateLocalizedText()
    {
        Title = LanguageService.Get("Logout_Title");

        var titleText = this.FindControl<TextBlock>("DialogTitleText");
        if (titleText != null) titleText.Text = LanguageService.Get("Logout_Title");

        var msgText = this.FindControl<TextBlock>("DialogMessageText");
        if (msgText != null) msgText.Text = LanguageService.Get("Logout_Message");

        var cancelBtn = this.FindControl<Button>("CancelButton");
        if (cancelBtn != null) cancelBtn.Content = LanguageService.Get("Logout_Cancel");

        var logoutBtn = this.FindControl<Button>("LogoutButton");
        if (logoutBtn != null) logoutBtn.Content = LanguageService.Get("Logout_Confirm");
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Logout_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
