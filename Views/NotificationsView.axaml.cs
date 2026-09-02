using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using ADIapp.Services;
using ADIapp.Models;

namespace ADIapp.Views;

public partial class NotificationsView : UserControl
{
    public NotificationsView()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (s, e) =>
        {
            NotificationService.NotificationsUpdated += OnNotificationsUpdated;
            LanguageService.LanguageChanged += OnLanguageChanged;
            RenderNotifications();
            UpdateLocalizedText();
        };

        this.DetachedFromVisualTree += (s, e) =>
        {
            NotificationService.NotificationsUpdated -= OnNotificationsUpdated;
            LanguageService.LanguageChanged -= OnLanguageChanged;
        };
    }

    private void OnNotificationsUpdated()
    {
        Dispatcher.UIThread.Post(() => RenderNotifications());
    }

    private void OnLanguageChanged()
    {
        Dispatcher.UIThread.Post(() => UpdateLocalizedText());
    }

    private void UpdateLocalizedText()
    {
        if (NotificationsTitleBlock != null) NotificationsTitleBlock.Text = LanguageService.Get("Notifications_Title");
        if (NotificationsSubtitleBlock != null) NotificationsSubtitleBlock.Text = LanguageService.Get("Notifications_Subtitle");
        if (MarkAllReadBtn != null) MarkAllReadBtn.Content = LanguageService.Get("Notifications_MarkAll");
        if (ClearAllBtn != null) ClearAllBtn.Content = LanguageService.Get("TopPanel_ClearAll");
        if (BackButton != null) BackButton.Content = LanguageService.Get("Tune_Back");
    }

    private void RenderNotifications()
    {
        if (NotificationsListPanel == null) return;

        NotificationsListPanel.Children.Clear();
        var list = NotificationService.Notifications;

        if (NotificationCountBlock != null)
        {
            NotificationCountBlock.Text = string.Format(LanguageService.Get("Notifications_AllCount"), list.Count);
        }

        if (list.Count == 0)
        {
            var emptyStack = new StackPanel
            {
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 40)
            };

            emptyStack.Children.Add(new TextBlock
            {
                Text = "🔔",
                FontSize = 36,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            });

            emptyStack.Children.Add(new TextBlock
            {
                Text = LanguageService.Get("Notifications_NoNotifs"),
                FontSize = 15,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            });

            emptyStack.Children.Add(new TextBlock
            {
                Text = LanguageService.Get("Notifications_AllCaughtUp"),
                FontSize = 13,
                Foreground = Brush.Parse("#888888"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            });

            NotificationsListPanel.Children.Add(emptyStack);
            return;
        }

        foreach (var notif in list)
        {
            if (notif == null) continue;
            string notifId = notif.Id;

            var cardBorder = new Border
            {
                Background = Brush.Parse(notif.IsRead ? "#222222" : "#2A2A2A"),
                BorderBrush = Brush.Parse(notif.IsRead ? "#333333" : "#444444"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 14),
                Margin = new Thickness(0, 2)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto")
            };

            // 1. Icon Badge
            string iconText = notif.Type?.ToLower() switch
            {
                "info" => "ℹ️",
                "order" => "⚡",
                "ticket" => "📩",
                "warning" => "⚠️",
                _ => "🔔"
            };

            var iconBorder = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(10),
                Background = Brush.Parse("#1A1A1A"),
                Margin = new Thickness(0, 0, 14, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = iconText,
                    FontSize = 18,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };
            Grid.SetColumn(iconBorder, 0);
            grid.Children.Add(iconBorder);

            // 2. Content Stack (Message & Timestamp)
            var contentStack = new StackPanel { Spacing = 4, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            contentStack.Children.Add(new TextBlock
            {
                Text = notif.Message,
                FontSize = 14,
                FontWeight = notif.IsRead ? FontWeight.Normal : FontWeight.SemiBold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });

            var displayDate = notif.CreatedAt.Kind == DateTimeKind.Utc ? notif.CreatedAt.ToLocalTime() : notif.CreatedAt;
            contentStack.Children.Add(new TextBlock
            {
                Text = displayDate.ToString("yyyy-MM-dd HH:mm"),
                FontSize = 11,
                Foreground = Brush.Parse("#888888")
            });
            Grid.SetColumn(contentStack, 1);
            grid.Children.Add(contentStack);

            // 3. Unread Status Dot
            if (!notif.IsRead)
            {
                var unreadDot = new Border
                {
                    Width = 10,
                    Height = 10,
                    CornerRadius = new CornerRadius(5),
                    Background = Brush.Parse("#FF4D4D"),
                    Margin = new Thickness(10, 0),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                Grid.SetColumn(unreadDot, 2);
                grid.Children.Add(unreadDot);
            }

            // 4. INDIVIDUAL DELETE BUTTON 🗑️
            var deleteBtn = new Button
            {
                Content = LanguageService.Get("Notifications_Delete"),
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brush.Parse("#FF8080"),
                Background = Brush.Parse("#381A1A"),
                BorderBrush = Brush.Parse("#592828"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            deleteBtn.Click += (s, e) =>
            {
                NotificationService.DeleteNotification(notifId);
            };
            Grid.SetColumn(deleteBtn, 3);
            grid.Children.Add(deleteBtn);

            cardBorder.Child = grid;
            NotificationsListPanel.Children.Add(cardBorder);
        }
    }

    private void MarkAllRead_Click(object? sender, RoutedEventArgs e)
    {
        NotificationService.MarkAllAsRead();
    }

    private void ClearAll_Click(object? sender, RoutedEventArgs e)
    {
        NotificationService.ClearAll();
    }

    private void Back_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.FindAncestorOfType<MainWindow>();
        window?.Navigate(new HomeView());
    }
}
