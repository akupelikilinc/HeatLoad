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
}
