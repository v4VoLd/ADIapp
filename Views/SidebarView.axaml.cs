using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ADIapp.Views;

public partial class SidebarView : UserControl
{
    private Button? _selectedButton;

    public SidebarView()
    {
        InitializeComponent();
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
