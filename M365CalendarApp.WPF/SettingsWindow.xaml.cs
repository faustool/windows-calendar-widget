using System.Windows;
using System.Windows.Controls;
using M365CalendarApp.WPF.Services;

namespace M365CalendarApp.WPF;

public partial class SettingsWindow : Window
{
    private readonly ConfigurationService _configService;
    private AppConfiguration _configuration = null!;

    public SettingsWindow(ConfigurationService configService)
    {
        InitializeComponent();
        _configService = configService;
        
        // Load settings asynchronously after the window is loaded
        Loaded += async (s, e) => await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            _configuration = await _configService.LoadConfigurationAsync();
            
            // Load application settings
            StartWithWindowsCheckBox.IsChecked = StartupService.IsStartupEnabled();
            StayOnTopCheckBox.IsChecked = _configuration.Application.StayOnTop;
            
            // Set default theme
            var themeIndex = _configuration.Application.DefaultTheme.ToLower() switch
            {
                "dark" => 1,
                "system" => 2,
                _ => 0 // Light
            };
            DefaultThemeComboBox.SelectedIndex = themeIndex;
            
            // Load calendar settings
            RefreshIntervalComboBox.SelectedValue = _configuration.Calendar.RefreshIntervalMinutes;
            ShowAllDayEventsCheckBox.IsChecked = _configuration.Calendar.ShowAllDayEvents;
            ShowPrivateEventsCheckBox.IsChecked = _configuration.Calendar.ShowPrivateEvents;
            
            var timeFormatIndex = _configuration.Calendar.TimeFormat == "24h" ? 1 : 0;
            TimeFormatComboBox.SelectedIndex = timeFormatIndex;
            
            // Load authentication settings
            ClientIdTextBox.Text = _configuration.Authentication.ClientId;
            
            // Load proxy settings
            UseSystemProxyCheckBox.IsChecked = _configuration.Proxy.UseSystemProxy;
            ProxyAddressTextBox.Text = _configuration.Proxy.CustomProxy.Address;
            ProxyUsernameTextBox.Text = _configuration.Proxy.CustomProxy.Username;
            ProxyPasswordBox.Password = _configuration.Proxy.CustomProxy.Password;
            
            UpdateProxyPanelState();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading settings: {ex.Message}", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateProxyPanelState()
    {
        CustomProxyPanel.IsEnabled = UseSystemProxyCheckBox.IsChecked == false;
    }

    private void StartWithWindowsCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        StartupService.EnableStartup();
    }

    private void StartWithWindowsCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        StartupService.DisableStartup();
    }

    private void StayOnTopCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        _configuration.Application.StayOnTop = true;
    }

    private void StayOnTopCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        _configuration.Application.StayOnTop = false;
    }

    private void DefaultThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_configuration == null) return;
        
        var selectedTheme = DefaultThemeComboBox.SelectedIndex switch
        {
            1 => "Dark",
            2 => "System",
            _ => "Light"
        };
        
        _configuration.Application.DefaultTheme = selectedTheme;
    }

    private void RefreshIntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_configuration == null || RefreshIntervalComboBox.SelectedItem == null) return;
        
        if (RefreshIntervalComboBox.SelectedItem is ComboBoxItem item && 
            item.Tag is string tagValue && 
            int.TryParse(tagValue, out int interval))
        {
            _configuration.Calendar.RefreshIntervalMinutes = interval;
        }
    }

    private void ShowAllDayEventsCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (_configuration != null)
            _configuration.Calendar.ShowAllDayEvents = true;
    }

    private void ShowAllDayEventsCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_configuration != null)
            _configuration.Calendar.ShowAllDayEvents = false;
    }

    private void ShowPrivateEventsCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (_configuration != null)
            _configuration.Calendar.ShowPrivateEvents = true;
    }

    private void ShowPrivateEventsCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_configuration != null)
            _configuration.Calendar.ShowPrivateEvents = false;
    }

    private void TimeFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_configuration == null) return;
        
        var selectedFormat = TimeFormatComboBox.SelectedIndex == 1 ? "24h" : "12h";
        _configuration.Calendar.TimeFormat = selectedFormat;
    }

    private void ClientIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_configuration != null)
            _configuration.Authentication.ClientId = ClientIdTextBox.Text;
    }

    private void UseSystemProxyCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (_configuration != null)
            _configuration.Proxy.UseSystemProxy = true;
        UpdateProxyPanelState();
    }

    private void UseSystemProxyCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_configuration != null)
            _configuration.Proxy.UseSystemProxy = false;
        UpdateProxyPanelState();
    }

    private void ProxyAddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_configuration != null)
            _configuration.Proxy.CustomProxy.Address = ProxyAddressTextBox.Text;
    }

    private void ProxyUsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_configuration != null)
            _configuration.Proxy.CustomProxy.Username = ProxyUsernameTextBox.Text;
    }

    private void ProxyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_configuration != null)
            _configuration.Proxy.CustomProxy.Password = ProxyPasswordBox.Password;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _configService.SaveConfigurationAsync(_configuration);
            
            MessageBox.Show("Settings saved successfully. Some changes may require restarting the application.", 
                          "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}