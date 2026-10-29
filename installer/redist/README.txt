PawnIO 2.2.0 — bundled unmodified.

The PawnIO setup states it can be redistributed unmodified, which is why the original
executable ships inside HeatLoad-Setup.exe (optional task "pawnio", installed silently
with -install -silent when the box is ticked).

Source:  https://github.com/namazso/PawnIO.Setup/releases/tag/2.2.0
SHA256:  1F519A22E47187F70A1379A48CA604981C4FCF694F4E65B734AAA74A9FBA3032

LibreHardwareMonitor needs this driver to read CPU and DIMM temperatures on Windows.
Upgrade by replacing this file with a newer official setup, then bump the version in
installer/HeatLoad.iss and rebuild.
