using System.Text.Json;

namespace HeatLoad;

internal sealed class SettingsData
{
    public bool TemperatureFirst { get; set; }
    public bool Compact { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
}

/// <summary>Per-user preferences in a JSON file, so the same code works on every OS.</summary>
internal static class AppSettings
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HeatLoad");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");
    private static SettingsData _data = Load();

    public static bool TemperatureFirst
    {
        get => _data.TemperatureFirst;
        set { _data.TemperatureFirst = value; Save(); }
    }

    /// <summary>Compact card: only the numbers, no labels or bars.</summary>
    public static bool Compact
    {
        get => _data.Compact;
        set { _data.Compact = value; Save(); }
    }

    /// <summary>Where the user dragged the widget; null means "still above the clock". Stored in screen pixels.</summary>
    public static (int X, int Y)? Position
    {
        get => _data.X is { } x && _data.Y is { } y ? (x, y) : null;
        set
        {
            _data.X = value?.X;
            _data.Y = value?.Y;
            Save();
        }
    }

    private static SettingsData Load()
    {
        try
        {
            return File.Exists(FilePath)
                ? JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(FilePath)) ?? new SettingsData()
                : new SettingsData();
        }
        catch
        {
            return new SettingsData(); // a corrupt file must not stop the widget
        }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_data));
        }
        catch
        {
            // losing a preference is not worth a crash
        }
    }
}
