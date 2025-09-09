# Azure AD App Registration Setup

## Creating Your Own App Registration

To support both personal and work Microsoft accounts, you need to create your own Azure AD app registration.

### Step 1: Access Azure Portal
1. Go to [Azure Portal](https://portal.azure.com)
2. Sign in with your Microsoft account
3. Navigate to **Azure Active Directory** > **App registrations**

### Step 2: Create New Registration
1. Click **"New registration"**
2. Fill in the details:
   - **Name**: `M365 Calendar App` (or your preferred name)
   - **Supported account types**: Select **"Accounts in any organizational directory and personal Microsoft accounts"**
   - **Redirect URI**: Select **"Public client/native (mobile & desktop)"** and enter: `http://localhost`

3. Click **"Register"**

### Step 3: Configure API Permissions
1. In your new app registration, go to **"API permissions"**
2. Click **"Add a permission"**
3. Select **"Microsoft Graph"**
4. Choose **"Delegated permissions"**
5. Add these permissions:
   - `Calendars.Read`
   - `User.Read`
6. Click **"Add permissions"**
7. **Important**: Click **"Grant admin consent"** (if you have admin rights) or ask your admin to grant consent

### Step 4: Configure Authentication
1. Go to **"Authentication"**
2. Under **"Advanced settings"**, ensure:
   - **"Allow public client flows"** is set to **"Yes"**
3. Save the changes

### Step 5: Get Your Client ID
1. Go to **"Overview"**
2. Copy the **"Application (client) ID"** - this is your new Client ID

### Step 6: Update Application Configuration
Replace the Client ID in your application:

```csharp
private const string ClientId = "YOUR_NEW_CLIENT_ID_HERE";
private const string TenantId = "common"; // Now supports both personal and work accounts
```

## Configuration Options

### For Personal Accounts Only
```csharp
private const string TenantId = "consumers";
```

### For Work/School Accounts Only
```csharp
private const string TenantId = "organizations";
```

### For Both Personal and Work Accounts
```csharp
private const string TenantId = "common";
```

### For Specific Organization Only
```csharp
private const string TenantId = "your-tenant-id-here";
```

## Troubleshooting

### Common Issues

**"AADSTS50194: Application is not configured as a multi-tenant application"**
- Solution: Ensure "Supported account types" is set to include the account types you want to support

**"AADSTS65001: The user or administrator has not consented to use the application"**
- Solution: Grant admin consent for the API permissions, or have users consent during first login

**"AADSTS50020: User account from identity provider does not exist in tenant"**
- Solution: Check that the TenantId matches your account type (consumers/organizations/common)

### Testing Your Configuration

1. **Build and run** the application
2. **Click Login** - you should see the Microsoft login page
3. **Sign in** with your account
4. **Grant permissions** if prompted
5. **Verify** that calendar events load successfully

## Security Best Practices

1. **Keep your Client ID secure** - don't commit it to public repositories
2. **Use environment variables** for sensitive configuration in production
3. **Regularly review** API permissions and remove unused ones
4. **Monitor** app usage through Azure AD logs

## Alternative: Use Configuration File

You can also make the Client ID configurable through appsettings.json:

```json
{
  "Authentication": {
    "ClientId": "your-client-id-here",
    "TenantId": "common",
    "Scopes": [
      "https://graph.microsoft.com/Calendars.Read",
      "https://graph.microsoft.com/User.Read"
    ]
  }
}
```

Then update the AuthenticationService to read from configuration instead of using constants.