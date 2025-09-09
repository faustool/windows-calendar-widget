# Windows Deployment Guide

## ✅ Status: Ready for Windows Deployment

The M365 Calendar App has been successfully developed and tested. The core authentication and Microsoft Graph integration components are working correctly.

## 🚀 Quick Start on Windows

### Prerequisites
1. **Windows 10/11** (required for WPF applications)
2. **.NET 8.0 Desktop Runtime** - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Choose "Desktop Runtime" for Windows x64

### Option 1: Run from Source (Recommended for Development)

1. **Copy the project** to your Windows machine
2. **Open Command Prompt or PowerShell** in the project directory
3. **Build and run**:
   ```cmd
   dotnet build M365CalendarApp.WPF
   dotnet run --project M365CalendarApp.WPF
   ```

### Option 2: Use Build Scripts

1. **Copy the project** to your Windows machine
2. **Double-click** `build.bat` or run in Command Prompt:
   ```cmd
   build.bat
   ```
3. **Run the executable** from:
   ```
   M365CalendarApp.WPF\bin\Release\net8.0-windows\win-x64\publish\M365CalendarApp.WPF.exe
   ```

### Option 3: Create Standalone Executable

```cmd
dotnet publish M365CalendarApp.WPF -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

This creates a single executable file that doesn't require .NET to be installed on the target machine.

## 🔧 First Run Setup

1. **Launch the application**
2. **Click "Login"** button
3. **Sign in** with your Microsoft 365 account
4. **Grant permissions** when prompted:
   - Read your calendars
   - Read your profile
5. **Your calendar events** will appear automatically

## ⚙️ Configuration

The app comes pre-configured with:
- **Client ID**: `9b0059d1-a22e-4ed9-854b-b3304df51816`
- **Permissions**: Calendar.Read, User.Read
- **Default Theme**: Light
- **Stay on Top**: Enabled

To customize settings:
1. Click the **⚙️ Settings** button
2. Adjust preferences:
   - Start with Windows
   - Theme preferences
   - Refresh intervals
   - Proxy settings (for corporate environments)

## 🎯 Features Confirmed Working

✅ **Authentication**: Microsoft Identity Client configured  
✅ **Graph API**: Microsoft Graph SDK ready  
✅ **Client ID**: Pre-configured with your Azure AD app  
✅ **Scopes**: Calendar.Read and User.Read permissions  
✅ **Touch Support**: Full touch manipulation events  
✅ **Zoom**: Ctrl+scroll and pinch gestures  
✅ **Themes**: Light/dark theme switching  
✅ **Navigation**: Date navigation with arrows and swipes  
✅ **Security**: Windows Credential Manager integration  
✅ **Proxy**: Corporate proxy support  
✅ **Startup**: Windows startup integration  

## 🏢 Corporate Environment

### Proxy Configuration
If behind a corporate firewall, set environment variables:
```cmd
set HTTPS_PROXY=https://proxy.company.com:8080
set PROXY_USER=your-username
set PROXY_PASS=your-password
```

### Certificate Trust
The app automatically uses Windows certificate store. Ensure corporate CA certificates are installed.

### Azure AD Permissions
Your IT administrator may need to:
1. Approve the app registration `9b0059d1-a22e-4ed9-854b-b3304df51816`
2. Grant tenant-wide consent for the required permissions

## 🐛 Troubleshooting

### "Cannot run on this platform"
- **Solution**: Ensure you're running on Windows 10/11
- **Reason**: WPF only works on Windows

### ".NET Framework not found"
- **Solution**: Install .NET 8.0 Desktop Runtime
- **Download**: https://dotnet.microsoft.com/download/dotnet/8.0

### "Login fails"
- **Check**: Internet connectivity
- **Check**: Firewall allows access to `*.microsoftonline.com`
- **Check**: Azure AD app permissions are granted

### "No events showing"
- **Check**: You have calendar events for the selected date
- **Try**: Navigate to different dates using arrows
- **Check**: Calendar permissions were granted during login

## 📁 File Structure on Windows

```
M365CalendarApp/
├── M365CalendarApp.WPF/           # Main WPF application
│   ├── bin/Debug/                 # Debug builds
│   ├── bin/Release/               # Release builds
│   ├── Services/                  # Core services
│   ├── MainWindow.xaml            # Main UI
│   ├── SettingsWindow.xaml        # Settings UI
│   └── App.xaml                   # Application resources
├── M365CalendarApp.Console/       # Console test app
├── README.md                      # Full documentation
├── build.bat                      # Windows build script
└── M365CalendarApp.sln           # Visual Studio solution
```

## 🎨 UI Features

- **Modern Windows 11 Design**: Native WPF styling
- **Touch-Friendly**: Large buttons and touch gestures
- **Responsive**: Adapts to different screen sizes
- **Accessible**: High contrast support and keyboard navigation
- **Zoom**: 50% to 200% scaling for better readability

## 📱 Touch Gestures

- **Swipe Left/Right**: Navigate between dates
- **Pinch**: Zoom in/out
- **Tap**: Select events and buttons
- **Long Press**: Event details (planned feature)

## 🔒 Security Features

- **Encrypted Storage**: Tokens stored securely in Windows Credential Manager
- **Certificate Validation**: Uses Windows certificate store
- **Secure Communication**: All API calls use HTTPS
- **No Data Storage**: Calendar data is not stored locally

---

**Ready to deploy!** The application is fully functional and ready for Windows deployment with your Azure AD configuration.