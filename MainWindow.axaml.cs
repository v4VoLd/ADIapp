using Avalonia;
using Avalonia.Controls;
using ADIapp.Views;

namespace ADIapp;

public partial class MainWindow : Window
{
    private AppShellView? _appShell;

    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        try
        {
            Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new System.Uri("avares://ADIapp/Assets/sidebar_logo.ico")));
        }
        catch { }
        if (Helpers.VmDetector.IsVirtualMachine())
        {
            MainContent.Content = new VmBlockedView();
        }
        else
        {
            MainContent.Content = new LoginView();
        }
    }

    /// <summary>
    /// Navigate to a view.
    /// - LoginView → shown full-screen (login shell), app shell is discarded.
    /// - Any other view → shown inside the persistent AppShellView (app shell).
    /// </summary>
    public void Navigate(Control view)
    {
        if (MainContent.Content is VmBlockedView)
        {
            return;
        }

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
