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

    private void Info_Click(object? s, RoutedEventArgs e)
    {
        Select((Button)s!);
        Window?.Navigate(new InfoView());
    }

    private void Logout_Click(object? s, RoutedEventArgs e)
    {
        Select((Button)s!);
        Window?.Navigate(new LoginView());
    }
}
