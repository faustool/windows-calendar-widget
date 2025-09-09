using M365CalendarApp.WPF.Services;
using System;
using System.Collections.Generic;

namespace M365CalendarApp.WPF.Models;

public static class CalendarEventExtensions
{
    /// <summary>
    /// Creates notification objects for a calendar event based on its reminder settings
    /// </summary>
    public static List<EventNotification> CreateNotifications(this CalendarEventInfo eventInfo, NotificationSettings settings)
    {
        var notifications = new List<EventNotification>();

        // Skip all-day events if configured to do so
        if (eventInfo.IsAllDay && settings.ShowOnlyWorkingHours)
        {
            return notifications;
        }

        // Skip events outside working hours if configured
        if (settings.ShowOnlyWorkingHours && !IsWithinWorkingHours(eventInfo.StartTime, settings))
        {
            return notifications;
        }

        // Use event's reminder setting if enabled, otherwise use default
        var reminderMinutes = eventInfo.IsReminderOn ? eventInfo.ReminderMinutesBeforeStart : settings.DefaultReminderMinutes;
        var defaultNotificationTime = eventInfo.StartTime.AddMinutes(-reminderMinutes);
        
        // Don't create notifications for past events
        if (defaultNotificationTime <= DateTime.Now)
        {
            return notifications;
        }

        var notification = new EventNotification
        {
            EventId = !string.IsNullOrEmpty(eventInfo.Id) ? eventInfo.Id : GenerateEventId(eventInfo),
            EventSubject = eventInfo.Subject,
            EventStartTime = eventInfo.StartTime,
            EventEndTime = eventInfo.EndTime,
            EventLocation = eventInfo.Location,
            IsAllDay = eventInfo.IsAllDay,
            OriginalNotificationTime = defaultNotificationTime,
            ScheduledNotificationTime = defaultNotificationTime,
            Status = NotificationStatus.Pending
        };

        notifications.Add(notification);
        return notifications;
    }

    /// <summary>
    /// Generates a unique ID for an event based on its properties
    /// </summary>
    private static string GenerateEventId(CalendarEventInfo eventInfo)
    {
        // Create a hash based on event properties since we don't have the actual Graph API event ID
        var eventString = $"{eventInfo.Subject}_{eventInfo.StartTime:yyyyMMddHHmmss}_{eventInfo.EndTime:yyyyMMddHHmmss}_{eventInfo.Location}";
        return eventString.GetHashCode().ToString();
    }

    /// <summary>
    /// Checks if an event time falls within configured working hours
    /// </summary>
    private static bool IsWithinWorkingHours(DateTime eventTime, NotificationSettings settings)
    {
        // Check if it's a working day
        if (!settings.WorkingDays.Contains(eventTime.DayOfWeek))
        {
            return false;
        }

        // Check if it's within working hours
        var eventTimeOfDay = eventTime.TimeOfDay;
        return eventTimeOfDay >= settings.WorkingHoursStart && eventTimeOfDay <= settings.WorkingHoursEnd;
    }

    /// <summary>
    /// Updates an existing notification with snooze information
    /// </summary>
    public static void ApplySnooze(this EventNotification notification, NotificationAction snoozeAction)
    {
        notification.Status = NotificationStatus.Snoozed;
        notification.ScheduledNotificationTime = notification.CalculateSnoozeTime(snoozeAction);
        notification.SnoozeCount++;
        notification.LastSnoozedAt = DateTime.Now;

        // Reset to pending so it can be displayed again
        notification.Status = NotificationStatus.Pending;
    }

    /// <summary>
    /// Marks a notification as dismissed
    /// </summary>
    public static void Dismiss(this EventNotification notification)
    {
        notification.Status = NotificationStatus.Dismissed;
    }

    /// <summary>
    /// Checks if a notification should be auto-dismissed based on settings
    /// </summary>
    public static bool ShouldAutoDismiss(this EventNotification notification, NotificationSettings settings)
    {
        if (!settings.AutoDismissAfterMinutes)
        {
            return false;
        }

        if (notification.Status != NotificationStatus.Displayed)
        {
            return false;
        }

        // Check if the notification has been displayed for too long
        var displayTime = DateTime.Now - notification.ScheduledNotificationTime;
        return displayTime.TotalMinutes >= settings.AutoDismissMinutes;
    }

    /// <summary>
    /// Checks if a notification has exceeded the maximum snooze count
    /// </summary>
    public static bool HasExceededMaxSnoozes(this EventNotification notification, NotificationSettings settings)
    {
        return notification.SnoozeCount >= settings.MaxNotificationsPerEvent;
    }
}