using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace HeatLoad;

public partial class SettingsWindow : Window
{
    private readonly MainWindow _hud;

    public SettingsWindow(MainWindow hud)
    {
        InitializeComponent();
        _hud = hud;

        UsageSegment.IsChecked = !hud.TemperatureFirst;
        TempSegment.IsChecked = hud.TemperatureFirst;

        var autostart = Autostart.IsEnabled();
        StartSwitch.IsChecked = autostart is true;
        StartSwitch.IsEnabled = autostart is not null; // task missing (dev run): nothing to toggle

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };

        Opened += (_, _) => CenterOnScreen();
    }

    private void CenterOnScreen()
    {
        if (Screens.Primary is not { } screen) return;
        var wa = screen.WorkingArea;
        var scale = screen.Scaling;
        Position = new PixelPoint(
            wa.X + (wa.Width - (int)(Width * scale)) / 2,
            wa.Y + (wa.Height - (int)(Height * scale)) / 2);
    }

    private void Mode_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true } rb && rb == TempSegment)
            _hud.SetTemperatureFirst(true);
        else
            _hud.SetTemperatureFirst(false);
    }

    private void Start_Click(object? sender, RoutedEventArgs e)
    {
        var wanted = StartSwitch.IsChecked == true;
        if (Autostart.Set(wanted))
        {
            StartError.IsVisible = false;
            return;
        }

        // The change failed: put the switch back where reality is.
        StartSwitch.IsChecked = Autostart.IsEnabled() is true;
        StartError.IsVisible = true;
    }

    private void Card_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    private void Quit_Click(object? sender, RoutedEventArgs e) => _hud.ExitApp();
}
