using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ADIapp.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }
    private void GetStarted_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.FindAncestorOfType<MainWindow>();
        window?.Navigate(new TuneView());
    }
    private void Info_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var window = this.FindAncestorOfType<MainWindow>();
        window?.Navigate(new InfoView());
    }

}
