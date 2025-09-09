# Calendar Notifications

The M365 Calendar application now includes comprehensive notification functionality that displays calendar event reminders with flexible dismiss and snooze options.

## Features

### 🔔 Smart Notifications
- **Event-based reminders**: Automatically creates notifications based on calendar event reminder settings
- **Default reminders**: Uses configurable default reminder time (15 minutes before event) when event has no specific reminder
- **Working hours filtering**: Optional filtering to show notifications only during configured working hours
- **Persistent storage**: Notifications are saved and restored across application restarts

### ⏰ Flexible Snooze Options
- **1 minute**: Quick snooze for immediate follow-up
- **5 minutes**: Standard short snooze
- **10 minutes**: Standard medium snooze  
- **5 minutes before event**: Snooze until 5 minutes before the event starts
- **10 minutes before event**: Snooze until 10 minutes before the event starts

### 🎯 Smart Behavior
- **Auto-dismiss**: Optional automatic dismissal after configurable time (default: 10 minutes)
- **Snooze limits**: Configurable maximum snoozes per event (default: 3)
- **Missed notification recovery**: Shows notifications that should have appeared while app was closed
- **Event status awareness**: Disables "before event" snooze options for events that have already started

## Configuration

### Notification Settings

The notification system can be configured through the application settings:

```json
{
  "Notifications": {
    "NotificationsEnabled": true,
    "PlayNotificationSound": true,
    "DefaultReminderMinutes": 15,
    "ShowOnlyWorkingHours": false,
    "WorkingHoursStart": "09:00:00",
    "WorkingHoursEnd": "17:00:00",
    "WorkingDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
    "MaxNotificationsPerEvent": 3,
    "AutoDismissNotifications": false,
    "AutoDismissMinutes": 10
  }
}
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `NotificationsEnabled` | Enable/disable all notifications | `true` |
| `PlayNotificationSound` | Play system sound when notification appears | `true` |
| `DefaultReminderMinutes` | Default reminder time when event has no reminder | `15` |
| `ShowOnlyWorkingHours` | Only show notifications during working hours | `false` |
| `WorkingHoursStart` | Start of working hours (HH:mm:ss format) | `09:00:00` |
| `WorkingHoursEnd` | End of working hours (HH:mm:ss format) | `17:00:00` |
| `WorkingDays` | Days of week to show notifications | Mon-Fri |
| `MaxNotificationsPerEvent` | Maximum snoozes allowed per event | `3` |
| `AutoDismissNotifications` | Auto-dismiss notifications after time | `false` |
| `AutoDismissMinutes` | Minutes before auto-dismissing | `10` |

## User Interface

### Notification Window

The notification window displays:
- **Event subject** in bold
- **Event time** with clock icon
- **Event location** (if available) with location icon
- **Time until event** with countdown
- **Snooze count** (if event has been snoozed)

### Action Buttons

- **Snooze buttons**: Five different snooze options arranged horizontally
- **Dismiss button**: Red dismiss button to permanently close the notification
- **Close button (×)**: Alternative dismiss option in the header

### Window Behavior

- **Always on top**: Notifications appear above other windows
- **Auto-positioning**: Stacks multiple notifications in bottom-right corner
- **Draggable**: Can be moved around the screen
- **Auto-close**: Automatically closes after 10 minutes
- **Keyboard support**: Press Escape to dismiss

## Technical Architecture

### Core Components

1. **NotificationService**: Manages notification lifecycle, scheduling, and persistence
2. **NotificationManager**: Coordinates between calendar service and notification service
3. **NotificationWindow**: WPF window for displaying notifications
4. **EventNotification**: Data model representing a notification
5. **NotificationSettings**: Configuration model for notification behavior

### Data Flow

```
Calendar Events → NotificationManager → NotificationService → NotificationWindow
                                    ↓
                              Persistent Storage
```

### Persistence

- Notifications are stored in JSON format at: `%APPDATA%/M365CalendarApp/notifications.json`
- Automatic backup and recovery mechanisms prevent data loss
- Old notifications are automatically cleaned up after 7 days

### Integration Points

- **Calendar Service**: Retrieves events with reminder information from Microsoft Graph API
- **Configuration Service**: Loads and saves notification settings
- **Main Application**: Initializes notification system and handles cleanup

## Usage Examples

### Basic Usage

1. **Login** to the application with your Microsoft 365 account
2. **Calendar events** with reminders will automatically create notifications
3. **Notifications appear** at the scheduled reminder time
4. **Choose an action**: Dismiss or snooze with various time options

### Working Hours Configuration

To only receive notifications during business hours:

```json
{
  "ShowOnlyWorkingHours": true,
  "WorkingHoursStart": "08:00:00",
  "WorkingHoursEnd": "18:00:00",
  "WorkingDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
}
```

### Custom Reminder Times

The system respects individual event reminder settings from Outlook/Calendar:
- If an event has a 30-minute reminder → notification appears 30 minutes before
- If an event has no reminder → uses `DefaultReminderMinutes` setting
- All-day events can be optionally excluded with `ShowOnlyWorkingHours`

## Troubleshooting

### Common Issues

**Notifications not appearing:**
- Check that `NotificationsEnabled` is `true`
- Verify the application is running and authenticated
- Check if `ShowOnlyWorkingHours` is filtering out events

**Notifications appearing at wrong times:**
- Verify system time zone settings
- Check individual event reminder settings in Outlook
- Confirm `DefaultReminderMinutes` configuration

**Too many notifications:**
- Adjust `MaxNotificationsPerEvent` to limit snoozes
- Enable `AutoDismissNotifications` for automatic cleanup
- Use `ShowOnlyWorkingHours` to filter by time

### Debug Information

The application logs notification activity to the debug console:
- Notification creation and scheduling
- Missed notification recovery
- Configuration loading/saving
- Error conditions

### File Locations

- **Configuration**: `%APPDATA%/M365CalendarApp/appsettings.json`
- **Notifications**: `%APPDATA%/M365CalendarApp/notifications.json`
- **Backup**: `%APPDATA%/M365CalendarApp/notifications.json.backup`

## Future Enhancements

Potential future improvements:
- Custom notification sounds
- Email/SMS notification options
- Meeting preparation reminders
- Integration with Teams/Skype for meeting links
- Notification templates and customization
- Statistics and analytics dashboard

## API Reference

### NotificationService Methods

```csharp
// Add events for notification
void AddEventsForNotification(IEnumerable<CalendarEventInfo> events)

// Handle user actions
void HandleNotificationAction(EventNotification notification, NotificationAction action)

// Get pending notifications
List<EventNotification> GetPendingNotifications()

// Update settings
void UpdateSettings(NotificationSettings settings)

// Recover missed notifications
void RecoverMissedNotifications()
```

### NotificationManager Methods

```csharp
// Update notifications for date range
Task UpdateNotificationsAsync(DateTime startDate, int days = 7)

// Refresh all notifications
Task RefreshNotificationsAsync()

// Clear all notifications
void ClearAllNotifications()
```

### Events

```csharp
// Notification triggered
event EventHandler<NotificationActionEventArgs> NotificationTriggered

// Notification dismissed
event EventHandler<EventNotification> NotificationDismissed

// Notification snoozed
event EventHandler<EventNotification> NotificationSnoozed
```