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
        UpdateUserInfo();

        NotificationService.NotificationReceived += OnNotificationReceived;
        NotificationService.NotificationsUpdated += OnNotificationsUpdated;
        WebSocketManager.OrderUpdated += OnOrderUpdated;
        LanguageService.LanguageChanged += OnLanguageChanged;
        ApiService.CurrentUserChanged += OnCurrentUserChanged;

        UpdateNotificationsList();
        UpdateLocalizedText();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        NotificationService.NotificationReceived -= OnNotificationReceived;
        NotificationService.NotificationsUpdated -= OnNotificationsUpdated;
        WebSocketManager.OrderUpdated -= OnOrderUpdated;
        LanguageService.LanguageChanged -= OnLanguageChanged;
        ApiService.CurrentUserChanged -= OnCurrentUserChanged;
    }

    private void OnCurrentUserChanged(UserDto? user)
    {
        Dispatcher.UIThread.Post(() => UpdateUserInfo());
    }

    private async void OnOrderUpdated()
    {
        await ApiService.FetchProfileAsync();
    }

    private void UpdateUserInfo()
    {
        if (ApiService.CurrentUser != null)
        {
            var nameBlock = this.FindControl<TextBlock>("UserNameBlock");
            if (nameBlock != null)
            {
                nameBlock.Text = $"{ApiService.CurrentUser.FirstName} {ApiService.CurrentUser.LastName}".Trim();
            }

            if (UserTokenBlock != null)
            {
                var credit = ApiService.CurrentUser.AvailableCredit;
                UserTokenBlock.Text = $"{LanguageService.Get("TopPanel_Token")} {credit}";
            }
        }
    }

    private void OnLanguageChanged()
    {
        Dispatcher.UIThread.Post(() => {
            UpdateLocalizedText();
        });
    }

    private void UpdateLocalizedText()
    {
        if (HelloLabelBlock != null) HelloLabelBlock.Text = LanguageService.Get("TopPanel_Hello");
        if (UserTokenBlock != null)
        {
            var credit = ApiService.CurrentUser?.AvailableCredit ?? 0;
            UserTokenBlock.Text = $"{LanguageService.Get("TopPanel_Token")} {credit}";
        }
        if (NotifTitleBlock != null) NotifTitleBlock.Text = LanguageService.Get("TopPanel_Notifications");
        if (ClearAllBtn != null) ClearAllBtn.Content = LanguageService.Get("TopPanel_ClearAll");
        if (AccountBtn != null) AccountBtn.Content = LanguageService.Get("TopPanel_Account");
        if (LogoutBtn != null) LogoutBtn.Content = LanguageService.Get("TopPanel_Logout");
        var seeAllBtn = this.FindControl<Button>("SeeAllNotifsBtn");
        if (seeAllBtn != null) seeAllBtn.Content = LanguageService.Get("TopPanel_SeeAll");
        if (TokenBtn != null) TokenBtn.Content = LanguageService.Get("TopPanel_TokenMenu");
        if (OrderHistoryBtn != null) OrderHistoryBtn.Content = LanguageService.Get("TopPanel_OrderHistory");
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

    private void SeeAllNotifications_Click(object? sender, RoutedEventArgs e)
    {
        var notificationMenu = this.FindControl<Border>("NotificationMenu");
        if (notificationMenu != null) notificationMenu.IsVisible = false;
        Window?.Navigate(new NotificationsView());
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

    private void OrderHistory_Click(object? sender, RoutedEventArgs e)
    {
        Menu.IsVisible = false;
        var notificationMenu = this.FindControl<Border>("NotificationMenu");
        if (notificationMenu != null) notificationMenu.IsVisible = false;
        Window?.Navigate(new OrderHistoryView());
    }

    private async void Logout_Click(object? sender, RoutedEventArgs e)
    {
        Menu.IsVisible = false;
        var notificationMenu = this.FindControl<Border>("NotificationMenu");
        if (notificationMenu != null) notificationMenu.IsVisible = false;

        var window = Window;
        if (window == null) return;

        var dialog = new ConfirmLogoutDialog();
        var confirm = await dialog.ShowDialog<bool>(window);
        if (confirm)
        {
            ApiService.Logout();
            window.Navigate(new LoginView());
        }
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
            if (notif == null) continue;
            string notifId = notif.Id;
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

            var textStack = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 8, 0) };
            textStack.Children.Add(new TextBlock
            {
                Text = notif.Message,
                Foreground = Avalonia.Media.Brushes.White,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                FontSize = 12
            });
            var displayDate = notif.CreatedAt.Kind == DateTimeKind.Utc ? notif.CreatedAt.ToLocalTime() : notif.CreatedAt;
            textStack.Children.Add(new TextBlock
            {
                Text = displayDate.ToString("g"),
                Foreground = Avalonia.Media.Brushes.Gray,
                FontSize = 10
            });
            Grid.SetColumn(textStack, 0);
            grid.Children.Add(textStack);

            var deleteBtn = new Button
            {
                Content = "✕",
                FontSize = 10,
                Padding = new Thickness(6, 2),
                Background = Avalonia.Media.Brushes.Transparent,
                Foreground = Avalonia.Media.Brush.Parse("#AAAAAA"),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                CornerRadius = new CornerRadius(4),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            deleteBtn.Click += (s, e) =>
            {
                NotificationService.DeleteNotification(notifId);
            };
            Grid.SetColumn(deleteBtn, 1);
            grid.Children.Add(deleteBtn);

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
            Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new Uri("avares://ADIapp/Assets/sidebar_logo.png"))),
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
