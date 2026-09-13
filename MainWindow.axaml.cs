using System;
using Avalonia;
using Avalonia.Controls;
using ADIapp.Views;

namespace ADIapp;

public partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    private static System.Threading.Tasks.TaskCompletionSource<bool>? _alertTcs;

    private AppShellView? _appShell;

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        ApplyScreenRelativeSize();
        this.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Escape && GlobalAlertOverlay.IsVisible)
            {
                DismissGlobalAlert();
                e.Handled = true;
            }
        };

        try
        {
            Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new System.Uri("avares://ADIapp/Assets/app_icon.ico")));
        }
        catch (System.Exception ex)
        {
            Helpers.Logger.Error($"Failed to set window icon: {ex.Message}", ex);
        }
        if (Helpers.VmDetector.IsVirtualMachine())
        {
            MainContent.Content = new VmBlockedView();
        }
        else
        {
            if (Helpers.SecureStorageHelper.HasSavedSession())
            {
                // Show clean splash loading screen while verifying saved session (eliminates LoginView flash)
                MainContent.Content = CreateLoadingSplash();
                CheckAutoLoginAsync();
            }
            else
            {
                MainContent.Content = new LoginView();
            }
        }
    }

    private Control CreateLoadingSplash()
    {
        var grid = new Grid
        {
            Background = Avalonia.Media.Brush.Parse("#0D0D11"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };

        var stack = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Spacing = 16
        };

        try
        {
            var logo = new Image
            {
                Source = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.AssetLoader.Open(new System.Uri("avares://ADIapp/Assets/sidebar_logo.png"))),
                Width = 200,
                Height = 100,
                Stretch = Avalonia.Media.Stretch.Uniform
            };
            stack.Children.Add(logo);
        }
        catch { }

        grid.Children.Add(stack);
        return grid;
    }

    private async void CheckAutoLoginAsync()
    {
        try
        {
            var (success, user) = await Services.ApiService.TryAutoLoginAsync();
            if (success && user != null)
            {
                Navigate(new HomeView());

                try
                {
                    await Services.WebSocketManager.InitializeAsync(user.Id);
                }
                catch (System.Exception wsEx)
                {
                    Helpers.Logger.Error($"WebSocket initialization error: {wsEx.Message}", wsEx);
                }

                try
                {
                    await Services.NotificationService.LoadNotificationsAsync();
                }
                catch (System.Exception notifEx)
                {
                    Helpers.Logger.Error($"Notification loading error: {notifEx.Message}", notifEx);
                }

                // Check for application updates after auto-login
                _ = System.Threading.Tasks.Task.Run(() => Services.UpdateService.CheckAndPerformUpdateAsync());
            }
            else
            {
                // Auto-login failed / session expired: show login view
                MainContent.Content = new LoginView();
            }
        }
        catch (System.Exception ex)
        {
            Helpers.Logger.Error($"Auto-login startup check failed: {ex.Message}", ex);
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

    private void GlobalAlertOverlay_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        DismissGlobalAlert();
    }

    private void GlobalAlertCard_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    private void GlobalAlertDismiss_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DismissGlobalAlert();
    }

    public void DismissGlobalAlert()
    {
        var overlay = this.FindControl<Border>("GlobalAlertOverlay");
        if (overlay != null)
        {
            overlay.IsVisible = false;
        }

        _alertTcs?.TrySetResult(true);
        _alertTcs = null;
    }

    public static async System.Threading.Tasks.Task ShowGlobalAlertAsync(string message, string? title = null, string type = "info")
    {
        if (Instance == null) return;

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
        {
            _alertTcs?.TrySetResult(false);
            _alertTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

            string icon = "ℹ️";
            string color = "#38BDF8";
            string defaultTitle = Services.LanguageService.Get("Dialog_Notice");

            switch (type?.ToLowerInvariant())
            {
                case "warning":
                    icon = "⚠️";
                    color = "#F59E0B";
                    defaultTitle = "Warning";
                    break;
                case "error":
                case "failed":
                    icon = "❌";
                    color = "#EF4444";
                    defaultTitle = "Error";
                    break;
                case "success":
                    icon = "✅";
                    color = "#22C55E";
                    defaultTitle = "Success";
                    break;
            }

            var iconBlock = Instance.FindControl<TextBlock>("GlobalAlertIcon");
            var titleBlock = Instance.FindControl<TextBlock>("GlobalAlertTitle");
            var msgBlock = Instance.FindControl<TextBlock>("GlobalAlertMessage");
            var overlay = Instance.FindControl<Border>("GlobalAlertOverlay");

            if (iconBlock != null) iconBlock.Text = icon;
            if (titleBlock != null)
            {
                titleBlock.Text = !string.IsNullOrWhiteSpace(title) ? title : defaultTitle;
                titleBlock.Foreground = Avalonia.Media.Brush.Parse(color);
            }
            if (msgBlock != null) msgBlock.Text = message;
            if (overlay != null) overlay.IsVisible = true;

            await _alertTcs.Task;
        });
    }

    private void ApplyScreenRelativeSize()
    {
        try
        {
            var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
            if (screen != null)
            {
                double workingWidth = screen.WorkingArea.Width / screen.Scaling;
                double workingHeight = screen.WorkingArea.Height / screen.Scaling;

                if (workingWidth > 0 && workingHeight > 0)
                {
                    // Adjust minimum bounds if the monitor working area is smaller than the default 1200x700
                    if (workingWidth < MinWidth)
                    {
                        MinWidth = Math.Min(1000, workingWidth * 0.95);
                    }
                    if (workingHeight < MinHeight)
                    {
                        MinHeight = Math.Min(600, workingHeight * 0.95);
                    }

                    // Set default window size to 85% of available screen working area
                    Width = Math.Clamp(workingWidth * 0.85, MinWidth, workingWidth);
                    Height = Math.Clamp(workingHeight * 0.85, MinHeight, workingHeight);
                }
            }
        }
        catch (System.Exception ex)
        {
            Helpers.Logger.Error($"Failed to adjust window size relative to screen: {ex.Message}", ex);
        }
    }
}

