using System.Windows;
using System.Windows.Input;

namespace HeatLoad;

public partial class SettingsWindow : Window
{
    private readonly MainWindow _hud;
    private bool _loading = true;

    public SettingsWindow(MainWindow hud)
    {
        InitializeComponent();
        _hud = hud;

        UsageSegment.IsChecked = !hud.TemperatureFirst;
        TempSegment.IsChecked = hud.TemperatureFirst;

        var autostart = Autostart.IsEnabled();
        StartSwitch.IsChecked = autostart is true;
        StartSwitch.IsEnabled = autostart is not null; // task missing (dev run): nothing to toggle

        _loading = false;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };

        // CenterScreen lands off-screen on this desktop; place it on the work area ourselves.
        var work = SystemParameters.WorkArea;
        Left = Math.Round(work.Left + (work.Width - Width) / 2);
        Top = Math.Round(work.Top + (work.Height - Height) / 2);
    }

    private void Mode_Click(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _hud.SetTemperatureFirst(TempSegment.IsChecked == true);
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_loading) return;

        var wanted = StartSwitch.IsChecked == true;
        if (Autostart.Set(wanted)) return;

        // schtasks failed: put the switch back where reality is, without re-entering this handler.
        _loading = true;
        StartSwitch.IsChecked = Autostart.IsEnabled() is true;
        _loading = false;
        MessageBox.Show(this, "Açılış ayarı değiştirilemedi.", "HeatLoad", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Card_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Quit_Click(object sender, RoutedEventArgs e) => _hud.ExitApp();
}
