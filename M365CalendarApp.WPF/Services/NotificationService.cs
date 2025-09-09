using M365CalendarApp.WPF.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace M365CalendarApp.WPF.Services;

public class NotificationService : IDisposable
{
    private readonly List<EventNotification> _notifications = new();
    private readonly NotificationSettings _settings;
    private readonly Timer _checkTimer;
    private readonly string _notificationsFilePath;
    private bool _disposed = false;

    public event EventHandler<NotificationActionEventArgs>? NotificationTriggered;
    public event EventHandler<EventNotification>? NotificationDismissed;
    public event EventHandler<EventNotification>? NotificationSnoozed;

    public NotificationService(NotificationSettings? settings = null)
    {
        _settings = settings ?? new NotificationSettings();
        _notificationsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "M365CalendarApp",
            "notifications.json"
        );

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(_notificationsFilePath)!);

        // Load existing notifications
        LoadNotifications();

        // Start timer to check for notifications every 30 seconds (delay initial check by 5 seconds to avoid startup issues)
        _checkTimer = new Timer(CheckForNotifications, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Adds notifications for calendar events
    /// </summary>
    public void AddEventsForNotification(IEnumerable<CalendarEventInfo> events)
    {
        lock (_notifications)
        {
            foreach (var eventInfo in events)
            {
                var newNotifications = eventInfo.CreateNotifications(_settings);
                
                foreach (var notification in newNotifications)
                {
                    // Check if we already have a notification for this event
                    var existingNotification = _notifications.FirstOrDefault(n => 
                        n.EventId == notification.EventId && 
                        n.Status != NotificationStatus.Dismissed);

                    if (existingNotification == null)
                    {
                        _notifications.Add(notification);
                    }
                }
            }

            // Clean up old notifications
            CleanupOldNotifications();
            SaveNotifications();
        }
    }

    /// <summary>
    /// Handles notification actions (dismiss, snooze)
    /// </summary>
    public void HandleNotificationAction(EventNotification notification, NotificationAction action)
    {
        lock (_notifications)
        {
            var existingNotification = _notifications.FirstOrDefault(n => n.Id == notification.Id);
            if (existingNotification == null) return;

            switch (action)
            {
                case NotificationAction.Dismiss:
                    existingNotification.Dismiss();
                    NotificationDismissed?.Invoke(this, existingNotification);
                    break;

                case NotificationAction.Snooze1Minute:
                case NotificationAction.Snooze5Minutes:
                case NotificationAction.Snooze10Minutes:
                case NotificationAction.Snooze5MinutesBeforeEvent:
                case NotificationAction.Snooze10MinutesBeforeEvent:
                    if (!existingNotification.HasExceededMaxSnoozes(_settings))
                    {
                        existingNotification.ApplySnooze(action);
                        NotificationSnoozed?.Invoke(this, existingNotification);
                    }
                    else
                    {
                        // Auto-dismiss if max snoozes exceeded
                        existingNotification.Dismiss();
                        NotificationDismissed?.Invoke(this, existingNotification);
                    }
                    break;
            }

            SaveNotifications();
        }
    }

    /// <summary>
    /// Gets all pending notifications that should be displayed
    /// </summary>
    public List<EventNotification> GetPendingNotifications()
    {
        lock (_notifications)
        {
            return _notifications
                .Where(n => n.ShouldDisplay)
                .OrderBy(n => n.ScheduledNotificationTime)
                .ToList();
        }
    }

    /// <summary>
    /// Gets all notifications for debugging/management purposes
    /// </summary>
    public List<EventNotification> GetAllNotifications()
    {
        lock (_notifications)
        {
            return _notifications.ToList();
        }
    }

    /// <summary>
    /// Clears all notifications (useful for testing or reset)
    /// </summary>
    public void ClearAllNotifications()
    {
        lock (_notifications)
        {
            _notifications.Clear();
            SaveNotifications();
        }
    }

    /// <summary>
    /// Updates notification settings
    /// </summary>
    public void UpdateSettings(NotificationSettings newSettings)
    {
        // Copy settings
        _settings.NotificationsEnabled = newSettings.NotificationsEnabled;
        _settings.PlaySound = newSettings.PlaySound;
        _settings.SoundFile = newSettings.SoundFile;
        _settings.DefaultReminderMinutes = newSettings.DefaultReminderMinutes;
        _settings.ShowOnlyWorkingHours = newSettings.ShowOnlyWorkingHours;
        _settings.WorkingHoursStart = newSettings.WorkingHoursStart;
        _settings.WorkingHoursEnd = newSettings.WorkingHoursEnd;
        _settings.WorkingDays = newSettings.WorkingDays;
        _settings.MaxNotificationsPerEvent = newSettings.MaxNotificationsPerEvent;
        _settings.AutoDismissAfterMinutes = newSettings.AutoDismissAfterMinutes;
        _settings.AutoDismissMinutes = newSettings.AutoDismissMinutes;
    }

    /// <summary>
    /// Recovers notifications that should have been displayed while the application was closed
    /// </summary>
    public void RecoverMissedNotifications()
    {
        lock (_notifications)
        {
            var now = DateTime.Now;
            var missedNotifications = _notifications
                .Where(n => n.Status == NotificationStatus.Pending && 
                           n.ScheduledNotificationTime <= now &&
                           n.EventStartTime > now.AddMinutes(-30)) // Only recover notifications for events that haven't started more than 30 minutes ago
                .ToList();

            foreach (var notification in missedNotifications)
            {
                // Mark as displayed and trigger immediately
                notification.Status = NotificationStatus.Displayed;
                
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    NotificationTriggered?.Invoke(this, new NotificationActionEventArgs(notification, NotificationAction.Dismiss));
                });
            }

            if (missedNotifications.Any())
            {
                SaveNotifications();
                System.Diagnostics.Debug.WriteLine($"Recovered {missedNotifications.Count} missed notifications");
            }
        }
    }

    /// <summary>
    /// Gets statistics about notifications for debugging
    /// </summary>
    public NotificationStatistics GetStatistics()
    {
        lock (_notifications)
        {
            return new NotificationStatistics
            {
                TotalNotifications = _notifications.Count,
                PendingNotifications = _notifications.Count(n => n.Status == NotificationStatus.Pending),
                DisplayedNotifications = _notifications.Count(n => n.Status == NotificationStatus.Displayed),
                DismissedNotifications = _notifications.Count(n => n.Status == NotificationStatus.Dismissed),
                SnoozedNotifications = _notifications.Count(n => n.Status == NotificationStatus.Snoozed),
                NextNotificationTime = _notifications
                    .Where(n => n.Status == NotificationStatus.Pending)
                    .OrderBy(n => n.ScheduledNotificationTime)
                    .FirstOrDefault()?.ScheduledNotificationTime
            };
        }
    }

    private void CheckForNotifications(object? state)
    {
        if (!_settings.NotificationsEnabled) return;
        
        // Safety check: ensure application is fully initialized
        if (System.Windows.Application.Current?.Dispatcher == null)
        {
            System.Diagnostics.Debug.WriteLine("Skipping notification check - application not fully initialized");
            return;
        }

        try
        {
            List<EventNotification> notificationsToShow;
            
            lock (_notifications)
            {
                // Get notifications that should be displayed
                notificationsToShow = _notifications
                    .Where(n => n.ShouldDisplay)
                    .ToList();

                // Handle auto-dismiss
                var notificationsToAutoDismiss = _notifications
                    .Where(n => n.ShouldAutoDismiss(_settings))
                    .ToList();

                foreach (var notification in notificationsToAutoDismiss)
                {
                    notification.Dismiss();
                }

                // Mark notifications as displayed
                foreach (var notification in notificationsToShow)
                {
                    notification.Status = NotificationStatus.Displayed;
                }

                if (notificationsToAutoDismiss.Any() || notificationsToShow.Any())
                {
                    SaveNotifications();
                }
            }

            // Trigger notifications on UI thread
            if (notificationsToShow.Any())
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.HasShutdownStarted)
                {
                    dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            foreach (var notification in notificationsToShow)
                            {
                                NotificationTriggered?.Invoke(this, new NotificationActionEventArgs(notification, NotificationAction.Dismiss));
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error triggering notifications: {ex.Message}");
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking notifications: {ex.Message}");
        }
    }

    private void CleanupOldNotifications()
    {
        var cutoffDate = DateTime.Now.AddDays(-7); // Keep notifications for 7 days
        
        _notifications.RemoveAll(n => 
            (n.Status == NotificationStatus.Dismissed && n.CreatedAt < cutoffDate) ||
            (n.EventEndTime < DateTime.Now.AddHours(-2)) // Remove notifications for events that ended more than 2 hours ago
        );
    }

    private void LoadNotifications()
    {
        try
        {
            if (File.Exists(_notificationsFilePath))
            {
                var json = File.ReadAllText(_notificationsFilePath);
                var notifications = JsonSerializer.Deserialize<List<EventNotification>>(json);
                
                if (notifications != null)
                {
                    lock (_notifications)
                    {
                        _notifications.Clear();
                        _notifications.AddRange(notifications);
                        CleanupOldNotifications();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading notifications: {ex.Message}");
        }
    }

    private void SaveNotifications()
    {
        try
        {
            var json = JsonSerializer.Serialize(_notifications, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Create backup of existing file
            if (File.Exists(_notificationsFilePath))
            {
                var backupPath = _notificationsFilePath + ".backup";
                File.Copy(_notificationsFilePath, backupPath, true);
            }
            
            File.WriteAllText(_notificationsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving notifications: {ex.Message}");
            
            // Try to restore from backup if save failed
            TryRestoreFromBackup();
        }
    }

    private void TryRestoreFromBackup()
    {
        try
        {
            var backupPath = _notificationsFilePath + ".backup";
            if (File.Exists(backupPath))
            {
                File.Copy(backupPath, _notificationsFilePath, true);
                System.Diagnostics.Debug.WriteLine("Restored notifications from backup");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error restoring from backup: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _checkTimer?.Dispose();
            SaveNotifications();
            _disposed = true;
        }
    }
}