using Microsoft.Win32;

namespace HeatLoad;

/// <summary>Per-user preferences, kept in HKCU so the elevated app still writes to the user's hive.</summary>
internal static class AppSettings
{
    private const string KeyPath = @"Software\HeatLoad";

    public static bool TemperatureFirst
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue("Mode") is 1;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
            key.SetValue("Mode", value ? 1 : 0, RegistryValueKind.DWord);
        }
    }

    /// <summary>Compact card: only the numbers, no labels or bars.</summary>
    public static bool Compact
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue("Compact") is 1;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
            key.SetValue("Compact", value ? 1 : 0, RegistryValueKind.DWord);
        }
    }

    /// <summary>Where the user dragged the widget; null means "still above the clock".</summary>
    public static (int X, int Y)? Position
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue("X") is int x && key.GetValue("Y") is int y ? (x, y) : null;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
            if (value is { } p)
            {
                key.SetValue("X", p.X, RegistryValueKind.DWord);
                key.SetValue("Y", p.Y, RegistryValueKind.DWord);
            }
            else
            {
                key.DeleteValue("X", false);
                key.DeleteValue("Y", false);
            }
        }
    }
}
