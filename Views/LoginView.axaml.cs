using Avalonia.Controls;
using Avalonia.VisualTree;

namespace ADIapp.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void Login_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var window = this.FindAncestorOfType<MainWindow>();
        window?.Navigate(new HomeView());
    }
}


