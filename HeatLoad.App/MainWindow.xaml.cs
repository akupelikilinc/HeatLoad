using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace HeatLoad;

public partial class MainWindow : Window
{
    private readonly ManualResetEventSlim _quit = new(false);
    private readonly Thread _worker;
    private readonly DispatcherTimer _savePosition;
    private ISensorSource? _sensors;
    private TrayIcon? _tray;
    private SettingsWindow? _settings;
    private Reading _last;
    private bool _temperatureFirst = AppSettings.TemperatureFirst;
    private bool _compact = AppSettings.Compact;
    private bool _hudVisible = true;

    private static readonly IBrush Normal = Frozen(0xF2, 0xF6, 0xFC);
    private static readonly IBrush Muted = Frozen(0x9A, 0xA4, 0xB3);
    private static readonly IBrush Warm = Frozen(0xF2, 0xC1, 0x6B);
    private static readonly IBrush Hot = Frozen(0xFF, 0x7B, 0x7B);
    private static readonly IBrush AccentCpu = Frozen(0x5B, 0xC8, 0xFF);
    private static readonly IBrush AccentGpu = Frozen(0x6F, 0xE7, 0xB7);
    private static readonly IBrush AccentRam = Frozen(0xB7, 0xA6, 0xFF);

    public MainWindow()
    {
        InitializeComponent();
        _worker = new Thread(SensorLoop) { IsBackground = true, Name = "HeatLoad.Sensors" };

        // Placed on the first frame; hidden until then so it never flashes at 0,0.
        Opacity = 0;
        _savePosition = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _savePosition.Tick += (_, _) => { _savePosition.Stop(); SavePosition(); };
        PositionChanged += (_, _) => { _savePosition.Stop(); _savePosition.Start(); };

        ShowMode();
        SetCompact(_compact);
        AddTray();

        Opened += (_, _) =>
        {
            Place();
            Opacity = 1;
            _worker.Start();

            // Windows drops windows out of the topmost band; re-assert cheaply.
            var onTop = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            onTop.Tick += (_, _) => KeepOnTop();
            onTop.Start();
        };
        SizeChanged += (_, _) => Place();
        Closed += (_, _) =>
        {
            _savePosition.Stop();
            _quit.Set();
            _tray?.Dispose();
            _tray = null;
        };
    }

    public bool TemperatureFirst => _temperatureFirst;

    /// <summary>Swaps which metric gets the big number and the bar; both stay visible.</summary>
    public void SetTemperatureFirst(bool temperature)
    {
        _temperatureFirst = temperature;
        AppSettings.TemperatureFirst = temperature;
        ShowMode();
        Apply(_last);
    }

    private void ShowMode()
    {
        ModeLabel.Text = _temperatureFirst ? "SICAKLIK" : "KULLANIM";
        TempItem.IsChecked = _temperatureFirst;
    }

    /// <summary>Compact card: the same three metrics as one slim row, without labels, bars or header.</summary>
    private void SetCompact(bool compact)
    {
        _compact = compact;
        AppSettings.Compact = compact;

        FullHeader.IsVisible = !compact;
        FullBody.IsVisible = !compact;
        CompactBody.IsVisible = compact;

        Root.Margin = compact ? new Thickness(14, 8, 14, 8) : new Thickness(18, 10, 18, 13);
        Width = compact ? 236 : 382;
        Height = compact ? 62 : 124;

        ScaleGlyph.Text = compact ? "⤢" : "⤡";
        ToolTip.SetTip(ScaleButton, compact ? "Tam görünüme dön" : "Kompakt görünüme geç");
        CompactItem.IsChecked = compact;
    }

    private void Place()
    {
        if (!_hudVisible || Screens.Primary is not { } screen) return;

        // Dragged by the user before: stay there, as long as that spot is still on a screen.
        if (AppSettings.Position is { } saved && OnScreen(saved.X, saved.Y))
        {
            Position = new PixelPoint(saved.X, saved.Y);
            return;
        }

        var wa = screen.WorkingArea;
        var scale = screen.Scaling;
        Position = new PixelPoint(
            wa.Right - (int)(Width * scale) - (int)(10 * scale),
            wa.Bottom - (int)(Height * scale) - (int)(8 * scale));
    }

    /// <summary>True when enough of the card is visible to grab it back after a monitor change.</summary>
    private bool OnScreen(int x, int y)
    {
        var scale = Screens.Primary?.Scaling ?? 1;
        var w = (int)(Width * scale);
        var h = (int)(Height * scale);
        foreach (var screen in Screens.All)
        {
            var overlap = screen.Bounds.Intersect(new PixelRect(x, y, w, h));
            if (overlap.Width >= (int)(120 * scale) && overlap.Height >= (int)(40 * scale)) return true;
        }
        return false;
    }

    private void SavePosition()
    {
        if (_hudVisible) AppSettings.Position = (Position.X, Position.Y);
    }

    private void Card_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (e.Source is Visual v && (v is Button || v.FindAncestorOfType<Button>() is not null)) return;

        // Native move loop: keeps the position in sync with the cursor on every platform.
        BeginMoveDrag(e);
    }

    private void AddTray()
    {
        try
        {
            _tray = new TrayIcon
            {
                Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://HeatLoad/Assets/app.png"))),
                ToolTipText = "HeatLoad",
            };
            var menu = new NativeMenu();
            menu.Add(Item("Göster / gizle", () => ToggleHud()));
            menu.Add(Item("Ayarlar…", OpenSettings));
            menu.Add(new NativeMenuItemSeparator());
            menu.Add(Item("Çıkış", ExitApp));
            _tray.Menu = menu;
            _tray.Clicked += (_, _) => ToggleHud();
            TrayIcon.SetIcons(Application.Current!, new TrayIcons { _tray });
        }
        catch
        {
            _tray = null; // no tray support (e.g. bare X11 session): the card still works
        }

        static NativeMenuItem Item(string header, Action action)
        {
            var item = new NativeMenuItem(header);
            item.Click += (_, _) => action();
            return item;
        }
    }

    private void ToggleHud()
    {
        _hudVisible = !_hudVisible;
        HideItem.Header = _hudVisible ? "Göstergeyi gizle" : "Göstergeyi göster";
        if (_hudVisible)
        {
            Show();
            Place();
            Topmost = true;
        }
        else
        {
            Hide();
        }
    }

    private void KeepOnTop()
    {
        if (HudMenu.IsOpen || _settings is { IsVisible: true }) return;

        if (OperatingSystem.IsWindows() && TryGetPlatformHandle()?.Handle is { } hwnd && hwnd != IntPtr.Zero)
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        else
            Topmost = true;
    }

    public void OpenSettings()
    {
        if (_settings is { IsVisible: true })
        {
            _settings.Activate();
            return;
        }

        _settings = new SettingsWindow(this);
        _settings.Closed += (_, _) => _settings = null;
        _settings.Show();
        _settings.Activate();
    }

    public void ExitApp()
    {
        _quit.Set();
        _tray?.Dispose();
        _tray = null;
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }

    private void SensorLoop()
    {
        try { _sensors = Sensors.Create(); } catch { }

        while (!_quit.IsSet)
        {
            Reading reading;
            try { reading = _sensors?.Sample() ?? default; }
            catch { reading = default; }
            _last = reading;

            try { Dispatcher.UIThread.Post(() => Apply(reading)); } catch { }

            _quit.Wait(1000);
        }

        try { _sensors?.Dispose(); } catch { }
    }

    private void Apply(Reading r)
    {
        Update(CpuValue, CpuUnit, CpuSub, CpuBar, CpuDot, AccentCpu, r.CpuUsage, r.CpuTemp, "—");
        Update(GpuValue, GpuUnit, GpuSub, GpuBar, GpuDot, AccentGpu, r.GpuUsage, r.GpuTemp, "—");
        Update(RamValue, RamUnit, RamSub, RamBar, RamDot, AccentRam, r.RamUsage, r.RamTemp,
            r.RamUsedGb is { } gb ? $"{gb:0.0} GB" : "—");
    }

    /// <summary>Big number + bar follow the selected metric; the other one sits in the corner.</summary>
    private void Update(TextBlock value, TextBlock unit, TextBlock sub, Border bar, Avalonia.Controls.Shapes.Ellipse dot,
                        IBrush accent, double? usage, double? temperature, string fallback)
    {
        var heat = temperature is >= 85 ? Hot : temperature is >= 75 ? Warm : null;
        var shown = _temperatureFirst ? temperature : usage;

        value.Text = shown is { } v ? Math.Round(v).ToString("0") : "—";
        unit.Text = _temperatureFirst ? "°" : "%";
        value.Foreground = _temperatureFirst && heat is not null ? heat : Normal;
        bar.Background = _temperatureFirst ? heat ?? accent : accent;

        sub.Text = _temperatureFirst
            ? usage is { } u ? Math.Round(u) + "%" : fallback
            : temperature is { } t ? Math.Round(t) + "°" : fallback;
        sub.Foreground = !_temperatureFirst && heat is not null ? heat : Muted;
        dot.Fill = heat ?? accent;

        var target = Math.Clamp((shown ?? 0) / 100.0, 0, 1);
        bar.RenderTransform = new ScaleTransform(target, 1);
    }

    private void Swap_Click(object? sender, RoutedEventArgs e) => SetTemperatureFirst(!_temperatureFirst);

    private void Scale_Click(object? sender, RoutedEventArgs e) => SetCompact(!_compact);

    private void CompactMode_Click(object? sender, RoutedEventArgs e) => SetCompact(CompactItem.IsChecked);

    private void TempMode_Click(object? sender, RoutedEventArgs e) => SetTemperatureFirst(TempItem.IsChecked);

    private void Hide_Click(object? sender, RoutedEventArgs e) => ToggleHud();

    private void Settings_Click(object? sender, RoutedEventArgs e) => OpenSettings();

    private void Exit_Click(object? sender, RoutedEventArgs e) => ExitApp();

    private static IBrush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        return brush.ToImmutable();
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
}

