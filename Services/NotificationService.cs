using System;
using System.Collections.Generic;
using ADIapp.Models;

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
    }

    public static void ClearAll()
    {
        lock (Notifications)
        {
            Notifications.Clear();
        }

        NotificationsUpdated?.Invoke();
    }
}
