# Authentication Error Fix

## Problem
You encountered this error when logging in:
```
Error details: error invalid_request error_description: The request is not valid for the application's 'userAudience' configuration. In order to use /common/ endpoint, the application must not be configured with 'Consumer' as the user audience. The userAudience should be configured with 'All' to use /common/ endpoint.
```

## Root Cause
The pre-configured Azure AD app registration (`9b0059d1-a22e-4ed9-854b-b3304df51816`) is set up to only support **personal Microsoft accounts** (Consumer), but the application was trying to use the `/common/` endpoint which requires support for both personal and work accounts.

## Solution Applied
I've fixed this by updating the application to use the `consumers` endpoint instead of `common`, which matches the app registration configuration.

### Changes Made:

1. **Updated Default Configuration** (`ConfigurationService.cs`):
   ```csharp
   TenantId = "consumers" // Personal Microsoft accounts only
   ```

2. **Made Authentication Configurable** (`AuthenticationService.cs`):
   - Now reads Client ID, Tenant ID, and Scopes from `appsettings.json`
   - No more hardcoded values
   - Better error handling for missing configuration

3. **Updated MainWindow** to pass configuration service to authentication service

## Current Configuration
The application now uses these settings by default:
```json
{
  "Authentication": {
    "ClientId": "9b0059d1-a22e-4ed9-854b-b3304df51816",
    "TenantId": "consumers",
    "Scopes": [
      "https://graph.microsoft.com/Calendars.Read",
      "https://graph.microsoft.com/User.Read"
    ]
  }
}
```

## Account Type Support

| TenantId Value | Supported Accounts | Use Case |
|----------------|-------------------|----------|
| `consumers` | Personal Microsoft accounts only | @outlook.com, @hotmail.com, @live.com |
| `organizations` | Work/School accounts only | Company/organization accounts |
| `common` | Both personal and work accounts | Requires app registration with "All" audience |
| `your-tenant-id` | Specific organization only | Single organization deployment |

## Testing the Fix
1. **Build and run** the application
2. **Click Login** - you should now see the Microsoft login page
3. **Sign in** with a personal Microsoft account (@outlook.com, @hotmail.com, @live.com)
4. **Grant permissions** if prompted
5. **Verify** that calendar events load successfully

## For Work/School Accounts
If you need to support work/school accounts, you have two options:

### Option 1: Work Accounts Only
Change the configuration to:
```json
{
  "Authentication": {
    "TenantId": "organizations"
  }
}
```

### Option 2: Create Your Own App Registration
For both personal and work accounts:
1. Follow the guide in [AZURE_SETUP.md](AZURE_SETUP.md)
2. Create your own Azure AD app registration with "All" audience support
3. Update the configuration with your new Client ID:
```json
{
  "Authentication": {
    "ClientId": "your-new-client-id-here",
    "TenantId": "common"
  }
}
```

## Configuration File Location
The settings are stored in: `%APPDATA%\M365CalendarApp\appsettings.json`

If this file doesn't exist, the application will create it with default values on first run.

## Troubleshooting

### Still Getting Authentication Errors?
1. **Clear cached tokens**: Delete the MSAL cache folder at `%LOCALAPPDATA%\M365CalendarApp.cache`
2. **Check account type**: Ensure you're using the correct account type for your TenantId setting
3. **Verify configuration**: Check that `appsettings.json` has the correct values

### Need to Switch Account Types?
1. **Logout** from the current account in the application
2. **Update** the `TenantId` in `appsettings.json`
3. **Restart** the application
4. **Login** with the appropriate account type

The authentication system is now more flexible and should handle your login requirements correctly!