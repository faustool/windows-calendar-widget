using M365CalendarApp.WPF.Services;

namespace M365CalendarApp.Tests.Fixtures;

/// <summary>
/// Provides test data fixtures for consistent testing
/// </summary>
public static class TestDataFixtures
{
    /// <summary>
    /// Default test configuration
    /// </summary>
    public static AppConfiguration DefaultConfiguration => new()
    {
        Authentication = new AuthenticationConfig
        {
            ClientId = "test-client-id",
            TenantId = "common",
            Scopes = new[] { "https://graph.microsoft.com/Calendars.Read", "https://graph.microsoft.com/User.Read" }
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

    /// <summary>
    /// Configuration with custom proxy settings
    /// </summary>
    public static AppConfiguration ProxyConfiguration => new()
    {
        Authentication = DefaultConfiguration.Authentication,
        Proxy = new ProxyConfig
        {
            UseSystemProxy = false,
            CustomProxy = new CustomProxyConfig
            {
                Enabled = true,
                Address = "http://proxy.company.com:8080",
                Username = "testuser",
                Password = "testpass"
            }
        },
        Application = DefaultConfiguration.Application,
        Calendar = DefaultConfiguration.Calendar
    };

    /// <summary>
    /// Configuration with dark theme
    /// </summary>
    public static AppConfiguration DarkThemeConfiguration
    {
        get
        {
            var config = DefaultConfiguration;
            config.Application.DefaultTheme = "Dark";
            return config;
        }
    }

    /// <summary>
    /// Invalid configuration for error testing
    /// </summary>
    public static AppConfiguration InvalidConfiguration => new()
    {
        Authentication = new AuthenticationConfig
        {
            ClientId = "", // Invalid empty client ID
            TenantId = "",
            Scopes = Array.Empty<string>()
        },
        Proxy = new ProxyConfig(),
        Application = new ApplicationConfig(),
        Calendar = new CalendarConfig()
    };

    /// <summary>
    /// Sample JSON configuration string
    /// </summary>
    public static string DefaultConfigurationJson => """
        {
          "authentication": {
            "clientId": "test-client-id",
            "tenantId": "common",
            "scopes": [
              "https://graph.microsoft.com/Calendars.Read",
              "https://graph.microsoft.com/User.Read"
            ]
          },
          "proxy": {
            "useSystemProxy": true,
            "customProxy": {
              "enabled": false,
              "address": "",
              "username": "",
              "password": ""
            }
          },
          "application": {
            "startWithWindows": false,
            "stayOnTop": true,
            "defaultTheme": "Light",
            "defaultZoom": 1.0,
            "windowSize": {
              "width": 400,
              "height": 600
            },
            "windowPosition": {
              "x": -1,
              "y": -1
            }
          },
          "calendar": {
            "refreshIntervalMinutes": 15,
            "showAllDayEvents": true,
            "showPrivateEvents": true,
            "timeFormat": "12h"
          }
        }
        """;

    /// <summary>
    /// Invalid JSON for error testing
    /// </summary>
    public static string InvalidJson => """
        {
          "authentication": {
            "clientId": "test-client-id",
            "tenantId": "common"
            // Missing comma and closing brace
        """;

    /// <summary>
    /// Sample calendar event data
    /// </summary>
    public static class CalendarEvents
    {
        public static readonly DateTime TestDate = new(2024, 12, 9);
        
        public static readonly (string Subject, DateTime Start, DateTime End, string Location)[] SampleEvents = 
        {
            ("Morning Standup", TestDate.AddHours(9), TestDate.AddHours(9.5), "Conference Room A"),
            ("Project Review", TestDate.AddHours(11), TestDate.AddHours(12), "Meeting Room 1"),
            ("Lunch Meeting", TestDate.AddHours(12.5), TestDate.AddHours(13.5), "Restaurant"),
            ("Development Session", TestDate.AddHours(14), TestDate.AddHours(16), "Dev Lab"),
            ("Team Retrospective", TestDate.AddHours(16.5), TestDate.AddHours(17.5), "Conference Room B")
        };

        public static readonly (string Subject, DateTime Start, DateTime End, bool IsAllDay)[] AllDayEvents =
        {
            ("Company Holiday", TestDate, TestDate.AddDays(1), true),
            ("Conference Day", TestDate.AddDays(1), TestDate.AddDays(2), true)
        };
    }

    /// <summary>
    /// Test user data
    /// </summary>
    public static class Users
    {
        public static readonly (string DisplayName, string Email, string JobTitle)[] SampleUsers =
        {
            ("John Doe", "john.doe@company.com", "Software Engineer"),
            ("Jane Smith", "jane.smith@company.com", "Product Manager"),
            ("Bob Johnson", "bob.johnson@company.com", "Team Lead")
        };
    }

    /// <summary>
    /// Environment variables for proxy testing
    /// </summary>
    public static class EnvironmentVariables
    {
        public static readonly Dictionary<string, string> ProxySettings = new()
        {
            { "HTTPS_PROXY", "https://proxy.company.com:8080" },
            { "HTTP_PROXY", "http://proxy.company.com:8080" },
            { "PROXY_USER", "testuser" },
            { "PROXY_PASS", "testpass" }
        };

        public static readonly Dictionary<string, string> NoProxySettings = new()
        {
            { "HTTPS_PROXY", "" },
            { "HTTP_PROXY", "" },
            { "PROXY_USER", "" },
            { "PROXY_PASS", "" }
        };
    }
}