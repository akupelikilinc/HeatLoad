# HeatLoad

**English** · [Türkçe](README.tr.md)

A tiny always-on-top widget for Windows that shows live **CPU, GPU and RAM usage and temperature**, sitting right above the clock. No window chrome, no taskbar button — just a dark glass card and a tray icon.

**[Download HeatLoad-Setup.exe](https://github.com/akupelikilinc/HeatLoad/releases/latest)** — Windows 10/11, 51 MB, no .NET runtime needed.

<p>
  <img src="docs/usage.png" alt="HeatLoad in usage mode" width="382">
</p>

## Features

- **Live readings, once a second** — CPU load, GPU load, RAM load, plus CPU / GPU / RAM temperatures.
- **Two views, one click** — press the ⇄ arrow to swap between usage and temperature. The big number follows the active view, the corner badge keeps the other one visible.
- **Built for the corner** — frameless, always on top, never steals focus, no taskbar entry. Drag it anywhere; the spot is remembered.
- **Tray resident** — left click toggles the widget, right click opens the menu.
- **Same menu everywhere** — right click the widget or the tray icon: hide/show, switch view, settings, quit.
- **Starts with Windows** — via a scheduled task with highest privileges, so temperatures work at logon with no UAC prompt.
- **No network, no telemetry** — every value is read locally through Win32, NVML and LibreHardwareMonitor.

## Screenshots

**Temperature view** — same card, the hot value goes big, the load stays in the corner.

<img src="docs/temperature.png" alt="Temperature mode" width="382">

**Right-click menu** — identical on the widget and on the tray icon.

<img src="docs/menu.png" alt="Context menu" width="440">

**Settings** — default view, start with Windows, hide the widget.

<img src="docs/settings.png" alt="Settings window" width="330">

> The interface is currently in Turkish. The menu is: *show/hide widget*, *show temperatures*, *settings…*, *quit*.

## Requirements

- Windows 10 or 11, 64-bit
- [.NET 10 SDK](https://dotnet.microsoft.com/download) — only when installing from source; the Setup.exe build is self-contained
- Administrator rights — Windows does not expose CPU/board temperature sensors to normal processes
- [PawnIO](https://pawnio.eu/) — the low-level driver LibreHardwareMonitor uses to read sensors on modern AMD CPUs (install it once; it asks for a single UAC confirmation)
- NVIDIA GPU temperatures come from NVML, so any recent GeForce driver is enough (AMD GPU temperatures are not read yet)

## Install

### Setup.exe (no SDK needed)

Download **[HeatLoad-Setup-1.1.0.exe](https://github.com/akupelikilinc/HeatLoad/releases/latest)** and run it. It is a 51 MB self-contained installer — no .NET runtime, no PowerShell script, no SDK.

It installs to `C:\Program Files\HeatLoad`, registers the logon task, adds Start-menu entries and appears in *Apps & features* for uninstalling. Only the .NET-free build and PawnIO (see Requirements) are needed.

> Windows SmartScreen may warn because the installer is not code-signed: *More info* → *Run anyway*.

### From source

Clone the repository and run the install script. It builds the app, copies it to `C:\Program Files\HeatLoad` and registers the logon task.

```powershell
git clone https://github.com/akupelikilinc/HeatLoad.git
cd HeatLoad
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

The script elevates itself, publishes the project, registers the scheduled task **HeatLoad** (`At log on`, `Run with highest privileges`) and starts it. From the next logon the widget appears on its own, without a UAC prompt.

> `.NET 10 SDK` must be on `PATH`. If `dotnet` is missing, install it first.

### Run without installing

Useful while developing — starts the app from the build output, no scheduled task:

```powershell
dotnet run --project .\HeatLoad
```

Without administrator rights the app still runs, but temperatures read as `0`.

### Uninstall

Installed with Setup.exe? Remove it from *Settings → Apps → Installed apps → HeatLoad*. Otherwise:

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

This stops the app, deletes the scheduled task, `C:\Program Files\HeatLoad` and the `HKCU\Software\HeatLoad` settings key. The PawnIO driver is left installed — remove it from *Apps & features* if you do not need it.

## Usage

| Action | Result |
|:--|:--|
| Click ⇄ on the card | Swap the big value between usage and temperature |
| Drag the card | Move it anywhere on any monitor; it stays where you drop it |
| Right click the card | Menu: hide/show, temperature view, settings, quit |
| Left click the tray icon | Show or hide the widget |
| Right click the tray icon | The same menu |
| Windows starts | Widget appears automatically, already elevated |

The widget hides by moving off-screen, so nothing is left behind in the taskbar or Alt+Tab.

The chosen view is remembered in `HKCU\Software\HeatLoad` (`Mode`: `0` = usage, `1` = temperature). The card's own position is stored there too (`X`, `Y`); with no saved position it sits above the clock again.

### Settings

Open **Ayarlar…** from the menu:

- **Kullanım / Sıcaklık** — the default view.
- **Açılışta başlat** — enable or disable the logon task (`schtasks`), effective immediately.
- **Göstergeyi kapat** — hide the widget and keep the app in the tray.

Close it with `Esc`, the ✕ button, or by clicking anywhere outside.

### Tray icon

Windows 11 hides new tray icons in the overflow panel (the `^` arrow next to the clock). Drag the HeatLoad icon onto the taskbar once and Windows keeps it there.

## How it works

| Part | Source |
|:--|:--|
| CPU load | `GetSystemTimes` deltas between samples |
| RAM load | `GlobalMemoryStatusEx` |
| GPU load | NVML (`nvml.dll`) — `nvmlDeviceGetUtilizationRates` |
| CPU temperature | LibreHardwareMonitor — `Core (Tctl/Tdie)` |
| Motherboard / RAM temperature | LibreHardwareMonitor — DIMM SPD sensor |
| GPU temperature | NVML — `nvmlDeviceGetTemperature` |

Everything runs in one 1-second `DispatcherTimer` on the UI thread; readings are non-blocking Win32 calls plus a couple of driver queries. The app is a single WPF window with `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE` and topmost style, so it never takes focus.

## Troubleshooting

**Temperatures are 0.**
The process is not elevated, or PawnIO is missing. Install PawnIO, then start the app through the scheduled task (`Start-ScheduledTask -TaskName HeatLoad`) or run your terminal as administrator.

**GPU values are 0 or missing.**
NVML reports NVIDIA cards only. Check that the driver is installed — `nvidia-smi` should list the card.

**No tray icon.**
Windows 11 puts it in the overflow panel; drag it onto the taskbar. If it is not there either, the widget may have been hidden while the tray icon failed to create — run `install.ps1` again.

**Nothing appears at all.**
Check `%LOCALAPPDATA%\HeatLoad\error.log`. That file is only written on a crash.

**SmartScreen / antivirus warning on install.**
The executable is not code-signed. Build it yourself from source, or allow the file in your antivirus.

**The card is in the way.**
Right click → *Göstergeyi gizle*, or left click the tray icon. The flip arrow and the menu need clicks, so the card is not click-through.

## Project layout

```
HeatLoad/
  App.xaml, App.xaml.cs        Single instance, crash log, dark menu styles
  MainWindow.xaml(.cs)         The widget, tray icon, menu, mode switching
  SettingsWindow.xaml(.cs)     Settings card
  Collector.cs                 Win32 + NVML + LibreHardwareMonitor sampling
  Autostart.cs                 schtasks query/enable/disable
  AppSettings.cs               HKCU settings
  app.manifest                 requireAdministrator, PerMonitorV2
  app.ico                      Tray and exe icon (make-icon.ps1 regenerates it)
install.ps1                    Publish to Program Files + logon task
uninstall.ps1                  Remove task, files, settings
make-icon.ps1                  Regenerate app.ico
installer/HeatLoad.iss         Inno Setup script for HeatLoad-Setup.exe
```

## Building the installer

```powershell
dotnet publish HeatLoad -c Release -r win-x64 --self-contained true -o publish-selfcontained `
  -p:SatelliteResourceLanguages=en -p:DebugType=None -p:DebugSymbols=false
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer\HeatLoad.iss
```

The Setup lands in `dist\`. It bundles the self-contained publish (no .NET runtime needed on the target), plus the logon task and the uninstaller.

## License

[MIT](LICENSE)
