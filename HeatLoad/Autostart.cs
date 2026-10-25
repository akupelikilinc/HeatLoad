using System.Diagnostics;

namespace HeatLoad;

/// <summary>
/// Autostart is the "HeatLoad" scheduled task (the app needs admin, so the Run key cannot be used).
/// null means the task is missing, so the switch must stay off.
/// </summary>
internal static class Autostart
{
    private const string TaskName = "HeatLoad";

    public static bool? IsEnabled()
    {
        var xml = Run($"/Query /TN {TaskName} /XML");
        if (xml is null) return null;
        // schtasks omits <Enabled> entirely while the task is enabled.
        return !xml.Contains("<Enabled>false</Enabled>", StringComparison.OrdinalIgnoreCase);
    }

    public static bool Set(bool enabled) =>
        Run($"/Change /TN {TaskName} {(enabled ? "/ENABLE" : "/DISABLE")}") is not null;

    private static string? Run(string arguments)
    {
        try
        {
            var start = new ProcessStartInfo("schtasks.exe", arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }
}
