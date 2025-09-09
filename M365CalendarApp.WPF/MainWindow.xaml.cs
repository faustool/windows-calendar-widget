using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using M365CalendarApp.WPF.Services;

namespace M365CalendarApp.WPF;

public partial class MainWindow : Window
{
    private bool _isDarkTheme = false;
    private double _zoomFactor = 1.0;
    private DateTime _currentDate = DateTime.Today;
    private readonly AuthenticationService _authService;
    private readonly CalendarService _calendarService;
    private readonly ConfigurationService _configService;
    private bool _isLoading = false;

    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize services
        _configService = new ConfigurationService();
        _authService = new AuthenticationService();
        _calendarService = new CalendarService(_authService);
        
        InitializeWindow();
        UpdateCurrentDateDisplay();
        
        // Enable touch and manipulation
        this.IsManipulationEnabled = true;
        this.ManipulationDelta += MainWindow_ManipulationDelta;
        this.ManipulationStarting += MainWindow_ManipulationStarting;
        this.ManipulationCompleted += MainWindow_ManipulationCompleted;
        
        // Enable mouse wheel for zoom
        this.PreviewMouseWheel += MainWindow_PreviewMouseWheel;
        
        // Enable touch for navigation arrows
        PreviousDayButton.IsManipulationEnabled = true;
        NextDayButton.IsManipulationEnabled = true;
        
        // Initialize services asynchronously
        _ = InitializeServicesAsync();
    }

    private void InitializeWindow()
    {
        // Set initial theme
        ApplyTheme(_isDarkTheme);
        
        // Update zoom display
        UpdateZoomDisplay();
        
        // Set window to stay on top
        this.Topmost = true;
    }

    private void UpdateCurrentDateDisplay()
    {
        CurrentDateText.Text = _currentDate.ToString("dddd, MMMM d, yyyy");
    }

    private void ApplyTheme(bool isDark)
    {
        var themeKey = isDark ? "DarkTheme" : "LightTheme";
        var theme = (ResourceDictionary)Application.Current.Resources[themeKey];
        
        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(theme);
        
        // Update theme toggle button
        ThemeToggleButton.Content = isDark ? "☀️" : "🌙";
    }

    private void UpdateZoomDisplay()
    {
        ZoomText.Text = $"{(int)(_zoomFactor * 100)}%";
        
        // Apply zoom to the main content
        var transform = new ScaleTransform(_zoomFactor, _zoomFactor);
        EventsScrollViewer.LayoutTransform = transform;
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        ApplyTheme(_isDarkTheme);
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (_authService.IsAuthenticated)
        {
            // Logout
            await LogoutAsync();
        }
        else
        {
            // Login
            await LoginAsync();
        }
    }

    private void PreviousDayButton_Click(object sender, RoutedEventArgs e)
    {
        _currentDate = _currentDate.AddDays(-1);
        UpdateCurrentDateDisplay();
        LoadCalendarEvents();
    }

    private void NextDayButton_Click(object sender, RoutedEventArgs e)
    {
        _currentDate = _currentDate.AddDays(1);
        UpdateCurrentDateDisplay();
        LoadCalendarEvents();
    }

    private async void LoadCalendarEvents()
    {
        if (_isLoading) return;
        
        _isLoading = true;
        StatusText.Text = $"Loading events for {_currentDate:MM/dd/yyyy}...";
        
        try
        {
            // Clear existing events
            EventsPanel.Children.Clear();
            
            if (_authService.IsAuthenticated)
            {
                var events = await _calendarService.GetEventsForDateAsync(_currentDate);
                
                if (events.Any())
                {
                    foreach (var eventInfo in events)
                    {
                        AddCalendarEvent(eventInfo);
                    }
                    StatusText.Text = $"Loaded {events.Count} event(s) for {_currentDate:MM/dd/yyyy}";
                }
                else
                {
                    AddNoEventsMessage();
                    StatusText.Text = $"No events found for {_currentDate:MM/dd/yyyy}";
                }
            }
            else
            {
                AddLoginPrompt();
                StatusText.Text = "Please login to view calendar events";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error loading events: {ex.Message}";
            AddErrorMessage(ex.Message);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void AddPlaceholderEvent()
    {
        var eventBorder = new Border
        {
            Style = (Style)FindResource("CalendarItemStyle")
        };

        var eventPanel = new StackPanel();
        
        var titleText = new TextBlock
        {
            Text = "No events scheduled",
            FontSize = 14 * _zoomFactor,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("ForegroundBrush")
        };
        
        var timeText = new TextBlock
        {
            Text = "Click Login to connect to your calendar",
            FontSize = 12 * _zoomFactor,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            Opacity = 0.7,
            Margin = new Thickness(0, 2, 0, 0)
        };
        
        eventPanel.Children.Add(titleText);
        eventPanel.Children.Add(timeText);
        eventBorder.Child = eventPanel;
        
        EventsPanel.Children.Add(eventBorder);
    }

    private void MainWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Check if Ctrl is pressed for zoom
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            
            double zoomDelta = e.Delta > 0 ? 0.1 : -0.1;
            _zoomFactor = Math.Max(0.5, Math.Min(2.0, _zoomFactor + zoomDelta));
            
            UpdateZoomDisplay();
            RefreshEventDisplay();
        }
    }

    private void MainWindow_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
    {
        e.ManipulationContainer = this;
        e.Handled = true;
    }

    private void MainWindow_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
    {
        // Handle touch zoom (pinch gesture)
        if (Math.Abs(e.DeltaManipulation.Scale.X - 1.0) > 0.01 || Math.Abs(e.DeltaManipulation.Scale.Y - 1.0) > 0.01)
        {
            double scaleFactor = (e.DeltaManipulation.Scale.X + e.DeltaManipulation.Scale.Y) / 2.0;
            _zoomFactor = Math.Max(0.5, Math.Min(2.0, _zoomFactor * scaleFactor));
            
            UpdateZoomDisplay();
            RefreshEventDisplay();
            e.Handled = true;
        }
        
        // Handle swipe gestures for date navigation
        if (Math.Abs(e.DeltaManipulation.Translation.X) > 50 && Math.Abs(e.DeltaManipulation.Translation.Y) < 30)
        {
            if (e.DeltaManipulation.Translation.X > 0)
            {
                // Swipe right - go to previous day
                _currentDate = _currentDate.AddDays(-1);
                UpdateCurrentDateDisplay();
                LoadCalendarEvents();
            }
            else
            {
                // Swipe left - go to next day
                _currentDate = _currentDate.AddDays(1);
                UpdateCurrentDateDisplay();
                LoadCalendarEvents();
            }
            e.Handled = true;
        }
    }

    private void MainWindow_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
    {
        // Handle final swipe velocity for smoother navigation
        if (Math.Abs(e.FinalVelocities.LinearVelocity.X) > 500)
        {
            if (e.FinalVelocities.LinearVelocity.X > 0)
            {
                // Fast swipe right - go to previous day
                _currentDate = _currentDate.AddDays(-1);
            }
            else
            {
                // Fast swipe left - go to next day
                _currentDate = _currentDate.AddDays(1);
            }
            UpdateCurrentDateDisplay();
            LoadCalendarEvents();
        }
        e.Handled = true;
    }

    private void RefreshEventDisplay()
    {
        // Refresh the event display with new zoom factor
        foreach (Border eventBorder in EventsPanel.Children.OfType<Border>())
        {
            if (eventBorder.Child is StackPanel panel)
            {
                foreach (TextBlock textBlock in panel.Children.OfType<TextBlock>())
                {
                    if (textBlock.FontWeight == FontWeights.SemiBold)
                    {
                        textBlock.FontSize = 14 * _zoomFactor;
                    }
                    else
                    {
                        textBlock.FontSize = 12 * _zoomFactor;
                    }
                }
            }
        }
    }

    private async Task InitializeServicesAsync()
    {
        try
        {
            // Configure proxy settings
            ProxyService.ConfigureProxy();
            
            // Initialize authentication service
            var authInitialized = await _authService.InitializeAsync();
            if (authInitialized)
            {
                UpdateLoginButton();
                
                if (_authService.IsAuthenticated)
                {
                    await _calendarService.InitializeAsync();
                    LoadCalendarEvents();
                }
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Initialization error: {ex.Message}";
        }
    }

    private async Task LoginAsync()
    {
        try
        {
            StatusText.Text = "Signing in...";
            LoginButton.IsEnabled = false;
            
            var success = await _authService.LoginAsync();
            if (success)
            {
                await _calendarService.InitializeAsync();
                UpdateLoginButton();
                LoadCalendarEvents();
                
                var user = await _calendarService.GetCurrentUserAsync();
                if (user != null)
                {
                    StatusText.Text = $"Signed in as {user.DisplayName}";
                }
            }
            else
            {
                StatusText.Text = "Sign in failed";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Sign in error: {ex.Message}";
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private async Task LogoutAsync()
    {
        try
        {
            StatusText.Text = "Signing out...";
            LoginButton.IsEnabled = false;
            
            await _authService.LogoutAsync();
            UpdateLoginButton();
            
            // Clear events and show login prompt
            EventsPanel.Children.Clear();
            AddLoginPrompt();
            
            StatusText.Text = "Signed out";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Sign out error: {ex.Message}";
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void UpdateLoginButton()
    {
        if (_authService.IsAuthenticated)
        {
            LoginButton.Content = "Logout";
            var userName = _authService.GetUserDisplayName();
            if (!string.IsNullOrEmpty(userName))
            {
                LoginButton.ToolTip = $"Signed in as {userName}";
            }
        }
        else
        {
            LoginButton.Content = "Login";
            LoginButton.ToolTip = "Sign in with Microsoft Account";
        }
    }

    private void AddCalendarEvent(CalendarEventInfo eventInfo)
    {
        var eventBorder = new Border
        {
            Style = (Style)FindResource("CalendarItemStyle")
        };

        // Add colored left border based on event status
        var colorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(eventInfo.GetStatusColor()));
        eventBorder.BorderBrush = colorBrush;
        eventBorder.BorderThickness = new Thickness(4, 1, 1, 1);

        var eventPanel = new StackPanel();
        
        // Event title
        var titleText = new TextBlock
        {
            Text = eventInfo.Subject,
            FontSize = 14 * _zoomFactor,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            TextWrapping = TextWrapping.Wrap
        };
        
        // Event time
        var timeText = new TextBlock
        {
            Text = eventInfo.GetTimeDisplay(),
            FontSize = 12 * _zoomFactor,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            Opacity = 0.7,
            Margin = new Thickness(0, 2, 0, 0)
        };

        // Event location (if available)
        if (!string.IsNullOrEmpty(eventInfo.Location))
        {
            var locationText = new TextBlock
            {
                Text = $"📍 {eventInfo.Location}",
                FontSize = 11 * _zoomFactor,
                Foreground = (Brush)FindResource("ForegroundBrush"),
                Opacity = 0.6,
                Margin = new Thickness(0, 1, 0, 0)
            };
            eventPanel.Children.Add(locationText);
        }

        // Event duration and attendees info
        var infoPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
        
        var durationText = new TextBlock
        {
            Text = eventInfo.GetDurationDisplay(),
            FontSize = 10 * _zoomFactor,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            Opacity = 0.5
        };
        infoPanel.Children.Add(durationText);

        if (eventInfo.AttendeesCount > 0)
        {
            var attendeesText = new TextBlock
            {
                Text = $" • {eventInfo.AttendeesCount} attendee(s)",
                FontSize = 10 * _zoomFactor,
                Foreground = (Brush)FindResource("ForegroundBrush"),
                Opacity = 0.5
            };
            infoPanel.Children.Add(attendeesText);
        }

        eventPanel.Children.Add(titleText);
        eventPanel.Children.Add(timeText);
        eventPanel.Children.Add(infoPanel);
        
        eventBorder.Child = eventPanel;
        
        // Add click handler for event details
        eventBorder.MouseLeftButtonUp += (s, e) => ShowEventDetails(eventInfo);
        
        EventsPanel.Children.Add(eventBorder);
    }

    private void AddNoEventsMessage()
    {
        var eventBorder = new Border
        {
            Style = (Style)FindResource("CalendarItemStyle"),
            Opacity = 0.7
        };

        var eventPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        
        var titleText = new TextBlock
        {
            Text = "No events scheduled",
            FontSize = 14 * _zoomFactor,
            FontWeight = FontWeights.Normal,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        var subtitleText = new TextBlock
        {
            Text = "Enjoy your free time! 🎉",
            FontSize = 12 * _zoomFactor,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            Opacity = 0.7,
            Margin = new Thickness(0, 2, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        eventPanel.Children.Add(titleText);
        eventPanel.Children.Add(subtitleText);
        eventBorder.Child = eventPanel;
        
        EventsPanel.Children.Add(eventBorder);
    }

    private void AddLoginPrompt()
    {
        var eventBorder = new Border
        {
            Style = (Style)FindResource("CalendarItemStyle"),
            Background = (Brush)FindResource("AccentBrush"),
            Opacity = 0.9
        };

        var eventPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        
        var titleText = new TextBlock
        {
            Text = "Connect to Microsoft 365",
            FontSize = 14 * _zoomFactor,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        var subtitleText = new TextBlock
        {
            Text = "Click the Login button to view your calendar",
            FontSize = 12 * _zoomFactor,
            Foreground = Brushes.White,
            Opacity = 0.9,
            Margin = new Thickness(0, 2, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        eventPanel.Children.Add(titleText);
        eventPanel.Children.Add(subtitleText);
        eventBorder.Child = eventPanel;
        
        EventsPanel.Children.Add(eventBorder);
    }

    private void AddErrorMessage(string error)
    {
        var eventBorder = new Border
        {
            Style = (Style)FindResource("CalendarItemStyle"),
            BorderBrush = Brushes.Red,
            BorderThickness = new Thickness(2)
        };

        var eventPanel = new StackPanel();
        
        var titleText = new TextBlock
        {
            Text = "Error loading calendar",
            FontSize = 14 * _zoomFactor,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.Red
        };
        
        var errorText = new TextBlock
        {
            Text = error,
            FontSize = 11 * _zoomFactor,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            Opacity = 0.7,
            Margin = new Thickness(0, 2, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        
        eventPanel.Children.Add(titleText);
        eventPanel.Children.Add(errorText);
        eventBorder.Child = eventPanel;
        
        EventsPanel.Children.Add(eventBorder);
    }

    private void ShowEventDetails(CalendarEventInfo eventInfo)
    {
        var details = $"Subject: {eventInfo.Subject}\n" +
                     $"Time: {eventInfo.GetTimeDisplay()}\n" +
                     $"Duration: {eventInfo.GetDurationDisplay()}";
        
        if (!string.IsNullOrEmpty(eventInfo.Location))
        {
            details += $"\nLocation: {eventInfo.Location}";
        }
        
        if (!string.IsNullOrEmpty(eventInfo.OrganizerName))
        {
            details += $"\nOrganizer: {eventInfo.OrganizerName}";
        }
        
        if (eventInfo.AttendeesCount > 0)
        {
            details += $"\nAttendees: {eventInfo.AttendeesCount}";
        }
        
        if (!string.IsNullOrEmpty(eventInfo.BodyPreview))
        {
            details += $"\n\nPreview: {eventInfo.BodyPreview}";
        }

        MessageBox.Show(details, "Event Details", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_configService)
        {
            Owner = this
        };
        
        var result = settingsWindow.ShowDialog();
        if (result == true)
        {
            // Reload configuration and apply changes
            _ = ApplyConfigurationChangesAsync();
        }
    }

    private async Task ApplyConfigurationChangesAsync()
    {
        try
        {
            var config = await _configService.LoadConfigurationAsync();
            
            // Apply stay on top setting
            this.Topmost = config.Application.StayOnTop;
            
            // Apply theme setting
            var isDark = config.Application.DefaultTheme.ToLower() == "dark";
            if (isDark != _isDarkTheme)
            {
                _isDarkTheme = isDark;
                ApplyTheme(_isDarkTheme);
            }
            
            // Apply zoom setting
            if (Math.Abs(config.Application.DefaultZoom - _zoomFactor) > 0.01)
            {
                _zoomFactor = config.Application.DefaultZoom;
                UpdateZoomDisplay();
                RefreshEventDisplay();
            }
            
            StatusText.Text = "Settings applied successfully";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error applying settings: {ex.Message}";
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        
        // Enable touch manipulation for the window
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            // Additional touch setup can be done here if needed
        }
    }
}