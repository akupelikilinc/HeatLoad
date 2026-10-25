using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace HeatLoad;

public partial class MainWindow : Window
{
    private readonly ManualResetEventSlim _quit = new(false);
    private readonly Thread _worker;
    private Collector? _collector;
    private WinForms.NotifyIcon? _tray;
    private SettingsWindow? _settings;
    private Reading _last;
    private bool _temperatureFirst = AppSettings.TemperatureFirst;
    private bool _compact = AppSettings.Compact;
    private bool _hudVisible = true;
    private Point _dragFromCursor;
    private Point _dragFromWindow;
    private bool _dragging;

    private static readonly Brush Normal = Frozen(0xF2, 0xF6, 0xFC);
    private static readonly Brush Muted = Frozen(0x9A, 0xA4, 0xB3);
    private static readonly Brush Warm = Frozen(0xF2, 0xC1, 0x6B);
    private static readonly Brush Hot = Frozen(0xFF, 0x7B, 0x7B);
    private static readonly Brush AccentCpu = Frozen(0x5B, 0xC8, 0xFF);
    private static readonly Brush AccentGpu = Frozen(0x6F, 0xE7, 0xB7);
    private static readonly Brush AccentRam = Frozen(0xB7, 0xA6, 0xFF);

    public MainWindow()
    {
        InitializeComponent();
        _worker = new Thread(SensorLoop) { IsBackground = true, Name = "HeatLoad.Sensors" };
        ShowMode();
        SetCompact(_compact);

        Loaded += (_, _) =>
        {
            Place();
            FadeIn();
            _worker.Start();

            // Explorer may still be starting at logon; re-place once the taskbar settles.
            var settle = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            settle.Tick += (_, _) => { Place(); settle.Stop(); };
            settle.Start();
        };
        SizeChanged += (_, _) => Place();
        SystemParameters.StaticPropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SystemParameters.WorkArea)) Place();
        };
        Closed += (_, _) =>
        {
            _quit.Set();
            _tray?.Dispose();
            _tray = null;
        };
        SourceInitialized += (_, _) =>
        {
            MakeOverlay();
            AddTray();
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

        FullHeader.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        FullBody.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        CompactBody.Visibility = compact ? Visibility.Visible : Visibility.Collapsed;

        Root.Margin = compact ? new Thickness(14, 8, 14, 8) : new Thickness(18, 10, 18, 13);
        Width = compact ? 236 : 382;
        Height = compact ? 62 : 124;

        ScaleGlyph.Text = compact ? "\uE740" : "\uE73F";
        MiniScaleGlyph.Text = compact ? "\uE740" : "\uE73F";
        ScaleButton.ToolTip = compact ? "Tam görünüme dön" : "Kompakt görünüme geç";
        CompactItem.IsChecked = compact;
    }

    private void Place()
    {
        if (!_hudVisible) return;

        // Dragged by the user before: stay there, as long as that spot is still on a screen.
        if (AppSettings.Position is { } saved && OnScreen(saved.X, saved.Y))
        {
            Left = saved.X;
            Top = saved.Y;
            return;
        }

        var work = SystemParameters.WorkArea;
        Left = Math.Round(work.Right - ActualWidth - 10);
        Top = Math.Round(work.Bottom - ActualHeight - 8);
    }

    /// <summary>True when enough of the card is visible to grab it back after a monitor change.</summary>
    private bool OnScreen(double x, double y)
    {
        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        var visible = Rect.Intersect(
            new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                     SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight),
            new Rect(x, y, width, height));
        return visible.Width >= 120 && visible.Height >= 40;
    }

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // No capture yet: a plain click still has to reach the swap arrow and the menu.
        _dragFromCursor = PointToScreen(e.GetPosition(this));
        _dragFromWindow = new Point(Left, Top);
        _dragging = false;
    }

    private void Card_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var now = PointToScreen(e.GetPosition(this));
        var dx = now.X - _dragFromCursor.X;
        var dy = now.Y - _dragFromCursor.Y;

        // Small movements stay clicks, so the arrow button keeps working.
        if (!_dragging && Math.Abs(dx) < 4 && Math.Abs(dy) < 4) return;

        if (!_dragging)
        {
            _dragging = true;
            Card.CaptureMouse();
        }

        Left = Math.Round(_dragFromWindow.X + dx);
        Top = Math.Round(_dragFromWindow.Y + dy);
    }

    private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;

        _dragging = false;
        Card.ReleaseMouseCapture();
        AppSettings.Position = ((int)Math.Round(Left), (int)Math.Round(Top));
        e.Handled = true; // the drop must not also count as a click on whatever is under the cursor
    }

    private void FadeIn()
    {
        var slide = (TranslateTransform)Card.RenderTransform;
        slide.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(420)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        Card.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(420)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }

    private void MakeOverlay()
    {
        // No WS_EX_TRANSPARENT: the card takes clicks now. Tool window + no activate keep it out of Alt-Tab and focus.
        var hwnd = new WindowInteropHelper(this).Handle;
        var ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    private void AddTray()
    {
        var icon = Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? Drawing.SystemIcons.Application;
        _tray = new WinForms.NotifyIcon { Icon = icon, Text = "HeatLoad", Visible = true };
        _tray.MouseUp += (_, e) =>
        {
            if (e.Button == WinForms.MouseButtons.Left) ToggleHud();
            else if (e.Button == WinForms.MouseButtons.Right) OpenMenu();
        };
    }

    private void OpenMenu()
    {
        HudMenu.Placement = PlacementMode.MousePoint;
        HudMenu.IsOpen = true;
    }

    private void ToggleHud()
    {
        _hudVisible = !_hudVisible;
        if (_hudVisible) Place();
        else Left = SystemParameters.VirtualScreenLeft - 10000;
        HideItem.Header = _hudVisible ? "Göstergeyi gizle" : "Göstergeyi göster";
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
        Application.Current.Shutdown();
    }

    private void SensorLoop()
    {
        try { _collector = new Collector(); } catch { }

        while (!_quit.IsSet)
        {
            Reading reading;
            try { reading = _collector?.Sample() ?? default; }
            catch { reading = default; }
            _last = reading;

            try { Dispatcher.BeginInvoke(() => Apply(reading)); } catch { }

            _quit.Wait(1000);
        }
    }

    private void Apply(Reading r)
    {
        Update(CpuValue, CpuUnit, CpuSub, CpuBar, CpuDot, AccentCpu, r.CpuUsage, r.CpuTemp, "—");
        Update(GpuValue, GpuUnit, GpuSub, GpuBar, GpuDot, AccentGpu, r.GpuUsage, r.GpuTemp, "—");
        Update(RamValue, RamUnit, RamSub, RamBar, RamDot, AccentRam, r.RamUsage, r.RamTemp,
            r.RamUsedGb is { } gb ? $"{gb:0.0} GB" : "—");
    }

    /// <summary>Big number + bar follow the selected metric; the other one sits in the corner.</summary>
    private void Update(TextBlock value, TextBlock unit, TextBlock sub, Border bar, Ellipse dot,
                        Brush accent, double? usage, double? temperature, string fallback)
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

        var scale = (ScaleTransform)bar.RenderTransform;
        var target = Math.Clamp((shown ?? 0) / 100.0, 0, 1);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(target, TimeSpan.FromMilliseconds(450)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }

    private void Swap_Click(object sender, RoutedEventArgs e) => SetTemperatureFirst(!_temperatureFirst);

    private void Scale_Click(object sender, RoutedEventArgs e) => SetCompact(!_compact);

    private void CompactMode_Click(object sender, RoutedEventArgs e) => SetCompact(CompactItem.IsChecked);

    private void TempMode_Click(object sender, RoutedEventArgs e) => SetTemperatureFirst(TempItem.IsChecked);

    private void Hide_Click(object sender, RoutedEventArgs e) => ToggleHud();

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void Exit_Click(object sender, RoutedEventArgs e) => ExitApp();

    private static Brush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
