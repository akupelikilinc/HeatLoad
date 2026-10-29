# HeatLoad

**English** · [Türkçe](README.tr.md)

A tiny always-on-top widget for **Windows, Linux and macOS** that shows live **CPU, GPU and RAM usage and temperature**, sitting right above the clock. No window chrome, no taskbar button — just a dark glass card and a tray icon.

**[Download](https://github.com/akupelikilinc/HeatLoad/releases/latest)** — `HeatLoad-Setup.exe` for Windows 10/11, `.tar.gz` archives for Linux (x64) and macOS (Apple Silicon / Intel). No .NET runtime needed.

<p>
  <img src="docs/usage.png" alt="HeatLoad in usage mode" width="382">
</p>

## Features

- **Live readings, once a second** — CPU load, GPU load, RAM load, plus CPU / GPU / RAM temperatures.
- **Two views, one click** — press the ⇄ arrow to swap between usage and temperature. The big number follows the active view, the corner badge keeps the other one visible.
- **Compact when you want quiet** — the ⤡ icon next to the arrow shrinks the card to a slim row with just the three numbers (colour-coded); ⤢ brings the full card back. The choice is remembered.
- **Built for the corner** — frameless, always on top, never steals focus, no taskbar entry. Drag it anywhere; the spot is remembered.
- **Tray resident** — left click toggles the widget, right click opens the menu.
- **Same menu everywhere** — right click the widget or the tray icon: hide/show, switch view, settings, quit.
- **Starts with your session** — Windows: a scheduled task with highest privileges, so temperatures work at logon with no UAC prompt. Linux: an XDG autostart entry. macOS: a launch agent.
- **No network, no telemetry** — every value is read locally: Win32, NVML and LibreHardwareMonitor on Windows, `/proc` and hwmon on Linux, Mach/sysctl on macOS.

## Screenshots

**Temperature view** — same card, the hot value goes big, the load stays in the corner.

<img src="docs/temperature.png" alt="Temperature mode" width="382">

**Right-click menu** — identical on the widget and on the tray icon.

<img src="docs/menu.png" alt="Context menu" width="403">

**Settings** — default view, start with Windows, hide the widget.

<img src="docs/settings.png" alt="Settings window" width="330">

**Compact view** — the ⤡ icon turns the card into a slim row with the three numbers only.

<img src="docs/compact.png" alt="Compact view" width="236">

> The interface is currently in Turkish. The menu is: *show/hide widget*, *show temperatures*, *compact view*, *settings…*, *quit*.

Every release, with its own screenshots: **[CHANGELOG.md](CHANGELOG.md)**.

## Requirements

**Windows**

- Windows 10 or 11, 64-bit
- Administrator rights — Windows does not expose CPU/board temperature sensors to normal processes
- [PawnIO](https://pawnio.eu/) — the low-level driver LibreHardwareMonitor uses to read sensors on modern AMD CPUs (install it once; it asks for a single UAC confirmation)
- NVIDIA GPU load and temperature come from NVML; any other card, integrated included, is read through LibreHardwareMonitor

**Linux**

- x64, glibc 2.31+ (Ubuntu 20.04 or newer, Fedora 32+, …)
- CPU and RAM need nothing extra; CPU/GPU temperatures are read from `/sys/class/hwmon` (kernel drivers such as `coretemp`, `k10temp`, `amdgpu`)
- NVIDIA GPU load/temperature use `libnvidia-ml.so.1` from the driver; AMD GPU load uses the `amdgpu` sysfs node. Intel integrated GPUs expose no busy counter there, so their values stay `—`

**macOS**

- macOS 12+, Apple Silicon or Intel
- CPU and RAM work out of the box. Temperatures and GPU load are not available yet: macOS exposes them only through SMC/IOKit, which needs extra privileged plumbing — those fields stay `—`

**.NET 10 SDK** — only when building from source; the release builds are self-contained

## Install

### Setup.exe (Windows, no SDK needed)

Download **[HeatLoad-Setup.exe](https://github.com/akupelikilinc/HeatLoad/releases/latest)** and run it. It is a 51 MB self-contained installer — no .NET runtime, no PowerShell script, no SDK.

It installs to `C:\Program Files\HeatLoad`, registers the logon task, adds Start-menu entries and appears in *Apps & features* for uninstalling. Only the .NET-free build and PawnIO (see Requirements) are needed.

> Windows SmartScreen may warn because the installer is not code-signed: *More info* → *Run anyway*.

### Linux / macOS (archives)

Download the archive for your machine, unpack it and run `HeatLoad`:

```bash
# Linux
tar -xzf HeatLoad-linux-x64.tar.gz
./HeatLoad

# macOS (Apple Silicon: HeatLoad-macos-arm64, Intel: HeatLoad-macos-x64)
tar -xzf HeatLoad-macos-arm64.tar.gz
open HeatLoad.app
```

The macOS build is not notarized: the first start needs *right click → Open*, or `xattr -dr com.apple.quarantine HeatLoad.app`.

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
# Windows
dotnet run --project .\HeatLoad.App

# Linux / macOS
dotnet run --project HeatLoad.App
```

Without administrator rights on Windows the app still runs, but temperatures read as `0`.

### Uninstall

Installed with Setup.exe? Remove it from *Settings → Apps → Installed apps → HeatLoad*. Otherwise:

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

This stops the app, deletes the scheduled task, `C:\Program Files\HeatLoad`, the `HKCU\Software\HeatLoad` key (older versions) and the settings file. The PawnIO driver is left installed — remove it from *Apps & features* if you do not need it.

On Linux/macOS remove `HeatLoad.app` (or the `HeatLoad` binary) and, if you enabled it, the autostart entry:

```bash
# Linux
rm -f ~/.config/autostart/heatload.desktop
# macOS
rm -f ~/Library/LaunchAgents/com.heatload.HeatLoad.plist
```

## Usage

| Action | Result |
|:--|:--|
| Click ⇄ on the card | Swap the big value between usage and temperature |
| Click ⤡ / ⤢ | Compact view (three numbers) / full card — remembered |
| Drag the card | Move it anywhere on any monitor; it stays where you drop it |
| Right click the card | Menu: hide/show, temperature view, compact view, settings, quit |
| Left click the tray icon | Show or hide the widget |
| Right click the tray icon | The same menu |
| Session starts | Widget appears automatically, already elevated (Windows) |

The widget hides itself (`Hide()`), so nothing is left behind in the taskbar or Alt+Tab.

The chosen view and the card's position are remembered in a small JSON file: `%APPDATA%\HeatLoad\settings.json` on Windows, `~/.config/HeatLoad/settings.json` on Linux, `~/Library/Application Support/HeatLoad/settings.json` on macOS. With no saved position it sits above the clock again.

### Settings

Open **Ayarlar…** from the menu:

- **Kullanım / Sıcaklık** — the default view.
- **Açılışta başlat** — enable or disable autostart (Windows: the logon task via `schtasks`; Linux: `~/.config/autostart/heatload.desktop`; macOS: `~/Library/LaunchAgents/com.heatload.HeatLoad.plist`, active from the next logon).
- **Göstergeyi kapat** — hide the widget and keep the app in the tray.

Close it with `Esc`, the ✕ button, or by clicking anywhere outside.

### Tray icon

Windows 11 hides new tray icons in the overflow panel (the `^` arrow next to the clock). Drag the HeatLoad icon onto the taskbar once and Windows keeps it there.

## How it works

| Part | Windows | Linux | macOS |
|:--|:--|:--|:--|
| CPU load | `GetSystemTimes` deltas | `/proc/stat` deltas | `host_statistics` (Mach) |
| RAM load | `GlobalMemoryStatusEx` | `/proc/meminfo` | `hw.memsize` + `vm.page_*_count` |
| GPU load | NVML (`nvml.dll`), else LibreHardwareMonitor | NVML (`libnvidia-ml.so.1`) or `gpu_busy_percent` | — |
| CPU / GPU / RAM temperature | LibreHardwareMonitor + NVML | `/sys/class/hwmon`, NVML | — |

Everything runs in one 1-second background thread; readings are non-blocking OS calls plus a couple of driver queries. The UI is a single Avalonia window: frameless, transparent, always on top, and on Windows with `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE` so it never takes focus.

## Troubleshooting

**Temperatures are 0.**
The process is not elevated, or PawnIO is missing. Install PawnIO, then start the app through the scheduled task (`Start-ScheduledTask -TaskName HeatLoad`) or run your terminal as administrator.

**GPU values are 0 or missing.**
NVML reports NVIDIA cards only. Without one the widget reads whatever card LibreHardwareMonitor found — AMD or Intel, integrated included — and picks it once at startup. An integrated GPU often has no core temperature sensor, so its usage shows live while the temperature stays `—`.

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
HeatLoad.App/
  App.axaml(.cs)               Single instance, crash log, dark styles
  MainWindow.axaml(.cs)        The widget, tray icon, menu, mode switching, topmost guard
  SettingsWindow.axaml(.cs)    Settings card
  Reading.cs                   Reading record + per-OS sensor factory
  Sensors/WindowsSensors.cs    Win32 + NVML + LibreHardwareMonitor
  Sensors/LinuxSensors.cs      /proc, hwmon, NVML, amdgpu
  Sensors/MacSensors.cs        Mach host_statistics + sysctl
  Autostart.cs                 schtasks / XDG autostart / launch agent
  AppSettings.cs               JSON settings (%APPDATA%\HeatLoad\settings.json)
install.ps1                    Publish to Program Files + logon task
uninstall.ps1                  Remove task, files, settings
make-icon.ps1                  Regenerate app.ico
installer/HeatLoad.iss         Inno Setup script for HeatLoad-Setup.exe
.github/workflows/build.yml    Windows / Linux / macOS release builds
```

## Building the installer

```powershell
dotnet publish HeatLoad.App -c Release -r win-x64 --self-contained true -o publish-selfcontained `
  -p:SatelliteResourceLanguages=en -p:DebugType=None -p:DebugSymbols=false
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer\HeatLoad.iss
```

The Setup lands in `dist\`. It bundles the self-contained publish (no .NET runtime needed on the target), plus the logon task and the uninstaller.

Linux and macOS builds are produced by GitHub Actions (`.github/workflows/build.yml`, also on tag pushes): `HeatLoad-linux-x64.tar.gz`, `HeatLoad-macos-arm64.tar.gz`, `HeatLoad-macos-x64.tar.gz`.

## License

[MIT](LICENSE)
