; HeatLoad kurulum paketi (Inno Setup 6). Derleme: ISCC.exe installer\HeatLoad.iss
; Uygulama Program Files'a kurulur ve açılışta yönetici olarak başlayan görevi oluşturur.
; Sensör sürücüsü (PawnIO) isteğe bağlı görev olarak birlikte gelir; varsayılan işaretli.

#define AppName "HeatLoad"
#define AppVersion "1.3.5"
#define AppPublisher "Ahmet KÜPELİKILINÇ"
#define AppURL "https://github.com/akupelikilinc/HeatLoad"

[Setup]
AppId={{8B1F6C34-2D7A-4E51-9C0B-5A6E3F21D7B4}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}/releases
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=..\dist
OutputBaseFilename=HeatLoad-Setup-{#AppVersion}
SetupIconFile=..\HeatLoad.App\Assets\app.ico
UninstallDisplayIcon={app}\{#AppName}.exe
UninstallDisplayName={#AppName}
LicenseFile=..\LICENSE
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes
CloseApplications=no
; Kullanıcı ayarı (settings.json) korunur: kurulum sırasında %APPDATA%\HeatLoad klasörüne dokunulmaz.

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "pawnio"; Description: "{cm:PawnIoTask}"; GroupDescription: "{cm:PawnIoGroup}"

[CustomMessages]
turkish.PawnIoGroup=Ek bileşen
turkish.PawnIoTask=CPU ve RAM sıcaklıkları için sensör sürücüsünü kur (PawnIO)
turkish.PawnIoStatus=Sensör sürücüsü kuruluyor…
english.PawnIoGroup=Optional component
english.PawnIoTask=Install the sensor driver for CPU and RAM temperatures (PawnIO)
english.PawnIoStatus=Installing the sensor driver…

[Files]
Source: "..\publish-selfcontained\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; PawnIO 2.2.0 (namazso, resmî sürüm): LibreHardwareMonitor'ün CPU/RAM sıcaklıkları için kullandığı sürücü.
; Lisansı değiştirilmeden yeniden dağıtıma izin verir; yalnızca görev seçiliyse geçici klasöre çıkarılır.
Source: "redist\PawnIO_setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Tasks: pawnio

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppName}.exe"
Name: "{group}\{#AppName} Ayarlar"; Filename: "{app}\{#AppName}.exe"; Parameters: "--settings"
Name: "{group}\{#AppName} kaldır"; Filename: "{uninstallexe}"

[Run]
; Önce sensör sürücüsü: uygulama açılışta sensörleri okuyor. Zaten kuruluysa bu adım atlanır.
Filename: "{tmp}\PawnIO_setup.exe"; Parameters: "-install -silent"; StatusMsg: "{cm:PawnIoStatus}"; Flags: runhidden waituntilterminated; Tasks: pawnio; Check: InstallPawnIo
; Görev, install.ps1 ile aynı biçimde kurulur: oturum açılışında, en yüksek yetkiyle, etkileşimli.
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""Register-ScheduledTask -TaskName HeatLoad -Action (New-ScheduledTaskAction -Execute '{app}\HeatLoad.exe') -Trigger (New-ScheduledTaskTrigger -AtLogOn) -Principal (New-ScheduledTaskPrincipal -UserId ($env:USERDOMAIN + '\' + $env:USERNAME) -LogonType Interactive -RunLevel Highest) -Force | Out-Null"""; Flags: runhidden
; runascurrentuser: the app manifest requires admin, so the post-install launch must inherit Setup's
; elevated token — the default (original user) fails with CreateProcess error 740.
Filename: "{app}\{#AppName}.exe"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[UninstallRun]
Filename: "{sys}\schtasks.exe"; Parameters: "/Delete /TN ""{#AppName}"" /F"; Flags: runhidden; RunOnceId: "DelTask"
Filename: "{sys}\taskkill.exe"; Parameters: "/IM {#AppName}.exe /F"; Flags: runhidden; RunOnceId: "StopApp"
Filename: "{sys}\reg.exe"; Parameters: "delete HKCU\Software\{#AppName} /f"; Flags: runhidden; RunOnceId: "DelSettings"

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
Type: filesandordirs; Name: "{userappdata}\HeatLoad"

[Code]
var
  KillResult: Integer;

{ PawnIO zaten kuruluysa kendi kurulumu "önce kaldırın" diyerek hata verir; o durumda adımı atla.
  Anahtar 64 bit görünümde ya da WOW6432Node altında olabilir. }
function InstallPawnIo: Boolean;
begin
  Result := not RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO') and
            not RegKeyExists(HKLM32, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO');
end;

{ Kurulum dosyaları kopyalamadan önce çalışan uygulamayı kapatır; aksi hâlde exe kilitli kalır. }
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/IM {#AppName}.exe /F', '', SW_HIDE, ewWaitUntilTerminated, KillResult);
end;
