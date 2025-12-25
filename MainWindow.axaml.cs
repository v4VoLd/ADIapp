using Avalonia.Controls;

namespace ADIapp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainContent.Content = new Views.LoginView();
    }

    public void Navigate(Control view)
    {
        MainContent.Content = view;
    }
}
