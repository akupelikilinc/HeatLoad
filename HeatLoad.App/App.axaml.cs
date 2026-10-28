using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace HeatLoad;

public partial class App : Application
{
    private Mutex? _mutex;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Dispatcher.UIThread.UnhandledException += (_, e) => Log(e.Exception);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            // A crash has to leave a trace somewhere: the app starts at logon with no console.
            _mutex = new Mutex(true, "HeatLoad.App.SingleInstance", out var created);
            if (!created)
            {
                _mutex.Dispose();
                _mutex = null;
                desktop.Shutdown();
                return;
            }

            var hud = new MainWindow();
            desktop.MainWindow = hud;
            desktop.Exit += (_, _) => { _mutex?.ReleaseMutex(); _mutex?.Dispose(); _mutex = null; };
            if (desktop.Args?.Contains("--settings") == true)
                Dispatcher.UIThread.Post(hud.OpenSettings, DispatcherPriority.Background);
        }

        base.OnFrameworkInitializationCompleted();
    }

    internal static void Log(object? message)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HeatLoad");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "error.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
        }
        catch
        {
            // logging must never take the app down
        }
    }
}



