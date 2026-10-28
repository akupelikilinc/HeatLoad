using System.Diagnostics;

namespace HeatLoad;

/// <summary>
/// Per-OS autostart. Windows uses the "HeatLoad" scheduled task (the app needs admin),
/// Linux an XDG desktop entry, macOS a launch agent. null means "cannot tell", so the switch stays off.
/// </summary>
internal static class Autostart
{
    private const string TaskName = "HeatLoad";
    private const string LaunchAgent = "com.heatload.HeatLoad";

    private static string ProcessPath =>
        Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];

    public static bool? IsEnabled()
    {
        if (OperatingSystem.IsWindows()) return WindowsQuery() is { } xml
            ? !xml.Contains("<Enabled>false</Enabled>", StringComparison.OrdinalIgnoreCase)
            : null;
        if (OperatingSystem.IsMacOS()) return File.Exists(LaunchAgentPath);
        return File.Exists(DesktopPath);
    }

    public static bool Set(bool enabled)
    {
        if (OperatingSystem.IsWindows())
            return Run($"/Change /TN {TaskName} {(enabled ? "/ENABLE" : "/DISABLE")}") is not null;

        try
        {
            if (OperatingSystem.IsMacOS())
            {
                if (enabled) File.WriteAllText(LaunchAgentPath, LaunchAgentPlist());
                else File.Delete(LaunchAgentPath);
            }
            else
            {
                if (enabled)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(DesktopPath)!);
                    File.WriteAllText(DesktopPath, DesktopEntry());
                }
                else
                {
                    File.Delete(DesktopPath);
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string DesktopPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "autostart", "heatload.desktop");

    private static string LaunchAgentPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents", $"{LaunchAgent}.plist");

    private static string DesktopEntry() =>
        $"""
        [Desktop Entry]
        Type=Application
        Name=HeatLoad
        Comment=CPU, GPU and RAM widget
        Exec="{ProcessPath}"
        Terminal=false
        X-GNOME-Autostart-enabled=true
        """;

    private static string LaunchAgentPlist() =>
        $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
        <plist version="1.0">
        <dict>
          <key>Label</key><string>{LaunchAgent}</string>
          <key>ProgramArguments</key><array><string>{ProcessPath}</string></array>
          <key>RunAtLoad</key><true/>
        </dict>
        </plist>
        """;

    private static string? WindowsQuery()
    {
        var xml = Run($"/Query /TN {TaskName} /XML");
        return xml;
    }

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
