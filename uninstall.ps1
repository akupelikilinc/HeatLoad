# HeatLoad kaldırma: görevi ve program dosyalarını siler. PawnIO sürücüsüne dokunmaz.
# Kullanım: uninstall.ps1

$ErrorActionPreference = 'Stop'

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Start-Process powershell -Verb RunAs -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"")
    exit
}

Stop-Process -Name HeatLoad -Force -ErrorAction SilentlyContinue
Unregister-ScheduledTask -TaskName 'HeatLoad' -Confirm:$false -ErrorAction SilentlyContinue
Remove-Item (Join-Path $env:ProgramFiles 'HeatLoad') -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item 'HKCU:\Software\HeatLoad' -Recurse -Force -ErrorAction SilentlyContinue

Write-Host 'HeatLoad kaldırıldı. (PawnIO sürücüsü kurulu bırakıldı.)'
