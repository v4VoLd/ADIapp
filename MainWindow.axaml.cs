using Avalonia.Controls;
using ADIapp.Views;

namespace ADIapp;

public partial class MainWindow : Window
{
    private AppShellView? _appShell;

    public MainWindow()
    {
        InitializeComponent();
        MainContent.Content = new LoginView();
    }

    /// <summary>
    /// Navigate to a view.
    /// - LoginView → shown full-screen (login shell), app shell is discarded.
    /// - Any other view → shown inside the persistent AppShellView (app shell).
    /// </summary>
    public void Navigate(Control view)
    {
        if (view is LoginView)
        {
            // Tear down the app shell and show the login shell full-screen
            _appShell = null;
            MainContent.Content = view;
        }
        else
        {
            // Lazily create the app shell once and keep it alive across page changes
            if (_appShell == null || MainContent.Content is not AppShellView)
            {
                _appShell = new AppShellView();
                MainContent.Content = _appShell;
            }

            _appShell.NavigatePage(view);
        }
    }
}
