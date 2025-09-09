using M365CalendarApp.WPF.Models;
using System;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace M365CalendarApp.WPF;

public partial class NotificationWindow : Window
{
    private readonly EventNotification _notification;
    private readonly DispatcherTimer _updateTimer;
    private readonly DispatcherTimer _autoCloseTimer;

    public event EventHandler<NotificationActionEventArgs>? NotificationAction;

    public NotificationWindow(EventNotification notification)
    {
        InitializeComponent();
        _notification = notification;

        // Set up update timer to refresh time until event
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();

        // Set up auto-close timer (10 minutes)
        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(10)
        };
        _autoCloseTimer.Tick += AutoCloseTimer_Tick;
        _autoCloseTimer.Start();

        InitializeNotification();
        PositionWindow();
        PlayNotificationSound();

        // Make window draggable
        this.MouseLeftButtonDown += NotificationWindow_MouseLeftButtonDown;
    }

    private void InitializeNotification()
    {
        EventSubjectText.Text = _notification.EventSubject;
        
        if (_notification.IsAllDay)
        {
            EventTimeText.Text = "All Day";
        }
        else
        {
            EventTimeText.Text = $"{_notification.EventStartTime:h:mm tt} - {_notification.EventEndTime:h:mm tt}";
        }

        if (!string.IsNullOrEmpty(_notification.EventLocation))
        {
            EventLocationText.Text = _notification.EventLocation;
            LocationPanel.Visibility = Visibility.Visible;
        }

        UpdateTimeUntilEvent();

        if (_notification.SnoozeCount > 0)
        {
            SnoozeCountText.Text = $"Snoozed {_notification.SnoozeCount} time{(_notification.SnoozeCount > 1 ? "s" : "")}";
            SnoozeCountText.Visibility = Visibility.Visible;
        }

        // Disable snooze options that would be in the past
        UpdateSnoozeButtonStates();
    }

    private void UpdateTimeUntilEvent()
    {
        TimeUntilEventText.Text = _notification.GetTimeUntilEventDisplay();
    }

    private void UpdateSnoozeButtonStates()
    {
        var now = DateTime.Now;
        
        // Disable "before event" options if they would be in the past
        Snooze5BeforeButton.IsEnabled = _notification.EventStartTime.AddMinutes(-5) > now;
        Snooze10BeforeButton.IsEnabled = _notification.EventStartTime.AddMinutes(-10) > now;

        // Disable all snooze options if event has already started
        if (_notification.EventStartTime <= now)
        {
            Snooze5BeforeButton.IsEnabled = false;
            Snooze10BeforeButton.IsEnabled = false;
        }
    }

    private void PositionWindow()
    {
        // Position in bottom-right corner of screen
        var workingArea = SystemParameters.WorkArea;
        this.Left = workingArea.Right - this.Width - 20;
        this.Top = workingArea.Bottom - this.Height - 20;

        // If there are other notification windows, stack them
        var existingWindows = 0;
        foreach (Window window in Application.Current.Windows)
        {
            if (window is NotificationWindow && window != this)
            {
                existingWindows++;
            }
        }

        if (existingWindows > 0)
        {
            this.Top -= (this.Height + 10) * existingWindows;
        }
    }

    private void PlayNotificationSound()
    {
        try
        {
            // Play system notification sound
            SystemSounds.Exclamation.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing notification sound: {ex.Message}");
        }
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        UpdateTimeUntilEvent();
        UpdateSnoozeButtonStates();
    }

    private void AutoCloseTimer_Tick(object? sender, EventArgs e)
    {
        // Auto-dismiss after 10 minutes
        HandleNotificationAction(Models.NotificationAction.Dismiss);
    }

    private void NotificationWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            this.DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        HandleNotificationAction(Models.NotificationAction.Dismiss);
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        HandleNotificationAction(Models.NotificationAction.Dismiss);
    }

    private void SnoozeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string actionString)
            return;

        if (Enum.TryParse<Models.NotificationAction>(actionString, out var action))
        {
            HandleNotificationAction(action);
        }
    }

    private void HandleNotificationAction(Models.NotificationAction action)
    {
        _updateTimer?.Stop();
        _autoCloseTimer?.Stop();

        NotificationAction?.Invoke(this, new NotificationActionEventArgs(_notification, action));
        
        this.Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _updateTimer?.Stop();
        _autoCloseTimer?.Stop();
        base.OnClosed(e);
    }

    // Handle Escape key to dismiss
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            HandleNotificationAction(Models.NotificationAction.Dismiss);
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    // Prevent window from being minimized
    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        base.OnStateChanged(e);
    }

    // Flash window to get attention
    public void FlashWindow()
    {
        try
        {
            this.Activate();
            this.Focus();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error flashing notification window: {ex.Message}");
        }
    }
}