using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ADIapp.Services;
using System;

namespace ADIapp.Views;

public partial class SidebarView : UserControl
{
    private Button? _selectedButton;

    public SidebarView()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        UpdateExpirationDate();
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
        if (NavHomeText != null) NavHomeText.Text = LanguageService.Get("Sidebar_Home");
        if (NavSettingsText != null) NavSettingsText.Text = LanguageService.Get("Sidebar_Settings");
        if (NavTicketsText != null) NavTicketsText.Text = LanguageService.Get("Sidebar_Tickets");
        if (NavOrdersText != null) NavOrdersText.Text = LanguageService.Get("Orders_Title");
        if (NavInfoText != null) NavInfoText.Text = LanguageService.Get("Sidebar_Info");
        if (NavLogoutText != null) NavLogoutText.Text = LanguageService.Get("Sidebar_Logout");
        if (LicenseExpTitleText != null) LicenseExpTitleText.Text = LanguageService.Get("Sidebar_LicenseExp");
    }

    private void UpdateExpirationDate()
    {
        if (ApiService.CurrentUser != null)
        {
            var licenseText = this.FindControl<TextBlock>("LicenseExpirationText");
            if (licenseText != null)
            {
                if (DateTime.TryParse(ApiService.CurrentUser.SubscriptionEndDate, out var date))
                {
                    licenseText.Text = date.ToString("dd/MM/yyyy");
                }
                else
                {
                    licenseText.Text = ApiService.CurrentUser.SubscriptionEndDate ?? "No License";
                }
            }
        }
    }

    private MainWindow? Window =>
        this.FindAncestorOfType<MainWindow>();

    private void Select(Button button)
    {
        _selectedButton?.Classes.Remove("selected");
        _selectedButton = button;
        _selectedButton.Classes.Add("selected");
    }

    private void Home_Click(object? s, RoutedEventArgs e)
    {
        Select((Button)s!);
        Window?.Navigate(new HomeView());
    }

    private void Settings_Click(object? s, RoutedEventArgs e)
    {
        Select((Button)s!);
        Window?.Navigate(new SettingsView());
    }

    private void Tickets_Click(object? s, RoutedEventArgs e)
    {
        Select((Button)s!);
        Window?.Navigate(new TicketView());
    }

    private void Orders_Click(object? s, RoutedEventArgs e)
    {
        Select((Button)s!);
        Window?.Navigate(new OrderHistoryView());
    }

    private void Info_Click(object? s, RoutedEventArgs e)
    {
        Select((Button)s!);
        Window?.Navigate(new InfoView());
    }

    private async void Logout_Click(object? s, RoutedEventArgs e)
    {
        var window = Window;
        if (window == null) return;

        var dialog = new ConfirmLogoutDialog();
        var confirm = await dialog.ShowDialog<bool>(window);
        if (confirm)
        {
            Select((Button)s!);
            ApiService.Logout();
            window.Navigate(new LoginView());
        }
    }
}
