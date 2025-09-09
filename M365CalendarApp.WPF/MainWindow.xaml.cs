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
    private readonly NotificationManager _notificationManager;
    private bool _isLoading = false;

    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize services
        _configService = new ConfigurationService();
        _authService = new AuthenticationService(_configService);
        _calendarService = new CalendarService(_authService);
        _notificationManager = new NotificationManager(_calendarService, _configService);
        
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
        try
        {
            var themeKey = isDark ? "DarkTheme" : "LightTheme";
            System.Diagnostics.Debug.WriteLine($"Applying theme: {themeKey}");
            
            var theme = (ResourceDictionary)Application.Current.Resources[themeKey];
            
            if (theme != null)
            {
                System.Diagnostics.Debug.WriteLine($"Found theme dictionary with {theme.Keys.Count} keys");
                
                // Update the main resource dictionary with theme colors
                foreach (var key in theme.Keys)
                {
                    var oldValue = Application.Current.Resources[key];
                    Application.Current.Resources[key] = theme[key];
                    System.Diagnostics.Debug.WriteLine($"Updated {key}: {oldValue} -> {theme[key]}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Theme dictionary '{themeKey}' not found!");
            }
            
            // Update theme toggle button
            ThemeToggleButton.Content = isDark ? "☀️" : "🌙";
            
            // Refresh the event display to apply new theme colors
            RefreshEventDisplay();
            
            // Force reload events to ensure they use new theme colors
            if (_authService.IsAuthenticated)
            {
                LoadCalendarEvents();
            }
            
            System.Diagnostics.Debug.WriteLine($"Theme applied successfully. Button content: {ThemeToggleButton.Content}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
        }
    }

    private void UpdateZoomDisplay()
    {
        ZoomText.Text = $"{(int)(_zoomFactor * 100)}%";
        
        // Apply zoom to the main content
        var transform = new ScaleTransform(_zoomFactor, _zoomFactor);
        EventsScrollViewer.LayoutTransform = transform;
    }

    private async void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        ApplyTheme(_isDarkTheme);
        
        // Save theme preference
        try
        {
            var config = await _configService.LoadConfigurationAsync();
            config.Application.DefaultTheme = _isDarkTheme ? "Dark" : "Light";
            await _configService.SaveConfigurationAsync(config);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving theme preference: {ex.Message}");
        }
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

                // Update notifications for the current week when events are loaded
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationManager.RefreshNotificationsAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error refreshing notifications: {ex.Message}");
                    }
                });
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
            RefreshElementZoom(eventBorder);
        }
    }

    private void RefreshElementZoom(DependencyObject element)
    {
        // Recursively update font sizes, button sizes, and theme colors
        if (element is TextBlock textBlock)
        {
            // Update font sizes for zoom
            if (textBlock.FontWeight == FontWeights.SemiBold)
            {
                textBlock.FontSize = 14 * _zoomFactor;
            }
            else if (textBlock.FontSize >= 12)
            {
                textBlock.FontSize = 12 * _zoomFactor;
            }
            else if (textBlock.FontSize >= 11)
            {
                textBlock.FontSize = 11 * _zoomFactor;
            }
            else
            {
                textBlock.FontSize = 10 * _zoomFactor;
            }
            
            // Update max height for body text
            if (textBlock.MaxHeight > 0)
            {
                textBlock.MaxHeight = 60 * _zoomFactor;
            }
            
            // Update theme colors
            try
            {
                textBlock.Foreground = (Brush)Application.Current.Resources["ForegroundBrush"];
            }
            catch { /* Ignore if resource not found */ }
        }
        else if (element is Button button)
        {
            button.FontSize = 10 * _zoomFactor;
            button.Width = 20 * _zoomFactor;
            button.Height = 20 * _zoomFactor;
            
            // Update theme colors
            try
            {
                button.Foreground = (Brush)Application.Current.Resources["ForegroundBrush"];
            }
            catch { /* Ignore if resource not found */ }
        }
        else if (element is Border border)
        {
            // Update border theme colors
            try
            {
                if (border.Style?.ToString().Contains("CalendarItemStyle") == true)
                {
                    border.Background = (Brush)Application.Current.Resources["SecondaryBrush"];
                    border.BorderBrush = (Brush)Application.Current.Resources["BorderBrush"];
                }
            }
            catch { /* Ignore if resource not found */ }
        }

        // Recursively process children
        int childCount = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(element, i);
            RefreshElementZoom(child);
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
                    
                    // Initialize notifications and recover any missed ones
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _notificationManager.RefreshNotificationsAsync();
                            // Recover notifications that should have been shown while app was closed
                            await Task.Delay(2000); // Wait a bit for the app to fully initialize
                            _notificationManager.GetNotificationService()?.RecoverMissedNotifications();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error initializing notifications: {ex.Message}");
                        }
                    });
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
                
                // Initialize notifications after successful login
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationManager.RefreshNotificationsAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error refreshing notifications after login: {ex.Message}");
                    }
                });
                
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

        // Main container with relative positioning for expand button
        var mainGrid = new Grid();
        
        // Main event content panel
        var eventPanel = new StackPanel();
        
        // Header with title and expand button
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        // Event title
        var titleText = new TextBlock
        {
            Text = eventInfo.Subject,
            FontSize = 14 * _zoomFactor,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(titleText, 0);
        
        // Expand button
        var expandButton = new Button
        {
            Content = "▼",
            FontSize = 10 * _zoomFactor,
            Width = 20 * _zoomFactor,
            Height = 20 * _zoomFactor,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = (Brush)FindResource("ForegroundBrush"),
            Opacity = 0.6,
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(5, 0, 0, 0),
            ToolTip = "Expand for details"
        };
        Grid.SetColumn(expandButton, 1);
        
        headerGrid.Children.Add(titleText);
        headerGrid.Children.Add(expandButton);
        
        // Event time
        var timeText = new TextBlock
        {
            Text = eventInfo.GetTimeDisplay(),
            FontSize = 12 * _zoomFactor,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            Opacity = 0.7,
            Margin = new Thickness(0, 2, 0, 0)
        };

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

        // Expandable details panel (initially collapsed)
        var detailsPanel = new StackPanel
        {
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 8, 0, 0)
        };

        // Create details grid for organized layout
        var detailsGrid = new Grid();
        detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        detailsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        detailsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Organizer section
        var organizerPanel = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        var organizerLabel = new TextBlock
        {
            Text = "Organizer:",
            FontSize = 11 * _zoomFactor,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            Opacity = 0.8
        };
        var organizerValue = new TextBlock
        {
            Text = !string.IsNullOrEmpty(eventInfo.OrganizerName) ? eventInfo.OrganizerName : "Not specified",
            FontSize = 10 * _zoomFactor,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            Opacity = 0.7,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 0)
        };
        organizerPanel.Children.Add(organizerLabel);
        organizerPanel.Children.Add(organizerValue);
        Grid.SetColumn(organizerPanel, 0);
        Grid.SetRow(organizerPanel, 0);

        // Location section
        var locationPanel = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
        var locationLabel = new TextBlock
        {
            Text = "Location:",
            FontSize = 11 * _zoomFactor,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            Opacity = 0.8
        };
        var locationValue = new TextBlock
        {
            Text = !string.IsNullOrEmpty(eventInfo.Location) ? eventInfo.Location : "Not specified",
            FontSize = 10 * _zoomFactor,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            Opacity = 0.7,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 0)
        };
        locationPanel.Children.Add(locationLabel);
        locationPanel.Children.Add(locationValue);
        Grid.SetColumn(locationPanel, 1);
        Grid.SetRow(locationPanel, 0);

        // Body section (spans both columns)
        var bodyPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        var bodyLabel = new TextBlock
        {
            Text = "Body:",
            FontSize = 11 * _zoomFactor,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            Opacity = 0.8
        };
        var bodyValue = new TextBlock
        {
            Text = !string.IsNullOrEmpty(eventInfo.BodyPreview) ? eventInfo.BodyPreview : "No description available",
            FontSize = 10 * _zoomFactor,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            Opacity = 0.7,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 0),
            MaxHeight = 60 * _zoomFactor // Limit height to prevent excessive expansion
        };
        bodyPanel.Children.Add(bodyLabel);
        bodyPanel.Children.Add(bodyValue);
        Grid.SetColumn(bodyPanel, 0);
        Grid.SetRow(bodyPanel, 1);
        Grid.SetColumnSpan(bodyPanel, 2);

        detailsGrid.Children.Add(organizerPanel);
        detailsGrid.Children.Add(locationPanel);
        detailsGrid.Children.Add(bodyPanel);
        detailsPanel.Children.Add(detailsGrid);

        // Add all elements to main panel
        eventPanel.Children.Add(headerGrid);
        eventPanel.Children.Add(timeText);
        eventPanel.Children.Add(infoPanel);
        eventPanel.Children.Add(detailsPanel);
        
        mainGrid.Children.Add(eventPanel);
        eventBorder.Child = mainGrid;

        // Expand/collapse functionality
        bool isExpanded = false;
        expandButton.Click += (s, e) =>
        {
            e.Handled = true; // Prevent event bubbling
            isExpanded = !isExpanded;
            
            if (isExpanded)
            {
                detailsPanel.Visibility = Visibility.Visible;
                expandButton.Content = "▲";
                expandButton.ToolTip = "Collapse details";
            }
            else
            {
                detailsPanel.Visibility = Visibility.Collapsed;
                expandButton.Content = "▼";
                expandButton.ToolTip = "Expand for details";
            }
        };

        // Add click handler for event details (but not when clicking expand button)
        eventBorder.MouseLeftButtonUp += (s, e) =>
        {
            // Only show details dialog if not clicking on expand button
            if (e.OriginalSource != expandButton)
            {
                ShowEventDetails(eventInfo);
            }
        };
        
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

    protected override void OnClosed(EventArgs e)
    {
        try
        {
            // Clean up notification manager
            _notificationManager?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
        }
        
        base.OnClosed(e);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            // Save any pending notification state
            _notificationManager?.GetNotificationService()?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving notification state: {ex.Message}");
        }
        
        base.OnClosing(e);
    }
}