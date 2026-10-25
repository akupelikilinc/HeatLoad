# HeatLoad kurulumu: derler, Program Files'a kopyalar, açılışta yönetici olarak başlatan görevi ekler.
# Kullanım: install.ps1 (yönetici ister; gerekirse kendini yükseltir)

$ErrorActionPreference = 'Stop'

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Start-Process powershell -Verb RunAs -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"")
    exit
}

$project = Join-Path $PSScriptRoot 'HeatLoad'
$dest = Join-Path $env:ProgramFiles 'HeatLoad'

Stop-Process -Name HeatLoad -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 400

Write-Host 'Derleniyor...'
dotnet publish $project -c Release -o $dest --nologo | Out-Null

$action = New-ScheduledTaskAction -Execute (Join-Path $dest 'HeatLoad.exe')
$trigger = New-ScheduledTaskTrigger -AtLogOn -User "$env:USERDOMAIN\$env:USERNAME"
$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Highest
Register-ScheduledTask -TaskName 'HeatLoad' -Action $action -Trigger $trigger -Principal $principal -Force | Out-Null

Start-ScheduledTask -TaskName 'HeatLoad'
Write-Host "Kuruldu: $dest"
Write-Host 'Görev: HeatLoad (oturum açılışında, yönetici olarak)'
