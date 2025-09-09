using System;
using System.Collections.Generic;

namespace M365CalendarApp.WPF.Models;

public enum NotificationAction
{
    Dismiss,
    Snooze1Minute,
    Snooze5Minutes,
    Snooze10Minutes,
    Snooze5MinutesBeforeEvent,
    Snooze10MinutesBeforeEvent
}

public enum NotificationStatus
{
    Pending,
    Displayed,
    Dismissed,
    Snoozed
}

public class EventNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventId { get; set; } = "";
    public string EventSubject { get; set; } = "";
    public DateTime EventStartTime { get; set; }
    public DateTime EventEndTime { get; set; }
    public string EventLocation { get; set; } = "";
    public bool IsAllDay { get; set; }
    public DateTime OriginalNotificationTime { get; set; }
    public DateTime ScheduledNotificationTime { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int SnoozeCount { get; set; } = 0;
    public DateTime? LastSnoozedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool ShouldDisplay => Status == NotificationStatus.Pending && 
                                DateTime.Now >= ScheduledNotificationTime;

    public TimeSpan TimeUntilEvent => EventStartTime - DateTime.Now;

    public string GetTimeUntilEventDisplay()
    {
        var timeUntil = TimeUntilEvent;
        
        if (timeUntil.TotalDays >= 1)
        {
            return $"in {timeUntil.Days}d {timeUntil.Hours}h";
        }
        else if (timeUntil.TotalHours >= 1)
        {
            return $"in {timeUntil.Hours}h {timeUntil.Minutes}m";
        }
        else if (timeUntil.TotalMinutes >= 1)
        {
            return $"in {timeUntil.Minutes}m";
        }
        else if (timeUntil.TotalMinutes > -5) // Event started less than 5 minutes ago
        {
            return "now";
        }
        else
        {
            return "started";
        }
    }

    public DateTime CalculateSnoozeTime(NotificationAction action)
    {
        return action switch
        {
            NotificationAction.Snooze1Minute => DateTime.Now.AddMinutes(1),
            NotificationAction.Snooze5Minutes => DateTime.Now.AddMinutes(5),
            NotificationAction.Snooze10Minutes => DateTime.Now.AddMinutes(10),
            NotificationAction.Snooze5MinutesBeforeEvent => EventStartTime.AddMinutes(-5),
            NotificationAction.Snooze10MinutesBeforeEvent => EventStartTime.AddMinutes(-10),
            _ => DateTime.Now.AddMinutes(5) // Default fallback
        };
    }
}

public class NotificationSettings
{
    public bool NotificationsEnabled { get; set; } = true;
    public bool PlaySound { get; set; } = true;
    public string SoundFile { get; set; } = "";
    public int DefaultReminderMinutes { get; set; } = 15;
    public bool ShowOnlyWorkingHours { get; set; } = false;
    public TimeSpan WorkingHoursStart { get; set; } = new TimeSpan(9, 0, 0);
    public TimeSpan WorkingHoursEnd { get; set; } = new TimeSpan(17, 0, 0);
    public List<DayOfWeek> WorkingDays { get; set; } = new List<DayOfWeek>
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    };
    public int MaxNotificationsPerEvent { get; set; } = 3;
    public bool AutoDismissAfterMinutes { get; set; } = false;
    public int AutoDismissMinutes { get; set; } = 10;
}

public class NotificationActionEventArgs : EventArgs
{
    public EventNotification Notification { get; }
    public NotificationAction Action { get; }

    public NotificationActionEventArgs(EventNotification notification, NotificationAction action)
    {
        Notification = notification;
        Action = action;
    }
}

public class NotificationStatistics
{
    public int TotalNotifications { get; set; }
    public int PendingNotifications { get; set; }
    public int DisplayedNotifications { get; set; }
    public int DismissedNotifications { get; set; }
    public int SnoozedNotifications { get; set; }
    public DateTime? NextNotificationTime { get; set; }
}