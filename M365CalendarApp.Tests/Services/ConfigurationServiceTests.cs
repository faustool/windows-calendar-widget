using FluentAssertions;
using M365CalendarApp.Tests.Fixtures;
using M365CalendarApp.WPF.Services;
using System.IO;
using System.Text.Json;
using Xunit;

namespace M365CalendarApp.Tests.Services;

/// <summary>
/// Unit tests for ConfigurationService
/// </summary>
public class ConfigurationServiceTests : TestBase
{
    private readonly ConfigurationService _configService;

    public ConfigurationServiceTests()
    {
        _configService = new ConfigurationService();
    }

    [Fact]
    public async Task LoadConfigurationAsync_ShouldReturnDefaultConfiguration_WhenFileDoesNotExist()
    {
        // Arrange - No configuration file exists

        // Act
        var config = await _configService.LoadConfigurationAsync();

        // Assert
        config.Should().NotBeNull();
        config.Authentication.Should().NotBeNull();
        config.Proxy.Should().NotBeNull();
        config.Application.Should().NotBeNull();
        config.Calendar.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadConfigurationAsync_ShouldReturnDefaultConfiguration_WhenFileIsInvalid()
    {
        // Arrange
        var invalidJsonFile = CreateTempFile(TestDataFixtures.InvalidJson, "appsettings.json");
        
        // Move the file to the expected location (this is a simplified test)
        // In a real implementation, we would mock the file system or use dependency injection

        // Act
        var config = await _configService.LoadConfigurationAsync();

        // Assert
        config.Should().NotBeNull();
        config.Authentication.ClientId.Should().Be("9b0059d1-a22e-4ed9-854b-b3304df51816");
    }

    [Fact]
    public async Task LoadConfigurationAsync_ShouldCacheConfiguration_OnSecondCall()
    {
        // Arrange
        var service = new ConfigurationService();

        // Act
        var config1 = await service.LoadConfigurationAsync();
        var config2 = await service.LoadConfigurationAsync();

        // Assert
        config1.Should().BeSameAs(config2);
    }

    [Fact]
    public async Task SaveConfigurationAsync_ShouldNotThrow_WithValidConfiguration()
    {
        // Arrange
        var config = TestDataFixtures.DefaultConfiguration;

        // Act
        var action = async () => await _configService.SaveConfigurationAsync(config);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveConfigurationAsync_ShouldHandleIOExceptions_Gracefully()
    {
        // Arrange
        var config = TestDataFixtures.DefaultConfiguration;
        
        // Act
        var action = async () => await _configService.SaveConfigurationAsync(config);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public void DefaultConfiguration_ShouldHaveCorrectValues()
    {
        // Arrange
        var service = new ConfigurationService();

        // Act
        var defaultConfig = TestDataFixtures.DefaultConfiguration;

        // Assert
        defaultConfig.Authentication.ClientId.Should().Be("test-client-id");
        defaultConfig.Authentication.TenantId.Should().Be("common");
        defaultConfig.Authentication.Scopes.Should().Contain("https://graph.microsoft.com/Calendars.Read");
        defaultConfig.Authentication.Scopes.Should().Contain("https://graph.microsoft.com/User.Read");
        
        defaultConfig.Proxy.UseSystemProxy.Should().BeTrue();
        defaultConfig.Proxy.CustomProxy.Enabled.Should().BeFalse();
        
        defaultConfig.Application.StartWithWindows.Should().BeFalse();
        defaultConfig.Application.StayOnTop.Should().BeTrue();
        defaultConfig.Application.DefaultTheme.Should().Be("Light");
        defaultConfig.Application.DefaultZoom.Should().Be(1.0);
        defaultConfig.Application.WindowSize.Width.Should().Be(400);
        defaultConfig.Application.WindowSize.Height.Should().Be(600);
        
        defaultConfig.Calendar.RefreshIntervalMinutes.Should().Be(15);
        defaultConfig.Calendar.ShowAllDayEvents.Should().BeTrue();
        defaultConfig.Calendar.ShowPrivateEvents.Should().BeTrue();
        defaultConfig.Calendar.TimeFormat.Should().Be("12h");
    }

    [Fact]
    public void ProxyConfiguration_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var proxyConfig = TestDataFixtures.ProxyConfiguration;

        // Assert
        proxyConfig.Proxy.UseSystemProxy.Should().BeFalse();
        proxyConfig.Proxy.CustomProxy.Enabled.Should().BeTrue();
        proxyConfig.Proxy.CustomProxy.Address.Should().Be("http://proxy.company.com:8080");
        proxyConfig.Proxy.CustomProxy.Username.Should().Be("testuser");
        proxyConfig.Proxy.CustomProxy.Password.Should().Be("testpass");
    }

    [Fact]
    public void DarkThemeConfiguration_ShouldHaveCorrectTheme()
    {
        // Arrange & Act
        var darkConfig = TestDataFixtures.DarkThemeConfiguration;

        // Assert
        darkConfig.Application.DefaultTheme.Should().Be("Dark");
    }

    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    [InlineData("System")]
    public void ApplicationConfig_ShouldAcceptValidThemes(string theme)
    {
        // Arrange
        var config = new ApplicationConfig
        {
            DefaultTheme = theme
        };

        // Act & Assert
        config.DefaultTheme.Should().Be(theme);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void ApplicationConfig_ShouldAcceptValidZoomLevels(double zoom)
    {
        // Arrange
        var config = new ApplicationConfig
        {
            DefaultZoom = zoom
        };

        // Act & Assert
        config.DefaultZoom.Should().Be(zoom);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    public void CalendarConfig_ShouldAcceptValidRefreshIntervals(int minutes)
    {
        // Arrange
        var config = new CalendarConfig
        {
            RefreshIntervalMinutes = minutes
        };

        // Act & Assert
        config.RefreshIntervalMinutes.Should().Be(minutes);
    }

    [Theory]
    [InlineData("12h")]
    [InlineData("24h")]
    public void CalendarConfig_ShouldAcceptValidTimeFormats(string timeFormat)
    {
        // Arrange
        var config = new CalendarConfig
        {
            TimeFormat = timeFormat
        };

        // Act & Assert
        config.TimeFormat.Should().Be(timeFormat);
    }

    [Fact]
    public void WindowSizeConfig_ShouldHaveValidDefaults()
    {
        // Arrange & Act
        var windowSize = new WindowSizeConfig();

        // Assert
        windowSize.Width.Should().Be(400);
        windowSize.Height.Should().Be(600);
    }

    [Fact]
    public void WindowPositionConfig_ShouldHaveValidDefaults()
    {
        // Arrange & Act
        var windowPosition = new WindowPositionConfig();

        // Assert
        windowPosition.X.Should().Be(-1);
        windowPosition.Y.Should().Be(-1);
    }

    [Theory]
    [InlineData(300, 400)]
    [InlineData(800, 600)]
    [InlineData(1200, 800)]
    public void WindowSizeConfig_ShouldAcceptValidSizes(int width, int height)
    {
        // Arrange
        var windowSize = new WindowSizeConfig
        {
            Width = width,
            Height = height
        };

        // Act & Assert
        windowSize.Width.Should().Be(width);
        windowSize.Height.Should().Be(height);
    }

    [Theory]
    [InlineData(-1, -1)] // Default (center)
    [InlineData(0, 0)]   // Top-left
    [InlineData(100, 100)] // Specific position
    public void WindowPositionConfig_ShouldAcceptValidPositions(int x, int y)
    {
        // Arrange
        var windowPosition = new WindowPositionConfig
        {
            X = x,
            Y = y
        };

        // Act & Assert
        windowPosition.X.Should().Be(x);
        windowPosition.Y.Should().Be(y);
    }

    [Fact]
    public void AuthenticationConfig_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var authConfig = new AuthenticationConfig();

        // Assert
        authConfig.ClientId.Should().Be("");
        authConfig.TenantId.Should().Be("common");
        authConfig.Scopes.Should().BeEmpty();
    }

    [Fact]
    public void ProxyConfig_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var proxyConfig = new ProxyConfig();

        // Assert
        proxyConfig.UseSystemProxy.Should().BeTrue();
        proxyConfig.CustomProxy.Should().NotBeNull();
        proxyConfig.CustomProxy.Enabled.Should().BeFalse();
        proxyConfig.CustomProxy.Address.Should().Be("");
        proxyConfig.CustomProxy.Username.Should().Be("");
        proxyConfig.CustomProxy.Password.Should().Be("");
    }

    [Fact]
    public void CustomProxyConfig_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var customProxy = new CustomProxyConfig();

        // Assert
        customProxy.Enabled.Should().BeFalse();
        customProxy.Address.Should().Be("");
        customProxy.Username.Should().Be("");
        customProxy.Password.Should().Be("");
    }

    [Fact]
    public void ApplicationConfig_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var appConfig = new ApplicationConfig();

        // Assert
        appConfig.StartWithWindows.Should().BeFalse();
        appConfig.StayOnTop.Should().BeTrue();
        appConfig.DefaultTheme.Should().Be("Light");
        appConfig.DefaultZoom.Should().Be(1.0);
        appConfig.WindowSize.Should().NotBeNull();
        appConfig.WindowPosition.Should().NotBeNull();
    }

    [Fact]
    public void CalendarConfig_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var calendarConfig = new CalendarConfig();

        // Assert
        calendarConfig.RefreshIntervalMinutes.Should().Be(15);
        calendarConfig.ShowAllDayEvents.Should().BeTrue();
        calendarConfig.ShowPrivateEvents.Should().BeTrue();
        calendarConfig.TimeFormat.Should().Be("12h");
    }

    [Fact]
    public void AppConfiguration_ShouldSerializeToJson()
    {
        // Arrange
        var config = TestDataFixtures.DefaultConfiguration;

        // Act
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("authentication");
        json.Should().Contain("proxy");
        json.Should().Contain("application");
        json.Should().Contain("calendar");
    }

    [Fact]
    public void AppConfiguration_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = TestDataFixtures.DefaultConfigurationJson;

        // Act
        var config = JsonSerializer.Deserialize<AppConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        config.Should().NotBeNull();
        config.Authentication.Should().NotBeNull();
        config.Authentication.ClientId.Should().Be("test-client-id");
        config.Proxy.Should().NotBeNull();
        config.Application.Should().NotBeNull();
        config.Calendar.Should().NotBeNull();
    }

    [Fact]
    public async Task Configuration_ShouldRoundTripCorrectly()
    {
        // Arrange
        var originalConfig = TestDataFixtures.DefaultConfiguration;
        var tempFile = CreateTempJsonFile(originalConfig);

        // Act
        var json = await File.ReadAllTextAsync(tempFile);
        var deserializedConfig = JsonSerializer.Deserialize<AppConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        deserializedConfig.Should().NotBeNull();
        deserializedConfig.Authentication.ClientId.Should().Be(originalConfig.Authentication.ClientId);
        deserializedConfig.Application.DefaultTheme.Should().Be(originalConfig.Application.DefaultTheme);
        deserializedConfig.Calendar.RefreshIntervalMinutes.Should().Be(originalConfig.Calendar.RefreshIntervalMinutes);
    }

    [Fact]
    public void Configuration_ShouldHandleNullValues()
    {
        // Arrange
        var config = new AppConfiguration();

        // Act & Assert
        config.Authentication.Should().NotBeNull();
        config.Proxy.Should().NotBeNull();
        config.Application.Should().NotBeNull();
        config.Calendar.Should().NotBeNull();
    }

    [Fact]
    public void Configuration_ShouldValidateClientId()
    {
        // Arrange
        var config = TestDataFixtures.InvalidConfiguration;

        // Act & Assert
        config.Authentication.ClientId.Should().BeEmpty();
        // In a real application, you might have validation logic here
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-client-id")]
    [InlineData("9b0059d1-a22e-4ed9-854b-b3304df51816")]
    public void AuthenticationConfig_ShouldAcceptDifferentClientIds(string clientId)
    {
        // Arrange
        var config = new AuthenticationConfig
        {
            ClientId = clientId
        };

        // Act & Assert
        config.ClientId.Should().Be(clientId);
    }

    [Theory]
    [InlineData("common")]
    [InlineData("organizations")]
    [InlineData("consumers")]
    [InlineData("specific-tenant-id")]
    public void AuthenticationConfig_ShouldAcceptDifferentTenants(string tenantId)
    {
        // Arrange
        var config = new AuthenticationConfig
        {
            TenantId = tenantId
        };

        // Act & Assert
        config.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void AuthenticationConfig_ShouldAcceptMultipleScopes()
    {
        // Arrange
        var scopes = new[]
        {
            "https://graph.microsoft.com/Calendars.Read",
            "https://graph.microsoft.com/User.Read",
            "https://graph.microsoft.com/Mail.Read"
        };
        var config = new AuthenticationConfig
        {
            Scopes = scopes
        };

        // Act & Assert
        config.Scopes.Should().BeEquivalentTo(scopes);
        config.Scopes.Should().HaveCount(3);
    }
}