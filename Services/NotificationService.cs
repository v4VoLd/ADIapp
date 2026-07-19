using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADIapp.Models;
using ADIapp.Helpers;

namespace ADIapp.Services;

public static class NotificationService
{
    public static List<NotificationModel> Notifications { get; } = new();

    public static event Action<NotificationModel>? NotificationReceived;
    public static event Action? NotificationsUpdated;

    public static void AddNotification(string id, string message, string type)
    {
        var notif = new NotificationModel
        {
            Id = id,
            Message = message,
            Type = type,
            CreatedAt = DateTime.Now,
            IsRead = false
        };

        lock (Notifications)
        {
            Notifications.Insert(0, notif); // Newest first
        }

        NotificationReceived?.Invoke(notif);
    }

    public static async Task LoadNotificationsAsync()
    {
        try
        {
            var notifs = await ApiService.FetchNotificationsAsync();
            lock (Notifications)
            {
                Notifications.Clear();
                Notifications.AddRange(notifs);
            }
            NotificationsUpdated?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load database notifications: {ex.Message}", ex);
        }
    }

    public static void MarkAllAsRead()
    {
        lock (Notifications)
        {
            foreach (var notif in Notifications)
            {
                notif.IsRead = true;
            }
        }

        NotificationsUpdated?.Invoke();

        // Sync with backend database
        _ = ApiService.MarkAllNotificationsAsReadAsync();
    }

    public static void ClearAll()
    {
        lock (Notifications)
        {
            Notifications.Clear();
        }

        NotificationsUpdated?.Invoke();

        // Sync with backend database
        _ = ApiService.ClearNotificationsAsync();
    }
}
