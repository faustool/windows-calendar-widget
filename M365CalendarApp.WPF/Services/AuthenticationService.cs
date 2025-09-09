using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace M365CalendarApp.WPF.Services;

public class AuthenticationService
{
    private const string CredentialTarget = "M365CalendarApp_Token";
    
    private readonly ConfigurationService _configService;
    private string _clientId = "";
    private string _tenantId = "";
    private string[] _scopes = Array.Empty<string>();

    private IPublicClientApplication? _clientApp;
    private AuthenticationResult? _authResult;

    public bool IsAuthenticated => _authResult != null && !IsTokenExpired();

    public AuthenticationService(ConfigurationService? configService = null)
    {
        _configService = configService ?? new ConfigurationService();
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            // Load configuration
            var config = await _configService.LoadConfigurationAsync();
            _clientId = config.Authentication.ClientId;
            _tenantId = config.Authentication.TenantId;
            _scopes = config.Authentication.Scopes;

            if (string.IsNullOrEmpty(_clientId))
            {
                throw new InvalidOperationException("Client ID not configured. Please set Authentication.ClientId in appsettings.json");
            }
            // Configure MSAL cache
            var storageProperties = new StorageCreationPropertiesBuilder(
                "M365CalendarApp.cache",
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
                .WithLinuxKeyring(
                    "M365CalendarApp",
                    "M365CalendarApp",
                    "M365 Calendar App Token Cache",
                    new KeyValuePair<string, string>("Version", "1.0.0.0"),
                    new KeyValuePair<string, string>("ProductGroup", "M365CalendarApp"))
                .WithMacKeyChain(
                    "M365CalendarApp",
                    "M365 Calendar App Token Cache")
                .Build();

            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);

            _clientApp = PublicClientApplicationBuilder
                .Create(_clientId)
                .WithAuthority($"https://login.microsoftonline.com/{_tenantId}")
                .WithRedirectUri("http://localhost")
                .WithLogging((level, message, containsPii) =>
                {
                    System.Diagnostics.Debug.WriteLine($"MSAL {level}: {message}");
                }, LogLevel.Info, enablePiiLogging: false, enableDefaultPlatformLogging: true)
                .Build();

            cacheHelper.RegisterCache(_clientApp.UserTokenCache);

            // Try to load existing token from Windows Credential Manager
            await LoadStoredTokenAsync();

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Authentication initialization failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> LoginAsync()
    {
        if (_clientApp == null)
        {
            throw new InvalidOperationException("Authentication service not initialized");
        }

        try
        {
            // First try silent authentication
            var accounts = await _clientApp.GetAccountsAsync();
            if (accounts.Any())
            {
                try
                {
                    _authResult = await _clientApp.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                        .ExecuteAsync();
                    
                    await StoreTokenSecurelyAsync(_authResult);
                    return true;
                }
                catch (MsalUiRequiredException)
                {
                    // Silent authentication failed, continue to interactive
                }
            }

            // Interactive authentication
            _authResult = await _clientApp.AcquireTokenInteractive(_scopes)
                .WithPrompt(Prompt.SelectAccount)
                .WithParentActivityOrWindow(GetActiveWindowHandle())
                .ExecuteAsync();

            await StoreTokenSecurelyAsync(_authResult);
            return true;
        }
        catch (MsalException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Authentication failed: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected authentication error: {ex.Message}");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        if (_clientApp == null || _authResult == null) return;

        try
        {
            var accounts = await _clientApp.GetAccountsAsync();
            if (accounts.Any())
            {
                await _clientApp.RemoveAsync(accounts.FirstOrDefault());
            }

            // Remove stored credentials
            RemoveStoredCredentials();
            _authResult = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Logout failed: {ex.Message}");
        }
    }

    public string? GetAccessToken()
    {
        return _authResult?.AccessToken;
    }

    public string? GetUserDisplayName()
    {
        return _authResult?.Account?.Username;
    }

    private bool IsTokenExpired()
    {
        return _authResult?.ExpiresOn <= DateTimeOffset.UtcNow.AddMinutes(-5);
    }

    private async Task StoreTokenSecurelyAsync(AuthenticationResult authResult)
    {
        try
        {
            var tokenData = new
            {
                AccessToken = authResult.AccessToken,
                ExpiresOn = authResult.ExpiresOn,
                Username = authResult.Account?.Username,
                TenantId = authResult.TenantId
            };

            var jsonData = JsonSerializer.Serialize(tokenData);
            var encryptedData = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(jsonData),
                null,
                DataProtectionScope.CurrentUser);

            // Store in Windows Credential Manager
            var credential = new System.Net.NetworkCredential("M365CalendarApp", Convert.ToBase64String(encryptedData));
            StoreCredentialInWindows(CredentialTarget, credential);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to store token securely: {ex.Message}");
        }
    }

    private async Task LoadStoredTokenAsync()
    {
        try
        {
            var credential = RetrieveCredentialFromWindows(CredentialTarget);
            if (credential == null) return;

            var encryptedData = Convert.FromBase64String(credential.Password);
            var decryptedData = ProtectedData.Unprotect(
                encryptedData,
                null,
                DataProtectionScope.CurrentUser);

            var jsonData = Encoding.UTF8.GetString(decryptedData);
            var tokenData = JsonSerializer.Deserialize<JsonElement>(jsonData);

            // Check if token is still valid
            var expiresOn = DateTimeOffset.Parse(tokenData.GetProperty("ExpiresOn").GetString()!);
            if (expiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                // Token is still valid, but we need to refresh it through MSAL
                // This will trigger a silent refresh if needed
                var accounts = await _clientApp!.GetAccountsAsync();
                if (accounts.Any())
                {
                    try
                    {
                        _authResult = await _clientApp.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();
                    }
                    catch (MsalUiRequiredException)
                    {
                        // Silent refresh failed, user will need to login again
                        RemoveStoredCredentials();
                    }
                }
            }
            else
            {
                // Token expired, remove it
                RemoveStoredCredentials();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load stored token: {ex.Message}");
            RemoveStoredCredentials();
        }
    }

    private void StoreCredentialInWindows(string target, System.Net.NetworkCredential credential)
    {
        try
        {
            // This is a simplified implementation
            // In a real Windows environment, you would use Windows Credential Manager APIs
            // For now, we'll use a secure file-based approach as fallback
            var credentialPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "M365CalendarApp",
                "credentials.dat");

            Directory.CreateDirectory(Path.GetDirectoryName(credentialPath)!);
            
            var encryptedCredential = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(credential.Password),
                null,
                DataProtectionScope.CurrentUser);

            File.WriteAllBytes(credentialPath, encryptedCredential);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to store credential: {ex.Message}");
        }
    }

    private System.Net.NetworkCredential? RetrieveCredentialFromWindows(string target)
    {
        try
        {
            var credentialPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "M365CalendarApp",
                "credentials.dat");

            if (!File.Exists(credentialPath)) return null;

            var encryptedCredential = File.ReadAllBytes(credentialPath);
            var decryptedCredential = ProtectedData.Unprotect(
                encryptedCredential,
                null,
                DataProtectionScope.CurrentUser);

            var password = Encoding.UTF8.GetString(decryptedCredential);
            return new System.Net.NetworkCredential("M365CalendarApp", password);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to retrieve credential: {ex.Message}");
            return null;
        }
    }

    private void RemoveStoredCredentials()
    {
        try
        {
            var credentialPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "M365CalendarApp",
                "credentials.dat");

            if (File.Exists(credentialPath))
            {
                File.Delete(credentialPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to remove stored credentials: {ex.Message}");
        }
    }

    private IntPtr GetActiveWindowHandle()
    {
        // Get the handle of the current WPF window
        var window = System.Windows.Application.Current?.MainWindow;
        if (window != null)
        {
            var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(window);
            return windowInteropHelper.Handle;
        }
        return IntPtr.Zero;
    }
}