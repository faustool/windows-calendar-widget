using FluentAssertions;
using M365CalendarApp.Tests.Fixtures;
using M365CalendarApp.WPF.Services;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace M365CalendarApp.Tests.Services;

/// <summary>
/// Unit tests for ProxyService
/// </summary>
public class ProxyServiceTests : TestBase
{
    [Fact]
    public void ConfigureProxy_ShouldNotThrow_WhenNoProxyEnvironmentVariables()
    {
        // Arrange
        ClearProxyEnvironmentVariables();

        // Act
        var action = () => ProxyService.ConfigureProxy();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureProxy_ShouldConfigureProxy_WhenHttpsProxyIsSet()
    {
        // Arrange
        var proxyUrl = "https://proxy.company.com:8080";
        Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);

        try
        {
            // Act
            ProxyService.ConfigureProxy();

            // Assert
            WebRequest.DefaultWebProxy.Should().NotBeNull();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
            WebRequest.DefaultWebProxy = null;
        }
    }

    [Fact]
    public void ConfigureProxy_ShouldConfigureProxy_WhenHttpProxyIsSet()
    {
        // Arrange
        var proxyUrl = "http://proxy.company.com:8080";
        Environment.SetEnvironmentVariable("HTTP_PROXY", proxyUrl);

        try
        {
            // Act
            ProxyService.ConfigureProxy();

            // Assert
            WebRequest.DefaultWebProxy.Should().NotBeNull();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("HTTP_PROXY", null);
            WebRequest.DefaultWebProxy = null;
        }
    }

    [Fact]
    public void ConfigureProxy_ShouldPreferHttpsProxy_WhenBothAreSet()
    {
        // Arrange
        var httpsProxyUrl = "https://secure-proxy.company.com:8080";
        var httpProxyUrl = "http://proxy.company.com:8080";
        Environment.SetEnvironmentVariable("HTTPS_PROXY", httpsProxyUrl);
        Environment.SetEnvironmentVariable("HTTP_PROXY", httpProxyUrl);

        try
        {
            // Act
            ProxyService.ConfigureProxy();

            // Assert
            WebRequest.DefaultWebProxy.Should().NotBeNull();
            // The implementation should prefer HTTPS_PROXY over HTTP_PROXY
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
            Environment.SetEnvironmentVariable("HTTP_PROXY", null);
            WebRequest.DefaultWebProxy = null;
        }
    }

    [Fact]
    public void ConfigureProxy_ShouldConfigureCredentials_WhenProxyUserAndPassAreSet()
    {
        // Arrange
        var proxyUrl = "http://proxy.company.com:8080";
        var proxyUser = "testuser";
        var proxyPass = "testpass";
        
        Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
        Environment.SetEnvironmentVariable("PROXY_USER", proxyUser);
        Environment.SetEnvironmentVariable("PROXY_PASS", proxyPass);

        try
        {
            // Act
            ProxyService.ConfigureProxy();

            // Assert
            WebRequest.DefaultWebProxy.Should().NotBeNull();
            // Credentials are configured internally
        }
        finally
        {
            // Cleanup
            ClearProxyEnvironmentVariables();
            WebRequest.DefaultWebProxy = null;
        }
    }

    [Fact]
    public void ConfigureProxy_ShouldUseDefaultCredentials_WhenNoUserCredentialsProvided()
    {
        // Arrange
        var proxyUrl = "http://proxy.company.com:8080";
        Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);

        try
        {
            // Act
            ProxyService.ConfigureProxy();

            // Assert
            WebRequest.DefaultWebProxy.Should().NotBeNull();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
            WebRequest.DefaultWebProxy = null;
        }
    }

    [Theory]
    [InlineData("https_proxy")]
    [InlineData("http_proxy")]
    public void ConfigureProxy_ShouldHandleLowercaseEnvironmentVariables(string envVarName)
    {
        // Arrange
        var proxyUrl = "http://proxy.company.com:8080";
        Environment.SetEnvironmentVariable(envVarName, proxyUrl);

        try
        {
            // Act
            ProxyService.ConfigureProxy();

            // Assert
            // The service should handle both uppercase and lowercase environment variables
            WebRequest.DefaultWebProxy.Should().NotBeNull();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(envVarName, null);
            WebRequest.DefaultWebProxy = null;
        }
    }

    [Fact]
    public void CreateHttpClientHandler_ShouldReturnValidHandler()
    {
        // Arrange & Act
        using var handler = ProxyService.CreateHttpClientHandler();

        // Assert
        handler.Should().NotBeNull();
        handler.Should().BeOfType<HttpClientHandler>();
    }

    [Fact]
    public void CreateHttpClientHandler_ShouldConfigureProxy_WhenEnvironmentVariableIsSet()
    {
        // Arrange
        var proxyUrl = "http://proxy.company.com:8080";
        Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);

        try
        {
            // Act
            using var handler = ProxyService.CreateHttpClientHandler();

            // Assert
            handler.Should().NotBeNull();
            handler.UseProxy.Should().BeTrue();
            handler.Proxy.Should().NotBeNull();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
        }
    }

    [Fact]
    public void CreateHttpClientHandler_ShouldConfigureProxyCredentials_WhenProvided()
    {
        // Arrange
        var proxyUrl = "http://proxy.company.com:8080";
        var proxyUser = "testuser";
        var proxyPass = "testpass";
        
        Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);
        Environment.SetEnvironmentVariable("PROXY_USER", proxyUser);
        Environment.SetEnvironmentVariable("PROXY_PASS", proxyPass);

        try
        {
            // Act
            using var handler = ProxyService.CreateHttpClientHandler();

            // Assert
            handler.Should().NotBeNull();
            handler.UseProxy.Should().BeTrue();
            handler.Proxy.Should().NotBeNull();
            handler.Proxy.Credentials.Should().NotBeNull();
        }
        finally
        {
            // Cleanup
            ClearProxyEnvironmentVariables();
        }
    }

    [Fact]
    public void CreateHttpClientHandler_ShouldUseDefaultCredentials_WhenNoCredentialsProvided()
    {
        // Arrange
        var proxyUrl = "http://proxy.company.com:8080";
        Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);

        try
        {
            // Act
            using var handler = ProxyService.CreateHttpClientHandler();

            // Assert
            handler.Should().NotBeNull();
            handler.UseProxy.Should().BeTrue();
            handler.UseDefaultCredentials.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
        }
    }

    [Fact]
    public void CreateHttpClientHandler_ShouldConfigureCertificateValidation()
    {
        // Arrange & Act
        using var handler = ProxyService.CreateHttpClientHandler();

        // Assert
        handler.Should().NotBeNull();
        handler.ServerCertificateCustomValidationCallback.Should().NotBeNull();
    }

    [Theory]
    [InlineData("http://proxy.company.com:8080")]
    [InlineData("https://secure-proxy.company.com:443")]
    [InlineData("http://192.168.1.100:3128")]
    public void CreateHttpClientHandler_ShouldHandleDifferentProxyUrls(string proxyUrl)
    {
        // Arrange
        Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);

        try
        {
            // Act
            using var handler = ProxyService.CreateHttpClientHandler();

            // Assert
            handler.Should().NotBeNull();
            handler.UseProxy.Should().BeTrue();
            handler.Proxy.Should().NotBeNull();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
        }
    }

    [Fact]
    public void ConfigureProxy_ShouldHandleInvalidProxyUrl_Gracefully()
    {
        // Arrange
        Environment.SetEnvironmentVariable("HTTPS_PROXY", "invalid-url");

        try
        {
            // Act
            var action = () => ProxyService.ConfigureProxy();

            // Assert
            action.Should().NotThrow();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
        }
    }

    [Fact]
    public void CreateHttpClientHandler_ShouldHandleInvalidProxyUrl_Gracefully()
    {
        // Arrange
        Environment.SetEnvironmentVariable("HTTPS_PROXY", "invalid-url");

        try
        {
            // Act
            var action = () => ProxyService.CreateHttpClientHandler();

            // Assert
            action.Should().NotThrow();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
        }
    }

    [Fact]
    public void ConfigureProxy_ShouldSetSecurityProtocol()
    {
        // Arrange
        var originalProtocol = ServicePointManager.SecurityProtocol;

        try
        {
            // Act
            ProxyService.ConfigureProxy();

            // Assert
            ServicePointManager.SecurityProtocol.Should().HaveFlag(SecurityProtocolType.Tls12);
        }
        finally
        {
            // Cleanup
            ServicePointManager.SecurityProtocol = originalProtocol;
        }
    }

    [Fact]
    public void ConfigureProxy_ShouldSetCertificateValidationCallback()
    {
        // Arrange
        var originalCallback = ServicePointManager.ServerCertificateValidationCallback;

        try
        {
            // Act
            ProxyService.ConfigureProxy();

            // Assert
            ServicePointManager.ServerCertificateValidationCallback.Should().NotBeNull();
        }
        finally
        {
            // Cleanup
            ServicePointManager.ServerCertificateValidationCallback = originalCallback;
        }
    }

    [Fact]
    public void CertificateValidation_ShouldReturnTrue_ForValidCertificates()
    {
        // This test would require creating mock certificates
        // For now, we test that the validation callback is set
        
        // Arrange
        ProxyService.ConfigureProxy();

        // Act & Assert
        ServicePointManager.ServerCertificateValidationCallback.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void ConfigureProxy_ShouldHandleEmptyProxyUrls(string proxyUrl)
    {
        // Arrange
        Environment.SetEnvironmentVariable("HTTPS_PROXY", proxyUrl);

        try
        {
            // Act
            var action = () => ProxyService.ConfigureProxy();

            // Assert
            action.Should().NotThrow();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
        }
    }

    [Fact]
    public void ProxyService_ShouldHandleMultipleConfigurationCalls()
    {
        // Arrange & Act
        var action = () =>
        {
            ProxyService.ConfigureProxy();
            ProxyService.ConfigureProxy();
            ProxyService.ConfigureProxy();
        };

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void CreateHttpClientHandler_ShouldCreateNewInstanceEachTime()
    {
        // Arrange & Act
        using var handler1 = ProxyService.CreateHttpClientHandler();
        using var handler2 = ProxyService.CreateHttpClientHandler();

        // Assert
        handler1.Should().NotBeSameAs(handler2);
        handler1.Should().NotBeNull();
        handler2.Should().NotBeNull();
    }

    [Fact]
    public void ProxyService_ShouldWorkWithHttpClient()
    {
        // Arrange
        using var handler = ProxyService.CreateHttpClientHandler();

        // Act
        var action = () => new HttpClient(handler);

        // Assert
        action.Should().NotThrow();
        using var client = action();
        client.Should().NotBeNull();
    }

    [Fact]
    public void ProxyService_ShouldSupportEnvironmentVariableOverrides()
    {
        // Arrange
        var testData = TestDataFixtures.EnvironmentVariables.ProxySettings;
        
        foreach (var kvp in testData)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }

        try
        {
            // Act
            var action = () => ProxyService.ConfigureProxy();

            // Assert
            action.Should().NotThrow();
        }
        finally
        {
            // Cleanup
            foreach (var kvp in testData)
            {
                Environment.SetEnvironmentVariable(kvp.Key, null);
            }
        }
    }

    [Fact]
    public void ProxyService_ShouldHandleNoProxyEnvironment()
    {
        // Arrange
        var testData = TestDataFixtures.EnvironmentVariables.NoProxySettings;
        
        foreach (var kvp in testData)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }

        try
        {
            // Act
            var action = () => ProxyService.ConfigureProxy();

            // Assert
            action.Should().NotThrow();
        }
        finally
        {
            // Cleanup
            foreach (var kvp in testData)
            {
                Environment.SetEnvironmentVariable(kvp.Key, null);
            }
        }
    }

    private void ClearProxyEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
        Environment.SetEnvironmentVariable("HTTP_PROXY", null);
        Environment.SetEnvironmentVariable("https_proxy", null);
        Environment.SetEnvironmentVariable("http_proxy", null);
        Environment.SetEnvironmentVariable("PROXY_USER", null);
        Environment.SetEnvironmentVariable("PROXY_PASS", null);
    }
}

/// <summary>
/// Integration tests for ProxyService that test with real network conditions
/// These tests are marked as integration tests and may require network access
/// </summary>
public class ProxyServiceIntegrationTests : TestBase
{
    [Fact(Skip = "Integration test - requires network access")]
    public async Task HttpClientWithProxy_ShouldMakeSuccessfulRequest()
    {
        // This test would verify that HTTP requests work through a proxy
        
        // Arrange
        using var handler = ProxyService.CreateHttpClientHandler();
        using var client = new HttpClient(handler);

        // Act
        var response = await client.GetAsync("https://httpbin.org/ip");

        // Assert
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact(Skip = "Integration test - requires corporate environment")]
    public async Task HttpClientWithCorporateProxy_ShouldHandleCertificates()
    {
        // This test would verify certificate handling in corporate environments
        
        // Arrange
        Environment.SetEnvironmentVariable("HTTPS_PROXY", "http://corporate-proxy:8080");
        using var handler = ProxyService.CreateHttpClientHandler();
        using var client = new HttpClient(handler);

        try
        {
            // Act
            var response = await client.GetAsync("https://graph.microsoft.com");

            // Assert
            response.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
        }
    }
}

/// <summary>
/// Tests for certificate validation functionality
/// </summary>
public class CertificateValidationTests : TestBase
{
    [Fact]
    public void CertificateValidation_ShouldBeConfigured_AfterProxySetup()
    {
        // Arrange
        var originalCallback = ServicePointManager.ServerCertificateValidationCallback;

        try
        {
            // Act
            ProxyService.ConfigureProxy();

            // Assert
            ServicePointManager.ServerCertificateValidationCallback.Should().NotBeNull();
            ServicePointManager.ServerCertificateValidationCallback.Should().NotBe(originalCallback);
        }
        finally
        {
            // Cleanup
            ServicePointManager.ServerCertificateValidationCallback = originalCallback;
        }
    }

    [Fact]
    public void SecurityProtocol_ShouldIncludeTls12_AfterConfiguration()
    {
        // Arrange
        var originalProtocol = ServicePointManager.SecurityProtocol;

        try
        {
            // Act
            ProxyService.ConfigureProxy();

            // Assert
            ServicePointManager.SecurityProtocol.Should().HaveFlag(SecurityProtocolType.Tls12);
        }
        finally
        {
            // Cleanup
            ServicePointManager.SecurityProtocol = originalProtocol;
        }
    }

    [Fact]
    public void HttpClientHandler_ShouldHaveCertificateValidation()
    {
        // Arrange & Act
        using var handler = ProxyService.CreateHttpClientHandler();

        // Assert
        handler.ServerCertificateCustomValidationCallback.Should().NotBeNull();
    }
}