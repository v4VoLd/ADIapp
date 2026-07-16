using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Threading;
using System;
using System.Linq;
using ADIapp.Services;
using ADIapp.Models;

namespace ADIapp.Views;

public partial class TopUserPanelView : UserControl
{
    public TopUserPanelView()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (ApiService.CurrentUser != null)
        {
            var nameBlock = this.FindControl<TextBlock>("UserNameBlock");
            if (nameBlock != null)
            {
                nameBlock.Text = ApiService.CurrentUser.Name;
            }
            var tokenBlock = this.FindControl<TextBlock>("UserTokenBlock");
            if (tokenBlock != null)
            {
                tokenBlock.Text = $"Token: {ApiService.CurrentUser.AvailableCredit}";
            }
        }

        NotificationService.NotificationReceived += OnNotificationReceived;
        NotificationService.NotificationsUpdated += OnNotificationsUpdated;
        UpdateNotificationsList();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        NotificationService.NotificationReceived -= OnNotificationReceived;
        NotificationService.NotificationsUpdated -= OnNotificationsUpdated;
    }

    private MainWindow? Window =>
        this.FindAncestorOfType<MainWindow>();

    // 🔔 Bell click → Toggle NotificationMenu and mark notifications as read
    private void Bell_Click(object? sender, PointerPressedEventArgs e)
    {
        var notificationMenu = this.FindControl<Border>("NotificationMenu");
        if (notificationMenu != null)
        {
            notificationMenu.IsVisible = !notificationMenu.IsVisible;
            if (notificationMenu.IsVisible)
            {
                // Collapse user menu if open
                var userMenu = this.FindControl<Border>("Menu");
                if (userMenu != null) userMenu.IsVisible = false;

                // Mark all as read when opening notifications panel
                NotificationService.MarkAllAsRead();
            }
        }
    }

    // 👤 User card click → expand / collapse menu
    private void User_Click(object? sender, PointerPressedEventArgs e)
    {
        Menu.IsVisible = !Menu.IsVisible;
        if (Menu.IsVisible)
        {
            var notificationMenu = this.FindControl<Border>("NotificationMenu");
            if (notificationMenu != null) notificationMenu.IsVisible = false;
        }
    }

    private void ClearNotifications_Click(object? sender, RoutedEventArgs e)
    {
        NotificationService.ClearAll();
    }

    // 👉 Account
    private void Account_Click(object? sender, RoutedEventArgs e)
    {
        Menu.IsVisible = false;
        var notificationMenu = this.FindControl<Border>("NotificationMenu");
        if (notificationMenu != null) notificationMenu.IsVisible = false;
        Window?.Navigate(new AccountView());
    }

    private void Token_Click(object? sender, RoutedEventArgs e)
    {
        Menu.IsVisible = false;
        var notificationMenu = this.FindControl<Border>("NotificationMenu");
        if (notificationMenu != null) notificationMenu.IsVisible = false;
        Window?.Navigate(new TokenView());
    }

    private void Logout_Click(object? sender, RoutedEventArgs e)
    {
        Menu.IsVisible = false;
        var notificationMenu = this.FindControl<Border>("NotificationMenu");
        if (notificationMenu != null) notificationMenu.IsVisible = false;
        Window?.Navigate(new LoginView());
    }

    private void OnNotificationReceived(NotificationModel notif)
    {
        Dispatcher.UIThread.Post(() => {
            UpdateNotificationsList();
        });
    }

    private void OnNotificationsUpdated()
    {
        Dispatcher.UIThread.Post(() => {
            UpdateNotificationsList();
        });
    }

    private void UpdateNotificationsList()
    {
        var notificationsList = this.FindControl<StackPanel>("NotificationsList");
        var notificationBadge = this.FindControl<Border>("NotificationBadge");
        if (notificationsList == null) return;

        notificationsList.Children.Clear();
        var list = NotificationService.Notifications;

        if (list.Count == 0)
        {
            notificationsList.Children.Add(new TextBlock
            {
                Text = "No notifications",
                Foreground = Avalonia.Media.Brushes.Gray,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 16, 0, 16),
                FontSize = 13
            });
            if (notificationBadge != null)
            {
                notificationBadge.IsVisible = false;
            }
            return;
        }

        bool hasUnread = false;
        foreach (var notif in list)
        {
            if (!notif.IsRead) hasUnread = true;

            var notifBorder = new Border
            {
                Background = Avalonia.Media.Brush.Parse(notif.IsRead ? "#252525" : "#2D2D2D"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 4)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto")
            };

            var textStack = new StackPanel { Spacing = 4 };
            textStack.Children.Add(new TextBlock
            {
                Text = notif.Message,
                Foreground = Avalonia.Media.Brushes.White,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                FontSize = 13
            });
            textStack.Children.Add(new TextBlock
            {
                Text = notif.CreatedAt.ToString("g"),
                Foreground = Avalonia.Media.Brushes.Gray,
                FontSize = 10
            });

            grid.Children.Add(textStack);

            notifBorder.Child = grid;
            notificationsList.Children.Add(notifBorder);
        }

        if (notificationBadge != null)
        {
            notificationBadge.IsVisible = hasUnread;
        }
    }

    // Simple message dialog (kept for backward compatibility if needed)
    private async System.Threading.Tasks.Task MessageBox(Window window, string message)
    {
        var dialog = new Window
        {
            Width = 280,
            Height = 120,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Padding = new Thickness(20),
                Background = Avalonia.Media.Brushes.Black,
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = Avalonia.Media.Brushes.White,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            }
        };

        await dialog.ShowDialog(window);
    }
}
