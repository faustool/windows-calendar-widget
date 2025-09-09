using Microsoft.Win32;
using System.Reflection;

namespace M365CalendarApp.WPF.Services;

public static class StartupService
{
    private const string AppName = "M365CalendarApp";
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            var value = key?.GetValue(AppName);
            return value != null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking startup status: {ex.Message}");
            return false;
        }
    }

    public static bool EnableStartup()
    {
        try
        {
            var executablePath = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(executablePath))
            {
                executablePath = Environment.ProcessPath;
            }

            if (string.IsNullOrEmpty(executablePath))
            {
                return false;
            }

            // For .NET applications, we need to use the executable path
            if (executablePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                executablePath = executablePath.Replace(".dll", ".exe");
            }

            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key != null)
            {
                key.SetValue(AppName, $"\"{executablePath}\"");
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error enabling startup: {ex.Message}");
        }
        return false;
    }

    public static bool DisableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key != null)
            {
                key.DeleteValue(AppName, false);
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error disabling startup: {ex.Message}");
        }
        return false;
    }

    public static void ToggleStartup()
    {
        if (IsStartupEnabled())
        {
            DisableStartup();
        }
        else
        {
            EnableStartup();
        }
    }
}