using System.IO;
using System.Text.Json;

namespace M365CalendarApp.WPF.Services;

public class ConfigurationService
{
    private const string ConfigFileName = "appsettings.json";
    private AppConfiguration? _configuration;

    public async Task<AppConfiguration> LoadConfigurationAsync()
    {
        if (_configuration != null)
            return _configuration;

        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            
            if (File.Exists(configPath))
            {
                var jsonContent = await File.ReadAllTextAsync(configPath);
                _configuration = JsonSerializer.Deserialize<AppConfiguration>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            
            _configuration ??= GetDefaultConfiguration();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load configuration: {ex.Message}");
            _configuration = GetDefaultConfiguration();
        }

        return _configuration;
    }

    public async Task SaveConfigurationAsync(AppConfiguration configuration)
    {
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            var jsonContent = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await File.WriteAllTextAsync(configPath, jsonContent);
            _configuration = configuration;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save configuration: {ex.Message}");
        }
    }

    private AppConfiguration GetDefaultConfiguration()
    {
        return new AppConfiguration
        {
            Authentication = new AuthenticationConfig
            {
                ClientId = "9b0059d1-a22e-4ed9-854b-b3304df51816",
                TenantId = "common",
                Scopes = new[]
                {
                    "https://graph.microsoft.com/Calendars.Read",
                    "https://graph.microsoft.com/User.Read"
                }
            },
            Proxy = new ProxyConfig
            {
                UseSystemProxy = true,
                CustomProxy = new CustomProxyConfig
                {
                    Enabled = false,
                    Address = "",
                    Username = "",
                    Password = ""
                }
            },
            Application = new ApplicationConfig
            {
                StartWithWindows = false,
                StayOnTop = true,
                DefaultTheme = "Light",
                DefaultZoom = 1.0,
                WindowSize = new WindowSizeConfig { Width = 400, Height = 600 },
                WindowPosition = new WindowPositionConfig { X = -1, Y = -1 }
            },
            Calendar = new CalendarConfig
            {
                RefreshIntervalMinutes = 15,
                ShowAllDayEvents = true,
                ShowPrivateEvents = true,
                TimeFormat = "12h"
            }
        };
    }
}

public class AppConfiguration
{
    public AuthenticationConfig Authentication { get; set; } = new();
    public ProxyConfig Proxy { get; set; } = new();
    public ApplicationConfig Application { get; set; } = new();
    public CalendarConfig Calendar { get; set; } = new();
    public NotificationConfig Notifications { get; set; } = new();
}

public class AuthenticationConfig
{
    public string ClientId { get; set; } = "";
    public string TenantId { get; set; } = "common";
    public string[] Scopes { get; set; } = Array.Empty<string>();
}

public class ProxyConfig
{
    public bool UseSystemProxy { get; set; } = true;
    public CustomProxyConfig CustomProxy { get; set; } = new();
}

public class CustomProxyConfig
{
    public bool Enabled { get; set; }
    public string Address { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class ApplicationConfig
{
    public bool StartWithWindows { get; set; }
    public bool StayOnTop { get; set; } = true;
    public string DefaultTheme { get; set; } = "Light";
    public double DefaultZoom { get; set; } = 1.0;
    public WindowSizeConfig WindowSize { get; set; } = new();
    public WindowPositionConfig WindowPosition { get; set; } = new();
}

public class WindowSizeConfig
{
    public int Width { get; set; } = 400;
    public int Height { get; set; } = 600;
}

public class WindowPositionConfig
{
    public int X { get; set; } = -1;
    public int Y { get; set; } = -1;
}

public class CalendarConfig
{
    public int RefreshIntervalMinutes { get; set; } = 15;
    public bool ShowAllDayEvents { get; set; } = true;
    public bool ShowPrivateEvents { get; set; } = true;
    public string TimeFormat { get; set; } = "12h";
}

public class NotificationConfig
{
    public bool NotificationsEnabled { get; set; } = true;
    public bool PlayNotificationSound { get; set; } = true;
    public int DefaultReminderMinutes { get; set; } = 15;
    public bool ShowOnlyWorkingHours { get; set; } = false;
    public TimeSpan WorkingHoursStart { get; set; } = new TimeSpan(9, 0, 0);
    public TimeSpan WorkingHoursEnd { get; set; } = new TimeSpan(17, 0, 0);
    public DayOfWeek[]? WorkingDays { get; set; } = new[]
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    };
    public int MaxNotificationsPerEvent { get; set; } = 3;
    public bool AutoDismissNotifications { get; set; } = false;
    public int AutoDismissMinutes { get; set; } = 10;
}