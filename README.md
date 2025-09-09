# M365 Calendar App

A modern Windows 11 WPF application that connects to Microsoft 365 Calendar and displays today's agenda with touch support, theme switching, and zoom functionality.

## Features

- **Microsoft 365 Integration**: Connects to your personal or work Microsoft account
- **Smart Notifications**: Calendar event reminders with flexible dismiss and snooze options
- **Touch-Friendly Interface**: Full touch support with swipe gestures for navigation
- **Zoom Support**: Ctrl+Mouse scroll or pinch gestures to adjust text and button sizes
- **Theme Support**: Light and dark themes with automatic switching
- **Stay on Top**: Option to keep the window always on top of other applications
- **Day Navigation**: Navigate between dates using arrows or swipe gestures
- **Secure Authentication**: Uses Windows Credential Manager for secure token storage
- **Corporate Proxy Support**: Works behind corporate proxies with HTTPS_PROXY environment variables
- **Certificate Validation**: Supports trusted CA certificates from Windows certificate store
- **Startup Integration**: Option to start with Windows

## Prerequisites

- Windows 10/11
- .NET 8.0 Runtime
- Microsoft 365 account (personal or work)
- Azure AD App Registration (see setup instructions below)

## Setup Instructions

### 1. Azure AD App Registration

The application is pre-configured with Client ID: `9b0059d1-a22e-4ed9-854b-b3304df51816`

**Note**: This app registration should already exist in your Azure AD tenant. If you need to create your own app registration:

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** > **App registrations**
3. Click **New registration**
4. Fill in the details:
   - **Name**: M365 Calendar App
   - **Supported account types**: Accounts in any organizational directory and personal Microsoft accounts
   - **Redirect URI**: Public client/native (mobile & desktop) - `http://localhost`
5. Click **Register**
6. Copy the **Application (client) ID** and update it in the settings

### 2. API Permissions

1. In your app registration, go to **API permissions**
2. Click **Add a permission**
3. Select **Microsoft Graph**
4. Choose **Delegated permissions**
5. Add the following permissions:
   - `Calendars.Read`
   - `User.Read`
6. Click **Add permissions**
7. **Important**: Click **Grant admin consent** if you're in an organization

### 3. Application Configuration

The application comes pre-configured with the Client ID. You can optionally:

1. Open the application
2. Click the **Settings** button (⚙️)
3. Verify the **Client ID** is set to `9b0059d1-a22e-4ed9-854b-b3304df51816`
4. Configure other settings as needed:
   - Start with Windows
   - Always stay on top
   - Default theme
   - Refresh interval
   - Proxy settings (if behind corporate firewall)

### 4. First Run

1. Launch the application
2. Click **Login** button
3. Sign in with your Microsoft account
4. Grant permissions when prompted
5. Your calendar events will appear automatically

## Usage

### Navigation
- **Arrow Buttons**: Click left/right arrows to navigate between days
- **Touch Gestures**: Swipe left/right to change dates
- **Transparency**: Navigation arrows are 80% transparent but become solid on hover/touch

### Zoom
- **Mouse**: Hold Ctrl and scroll mouse wheel
- **Touch**: Use pinch gestures to zoom in/out
- **Range**: 50% to 200% zoom levels

### Themes
- **Toggle**: Click the moon/sun icon in the header
- **Auto-apply**: Theme preference is saved and applied on startup

### Event Details
- **Click/Touch**: Tap any event to see detailed information in a popup
- **Expand**: Click the ▼ button in the bottom-right corner of any event to expand inline details
- **Expanded View**: Shows organizer, location, and event body description
- **Information**: Full details include subject, time, location, organizer, attendees, and description

### Notifications
- **Smart Reminders**: Automatic notifications based on event reminder settings
- **Flexible Snooze**: 5 snooze options including "before event" timing
- **Working Hours**: Optional filtering to show notifications only during business hours
- **Persistent**: Notifications are saved and restored across app restarts
- **Auto-dismiss**: Configurable automatic dismissal after set time

For detailed notification configuration and usage, see [NOTIFICATIONS.md](NOTIFICATIONS.md).

## Corporate Environment Setup

### Proxy Configuration
Set environment variables:
```cmd
set HTTPS_PROXY=https://proxy.company.com:8080
set HTTP_PROXY=http://proxy.company.com:8080
set PROXY_USER=your-username
set PROXY_PASS=your-password
```

### Certificate Trust
The application automatically uses certificates from the Windows certificate store. Ensure your corporate CA certificates are installed in:
- Current User > Trusted Root Certification Authorities
- Local Machine > Trusted Root Certification Authorities

## Troubleshooting

### Authentication Issues
1. **Login fails**: Check your Client ID in settings
2. **Permission denied**: Ensure API permissions are granted in Azure AD
3. **Corporate account**: Contact your IT administrator for app approval

### Network Issues
1. **Behind proxy**: Configure proxy settings in the application settings
2. **Certificate errors**: Install corporate CA certificates in Windows certificate store
3. **Firewall**: Ensure outbound HTTPS (443) access to `*.microsoftonline.com` and `*.graph.microsoft.com`

### Application Issues
1. **Won't start**: Ensure .NET 8.0 Runtime is installed
2. **Touch not working**: Check Windows touch settings
3. **Stay on top not working**: Try restarting the application

## File Locations

- **Configuration**: `%LOCALAPPDATA%\M365CalendarApp\appsettings.json`
- **Credentials**: Windows Credential Manager (secure storage)
- **Cache**: `%LOCALAPPDATA%\M365CalendarApp\M365CalendarApp.cache`

## Privacy & Security

- **Token Storage**: Authentication tokens are encrypted and stored in Windows Credential Manager
- **Data Access**: Only reads calendar events and basic user profile information
- **Network**: All communication uses HTTPS with certificate validation
- **Local Storage**: No calendar data is stored locally (cache only contains authentication tokens)

## Building from Source

### Requirements
- Visual Studio 2022 or VS Code
- .NET 8.0 SDK
- Windows 10/11 SDK

### Build Steps
```bash
git clone <repository-url>
cd M365CalendarApp
dotnet restore
dotnet build
dotnet run --project M365CalendarApp.WPF
```

### Publishing
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues and feature requests, please create an issue in the GitHub repository.

## Version History

- **v1.0.0**: Initial release with core functionality
  - Microsoft 365 calendar integration
  - Touch support and zoom functionality
  - Theme switching
  - Proxy and certificate support
  - Windows startup integration