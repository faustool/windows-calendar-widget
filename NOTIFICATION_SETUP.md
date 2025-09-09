# Quick Notification Setup Guide

## Getting Started with Calendar Notifications

### 1. Enable Notifications
Notifications are enabled by default. If you need to enable them:
1. Open the application settings
2. Navigate to the Notifications section
3. Set `NotificationsEnabled` to `true`

### 2. Basic Configuration
The default settings work well for most users:
- **15-minute reminders** for events without specific reminder times
- **System notification sound** when alerts appear
- **Up to 3 snoozes** per event allowed
- **All-day coverage** (not limited to working hours)

### 3. First Notification
1. **Login** to your Microsoft 365 account
2. **Create a test event** in Outlook/Calendar with a 1-minute reminder
3. **Wait for the notification** to appear in the bottom-right corner
4. **Try the snooze options** to see how they work

### 4. Customization Options

#### Working Hours Only
To receive notifications only during business hours:
```json
{
  "ShowOnlyWorkingHours": true,
  "WorkingHoursStart": "09:00:00",
  "WorkingHoursEnd": "17:00:00"
}
```

#### Auto-Dismiss
To automatically dismiss notifications after 5 minutes:
```json
{
  "AutoDismissNotifications": true,
  "AutoDismissMinutes": 5
}
```

#### Custom Default Reminder
To change the default reminder time to 30 minutes:
```json
{
  "DefaultReminderMinutes": 30
}
```

## Snooze Options Explained

| Option | Description | Use Case |
|--------|-------------|----------|
| **1 min** | Quick snooze | Last-minute preparation |
| **5 min** | Standard short snooze | Brief delay needed |
| **10 min** | Standard medium snooze | More preparation time |
| **5 min before** | Snooze until 5 min before event | Final reminder |
| **10 min before** | Snooze until 10 min before event | Preparation reminder |

## Common Scenarios

### Meeting Preparation
1. Get initial reminder (15 min before)
2. Snooze to "5 min before event" 
3. Get final reminder to join the meeting

### All-Day Events
- Notifications appear at the configured default time
- Can be filtered out with `ShowOnlyWorkingHours` if desired

### Recurring Meetings
- Each occurrence gets its own notification
- Snooze settings don't affect future occurrences

## Troubleshooting

### No Notifications Appearing
1. Check that you're logged in to Microsoft 365
2. Verify `NotificationsEnabled` is `true`
3. Ensure events have reminder times set in Outlook
4. Check if `ShowOnlyWorkingHours` is filtering events

### Too Many Notifications
1. Reduce `MaxNotificationsPerEvent` to limit snoozes
2. Enable `AutoDismissNotifications` for automatic cleanup
3. Use `ShowOnlyWorkingHours` to filter by time

### Missed Notifications
- The app automatically recovers notifications that should have appeared while it was closed
- Notifications for events that ended more than 30 minutes ago are not recovered

## Configuration File Location

Settings are stored in: `%APPDATA%\M365CalendarApp\appsettings.json`

You can edit this file directly or through the application settings interface.

## Need More Help?

See the complete [NOTIFICATIONS.md](NOTIFICATIONS.md) documentation for advanced configuration options and technical details.