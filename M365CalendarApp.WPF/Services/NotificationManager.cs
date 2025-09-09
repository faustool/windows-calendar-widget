using M365CalendarApp.WPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace M365CalendarApp.WPF.Services;

public class NotificationManager : IDisposable
{
    private readonly NotificationService _notificationService;
    private readonly CalendarService _calendarService;
    private readonly ConfigurationService _configService;
    private readonly List<NotificationWindow> _activeWindows = new();
    private bool _disposed = false;

    public NotificationManager(CalendarService calendarService, ConfigurationService configService)
    {
        _calendarService = calendarService;
        _configService = configService;
        
        // Load notification settings from configuration
        var notificationSettings = LoadNotificationSettings();
        _notificationService = new NotificationService(notificationSettings);

        // Subscribe to notification events
        _notificationService.NotificationTriggered += OnNotificationTriggered;
        _notificationService.NotificationDismissed += OnNotificationDismissed;
        _notificationService.NotificationSnoozed += OnNotificationSnoozed;
    }

    /// <summary>
    /// Updates notifications for the given date range
    /// </summary>
    public async Task UpdateNotificationsAsync(DateTime startDate, int days = 7)
    {
        try
        {
            var allEvents = new List<CalendarEventInfo>();

            // Get events for the specified date range
            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                var dayEvents = await _calendarService.GetEventsForDateAsync(date);
                allEvents.AddRange(dayEvents);
            }

            // Add events to notification service
            _notificationService.AddEventsForNotification(allEvents);

            System.Diagnostics.Debug.WriteLine($"Updated notifications for {allEvents.Count} events from {startDate:yyyy-MM-dd} to {startDate.AddDays(days-1):yyyy-MM-dd}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating notifications: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates notifications for today and the next week
    /// </summary>
    public async Task RefreshNotificationsAsync()
    {
        await UpdateNotificationsAsync(DateTime.Today, 7);
    }

    /// <summary>
    /// Gets all pending notifications for debugging
    /// </summary>
    public List<EventNotification> GetPendingNotifications()
    {
        return _notificationService.GetPendingNotifications();
    }

    /// <summary>
    /// Gets all notifications for debugging
    /// </summary>
    public List<EventNotification> GetAllNotifications()
    {
        return _notificationService.GetAllNotifications();
    }

    /// <summary>
    /// Clears all notifications
    /// </summary>
    public void ClearAllNotifications()
    {
        _notificationService.ClearAllNotifications();
        CloseAllNotificationWindows();
    }

    /// <summary>
    /// Updates notification settings
    /// </summary>
    public void UpdateNotificationSettings(NotificationSettings settings)
    {
        _notificationService.UpdateSettings(settings);
        SaveNotificationSettings(settings);
    }

    /// <summary>
    /// Gets the underlying notification service for advanced operations
    /// </summary>
    public NotificationService GetNotificationService()
    {
        return _notificationService;
    }

    private void OnNotificationTriggered(object? sender, NotificationActionEventArgs e)
    {
        // Show notification window on UI thread
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            ShowNotificationWindow(e.Notification);
        });
    }

    private void OnNotificationDismissed(object? sender, EventNotification e)
    {
        System.Diagnostics.Debug.WriteLine($"Notification dismissed: {e.EventSubject}");
    }

    private void OnNotificationSnoozed(object? sender, EventNotification e)
    {
        System.Diagnostics.Debug.WriteLine($"Notification snoozed: {e.EventSubject} until {e.ScheduledNotificationTime:HH:mm}");
    }

    private void ShowNotificationWindow(EventNotification notification)
    {
        try
        {
            // Check if we already have a window for this notification
            var existingWindow = _activeWindows.FirstOrDefault(w => w.DataContext?.Equals(notification.Id) == true);
            if (existingWindow != null)
            {
                existingWindow.FlashWindow();
                return;
            }

            var notificationWindow = new NotificationWindow(notification);
            notificationWindow.DataContext = notification.Id; // Store ID for tracking
            notificationWindow.NotificationAction += OnNotificationWindowAction;
            notificationWindow.Closed += OnNotificationWindowClosed;

            _activeWindows.Add(notificationWindow);
            notificationWindow.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing notification window: {ex.Message}");
        }
    }

    private void OnNotificationWindowAction(object? sender, NotificationActionEventArgs e)
    {
        _notificationService.HandleNotificationAction(e.Notification, e.Action);
    }

    private void OnNotificationWindowClosed(object? sender, EventArgs e)
    {
        if (sender is NotificationWindow window)
        {
            _activeWindows.Remove(window);
            window.NotificationAction -= OnNotificationWindowAction;
            window.Closed -= OnNotificationWindowClosed;
        }
    }

    private void CloseAllNotificationWindows()
    {
        var windowsToClose = _activeWindows.ToList();
        foreach (var window in windowsToClose)
        {
            try
            {
                window.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing notification window: {ex.Message}");
            }
        }
        _activeWindows.Clear();
    }

    private NotificationSettings LoadNotificationSettings()
    {
        try
        {
            var config = _configService.LoadConfigurationAsync().GetAwaiter().GetResult();
            return new NotificationSettings
            {
                NotificationsEnabled = config.Notifications.NotificationsEnabled,
                PlaySound = config.Notifications.PlayNotificationSound,
                DefaultReminderMinutes = config.Notifications.DefaultReminderMinutes,
                ShowOnlyWorkingHours = config.Notifications.ShowOnlyWorkingHours,
                WorkingHoursStart = config.Notifications.WorkingHoursStart,
                WorkingHoursEnd = config.Notifications.WorkingHoursEnd,
                WorkingDays = config.Notifications.WorkingDays?.ToList() ?? new List<DayOfWeek>
                {
                    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, 
                    DayOfWeek.Thursday, DayOfWeek.Friday
                },
                MaxNotificationsPerEvent = config.Notifications.MaxNotificationsPerEvent,
                AutoDismissAfterMinutes = config.Notifications.AutoDismissNotifications,
                AutoDismissMinutes = config.Notifications.AutoDismissMinutes
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading notification settings: {ex.Message}");
            return new NotificationSettings();
        }
    }

    private void SaveNotificationSettings(NotificationSettings settings)
    {
        try
        {
            var config = _configService.LoadConfigurationAsync().GetAwaiter().GetResult();
            config.Notifications.NotificationsEnabled = settings.NotificationsEnabled;
            config.Notifications.PlayNotificationSound = settings.PlaySound;
            config.Notifications.DefaultReminderMinutes = settings.DefaultReminderMinutes;
            config.Notifications.ShowOnlyWorkingHours = settings.ShowOnlyWorkingHours;
            config.Notifications.WorkingHoursStart = settings.WorkingHoursStart;
            config.Notifications.WorkingHoursEnd = settings.WorkingHoursEnd;
            config.Notifications.WorkingDays = settings.WorkingDays?.ToArray() ?? new[]
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, 
                DayOfWeek.Thursday, DayOfWeek.Friday
            };
            config.Notifications.MaxNotificationsPerEvent = settings.MaxNotificationsPerEvent;
            config.Notifications.AutoDismissNotifications = settings.AutoDismissAfterMinutes;
            config.Notifications.AutoDismissMinutes = settings.AutoDismissMinutes;

            _configService.SaveConfigurationAsync(config).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving notification settings: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            CloseAllNotificationWindows();
            _notificationService?.Dispose();
            _disposed = true;
        }
    }
}