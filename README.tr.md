# HeatLoad

[English](README.md) · **Türkçe**

Windows, Linux ve macOS için, saatin hemen üstünde duran küçük bir masaüstü widget'ı: **CPU, GPU ve RAM kullanımı ile sıcaklıklarını** canlı gösterir. Pencere çerçevesi yok, görev çubuğu düğmesi yok — sadece koyu cam görünümlü bir kart ve bir tepsi simgesi.

**[İndir](https://github.com/akupelikilinc/HeatLoad/releases/latest)** — Windows 10/11 için `HeatLoad-Setup.exe`, Linux (x64) ve macOS (Apple Silicon / Intel) için `.tar.gz` arşivleri. .NET çalışma zamanı gerekmez.

<p>
  <img src="docs/usage.png" alt="HeatLoad kullanım görünümü" width="382">
</p>

## Özellikler

- **Saniyede bir canlı veri** — CPU, GPU ve RAM kullanımı; CPU, GPU ve RAM sıcaklıkları.
- **Tek tıkla iki görünüm** — ⇄ okuyla kullanım ile sıcaklık arasında geçiş. Büyük değer seçili görünümü izler, köşedeki rozet diğerini göstermeye devam eder.
- **Sessiz kalmak istediğinde kompakt** — okun yanındaki ⤡ ikonu kartı yalnızca üç sayıdan (renk kodlu) oluşan ince bir şeride indirir; ⤢ tam kartı geri getirir. Seçim hatırlanır.
- **Köşe için tasarlandı** — çerçevesiz, her zaman üstte, odağı çalmaz, görev çubuğunda yer kaplamaz. İstediğin yere sürükleyebilirsin; bıraktığın yeri hatırlar.
- **Tepside yaşar** — sol tık göstergeyi açar/kapatır, sağ tık menüyü açar.
- **Her yerde aynı menü** — göstergede veya tepsi simgesinde sağ tık: gizle/göster, görünüm değiştir, ayarlar, çıkış.
- **Oturum açılışında başlar** — Windows: en yüksek yetkili zamanlanmış görev sayesinde UAC sormadan başlar. Linux: XDG autostart girdisi. macOS: launch agent.
- **Ağ yok, telemetri yok** — tüm değerler yerelde okunur: Windows'ta Win32, NVML ve LibreHardwareMonitor; Linux'ta `/proc` ve hwmon; macOS'ta Mach/sysctl.

## Ekran görüntüleri

**Sıcaklık görünümü** — aynı kart, sıcak değer büyür, yük köşede kalır.

<img src="docs/temperature.png" alt="Sıcaklık görünümü" width="382">

**Sağ tık menüsü** — göstergede ve tepsi simgesinde aynı.

<img src="docs/menu.png" alt="Sağ tık menüsü" width="403">

**Ayarlar** — varsayılan görünüm, açılışta başlat, göstergeyi kapat.

<img src="docs/settings.png" alt="Ayarlar penceresi" width="330">

**Kompakt görünüm** — ⤡ ikonu kartı yalnızca üç sayıdan oluşan ince bir şeride indirir.

<img src="docs/compact.png" alt="Kompakt görünüm" width="236">

Her sürümün değişiklikleri ve görselleri: **[CHANGELOG.md](CHANGELOG.md)**.

## Gereksinimler

**Windows**

- Windows 10 veya 11, 64 bit
- Yönetici yetkisi — Windows, CPU ve anakart sıcaklık sensörlerini normal süreçlere açmaz
- [PawnIO](https://pawnio.eu/) — LibreHardwareMonitor'ün yeni AMD işlemcilerde sensör okumak için kullandığı düşük seviyeli sürücü (bir kez kurulur, tek UAC onayı ister)
- NVIDIA ekran kartı yükü ve sıcaklığı NVML'den gelir; diğer kartlar (dahili GPU dahil) LibreHardwareMonitor üzerinden okunur

**Linux**

- x64, glibc 2.31+ (Ubuntu 20.04 veya daha yeni, Fedora 32+, …)
- CPU ve RAM için ek bir şey gerekmez; CPU/GPU sıcaklıkları `/sys/class/hwmon` üzerinden okunur (`coretemp`, `k10temp`, `amdgpu` gibi çekirdek sürücüleri)
- NVIDIA GPU yükü/sıcaklığı sürücüyle gelen `libnvidia-ml.so.1` üzerinden; AMD GPU yükü `amdgpu` sysfs düğümünden. Intel dahili GPU'lar orada meşguliyet sayacı sunmaz, bu yüzden değerleri `—` kalır

**macOS**

- macOS 12+, Apple Silicon veya Intel
- CPU ve RAM doğrudan çalışır. Sıcaklık ve GPU yükü henüz yok: macOS bunları yalnızca SMC/IOKit üzerinden, ek yetkilerle veriyor — bu alanlar `—` kalır

**.NET 10 SDK** — yalnızca kaynaktan derlemede gerekir; sürüm paketleri kendinden içeriklidir

## Kurulum

### Setup.exe (SDK gerekmez)

**[HeatLoad-Setup.exe](https://github.com/akupelikilinc/HeatLoad/releases/latest)** dosyasını indirip çalıştırın. 51 MB'lık kendinden içerikli kurulum paketi — .NET çalışma zamanı, PowerShell betiği veya SDK gerekmez.

`C:\Program Files\HeatLoad` altına kurar, açılış görevini oluşturur, başlat menüsüne kısayol ekler ve *Uygulamalar ve özellikler* listesine yazılır. Tek ek gereksinim PawnIO (bkz. Gereksinimler).

> Kurulum paketi kod imzalı olmadığı için SmartScreen uyarabilir: *Daha fazla bilgi* → *Yine de çalıştır*.

### Linux / macOS (arşivler)

Makinenize uygun arşivi indirip açın ve `HeatLoad`'u çalıştırın:

```bash
# Linux
tar -xzf HeatLoad-linux-x64.tar.gz
./HeatLoad

# macOS (Apple Silicon: HeatLoad-macos-arm64, Intel: HeatLoad-macos-x64)
tar -xzf HeatLoad-macos-arm64.tar.gz
open HeatLoad.app
```

macOS derlemesi noterlenmiş değildir: ilk açılışta *sağ tık → Aç* ya da `xattr -dr com.apple.quarantine HeatLoad.app` gerekir.

### Kaynaktan

Depoyu klonlayın ve kurulum betiğini çalıştırın. Uygulamayı derler, `C:\Program Files\HeatLoad` altına kopyalar ve açılış görevini ekler.

```powershell
git clone https://github.com/akupelikilinc/HeatLoad.git
cd HeatLoad
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

Betik kendini yönetici olarak yükseltir, projeyi yayımlar, **HeatLoad** adlı zamanlanmış görevi (`Oturum açılışında`, `En yüksek ayrıcalıklarla çalıştır`) oluşturur ve başlatır. Bir sonraki oturum açılışından itibaren gösterge UAC sormadan kendiliğinden gelir.

> `dotnet` komutunun `PATH` üzerinde olması gerekir. Yoksa önce .NET 10 SDK'yı kurun.

### Kurmadan çalıştırma

Geliştirirken işe yarar — zamanlanmış görev oluşturmadan derleme çıktısından çalıştırır:

```powershell
# Windows
dotnet run --project .\HeatLoad.App

# Linux / macOS
dotnet run --project HeatLoad.App
```

Yönetici yetkisi olmadan da açılır, ancak Windows'ta sıcaklıklar `0` görünür.

### Kaldırma

Setup.exe ile kurduysanız *Ayarlar → Uygulamalar → Yüklü uygulamalar → HeatLoad* üzerinden kaldırın. Kurulum betiğiyle kurduysanız:

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

Uygulamayı durdurur, zamanlanmış görevi, `C:\Program Files\HeatLoad` klasörünü, `HKCU\Software\HeatLoad` anahtarını (eski sürümler) ve ayar dosyasını siler. PawnIO sürücüsüne dokunmaz — gerekmiyorsa *Uygulamalar ve özellikler*'den kaldırabilirsiniz.

Linux/macOS'ta `HeatLoad.app` (veya `HeatLoad` dosyası) ile birlikte, açtıysanız otomatik başlatma girdisini silin:

```bash
# Linux
rm -f ~/.config/autostart/heatload.desktop
# macOS
rm -f ~/Library/LaunchAgents/com.heatload.HeatLoad.plist
```

## Kullanım

| Eylem | Sonuç |
|:--|:--|
| Karttaki ⇄ | Büyük değeri kullanım ile sıcaklık arasında değiştirir |
| Karttaki ⤡ / ⤢ | Kompakt görünüm (üç sayı) / tam kart — hatırlanır |
| Kartı sürükle | İstediğin yere, istediğin monitöre taşır; bıraktığın yerde kalır |
| Karta sağ tık | Menü: gizle/göster, sıcaklık görünümü, kompakt görünüm, ayarlar, çıkış |
| Tepsi simgesine sol tık | Göstergeyi gösterir veya gizler |
| Tepsi simgesine sağ tık | Aynı menü |
| Oturum açılışı | Gösterge kendiliğinden, yönetici olarak gelir (Windows) |

Gösterge gizlenir; görev çubuğunda veya Alt+Tab listesinde iz bırakmaz.

Seçilen görünüm ve kartın konumu küçük bir JSON dosyasında saklanır: Windows'ta `%APPDATA%\HeatLoad\settings.json`, Linux'ta `~/.config/HeatLoad/settings.json`, macOS'ta `~/Library/Application Support/HeatLoad/settings.json`. Kayıtlı konum yoksa saat üstündeki varsayılan yere döner.

### Ayarlar

Menüden **Ayarlar…**:

- **Kullanım / Sıcaklık** — varsayılan görünüm.
- **Açılışta başlat** — otomatik başlatmayı açar/kapatır (Windows: `schtasks` ile açılış görevi; Linux: `~/.config/autostart/heatload.desktop`; macOS: `~/Library/LaunchAgents/com.heatload.HeatLoad.plist`, sonraki oturum açılışında etkili olur).
- **Göstergeyi kapat** — göstergeyi gizler, uygulama tepside kalır.

`Esc`, ✕ düğmesi veya dışarıya tıklayarak kapatılır.

### Tepsi simgesi

Windows 11 yeni tepsi simgelerini gizli simgeler panelinde (`^` oku) tutar. HeatLoad simgesini bir kez görev çubuğuna sürükleyin; Windows orada tutar.

## Nasıl çalışır

| Bölüm | Windows | Linux | macOS |
|:--|:--|:--|:--|
| CPU kullanımı | `GetSystemTimes` farkı | `/proc/stat` farkı | `host_statistics` (Mach) |
| RAM kullanımı | `GlobalMemoryStatusEx` | `/proc/meminfo` | `hw.memsize` + `vm.page_*_count` |
| GPU kullanımı | NVML (`nvml.dll`), yoksa LibreHardwareMonitor | NVML (`libnvidia-ml.so.1`) veya `gpu_busy_percent` | — |
| CPU / GPU / RAM sıcaklığı | LibreHardwareMonitor + NVML | `/sys/class/hwmon`, NVML | — |

Her şey tek bir 1 saniyelik arka plan iş parçacığında döner; okumalar bloklamayan işletim sistemi çağrıları ve birkaç sürücü sorgusundan oluşur. Arayüz tek bir Avalonia penceresidir: çerçevesiz, saydam, her zaman üstte; Windows'ta `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE` sayesinde odağı hiç almaz.

## Sorun giderme

**Sıcaklıklar 0 görünüyor.**
Süreç yönetici değil veya PawnIO kurulu değil. PawnIO'yu kurun, ardından uygulamayı zamanlanmış görevle başlatın (`Start-ScheduledTask -TaskName HeatLoad`) ya da terminali yönetici olarak açın.

**GPU değerleri 0 veya yok.**
NVML yalnızca NVIDIA kartları görür. NVIDIA yoksa widget LibreHardwareMonitor'ün bulduğu kartı okur — AMD veya Intel, dahili GPU dahil — ve açılışta bir kez seçer. Dahili GPU'larda genelde çekirdek sıcaklık sensörü yoktur; kullanım canlı görünür, sıcaklık `—` kalır.

**Tepsi simgesi yok.**
Windows 11 onu gizli simgeler paneline koyar; görev çubuğuna sürükleyin. Orada da yoksa `install.ps1` betiğini tekrar çalıştırın.

**Hiçbir şey görünmüyor.**
`%LOCALAPPDATA%\HeatLoad\error.log` dosyasına bakın. Bu dosya yalnızca çökme durumunda yazılır.

**Kurulumda SmartScreen / antivirüs uyarısı.**
Çalıştırılabilir dosya kod imzalı değil. Kendiniz kaynaktan derleyin veya antivirüsünüzde izin verin.

**Kart yolun üstünde duruyor.**
Sağ tık → *Göstergeyi gizle* veya tepsi simgesine sol tık. Ok ve menü tıklama gerektirdiği için kart tıklamayı geçirmez.

## Proje yapısı

```
HeatLoad.App/
  App.axaml(.cs)               Tek örnek, çökme günlüğü, koyu stiller
  MainWindow.axaml(.cs)        Gösterge, tepsi simgesi, menü, görünüm geçişi, en üstte tutma
  SettingsWindow.axaml(.cs)    Ayarlar kartı
  Reading.cs                   Reading kaydı + işletim sistemine göre sensör fabrikası
  Sensors/WindowsSensors.cs    Win32 + NVML + LibreHardwareMonitor
  Sensors/LinuxSensors.cs      /proc, hwmon, NVML, amdgpu
  Sensors/MacSensors.cs        Mach host_statistics + sysctl
  Autostart.cs                 schtasks / XDG autostart / launch agent
  AppSettings.cs               JSON ayarları (%APPDATA%\HeatLoad\settings.json)
install.ps1                    Program Files'a yayımla + açılış görevi
uninstall.ps1                  Görevi, dosyaları, ayarları sil
make-icon.ps1                  app.ico yeniden üretimi
installer/HeatLoad.iss         HeatLoad-Setup.exe için Inno Setup betiği
.github/workflows/build.yml    Windows / Linux / macOS sürüm derlemeleri
```

## Kurulum paketini üretmek

```powershell
dotnet publish HeatLoad.App -c Release -r win-x64 --self-contained true -o publish-selfcontained `
  -p:SatelliteResourceLanguages=en -p:DebugType=None -p:DebugSymbols=false
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer\HeatLoad.iss
```

Paket `dist\` altında oluşur. Kendinden içerikli yayını (hedef makinede .NET çalışma zamanı gerekmez), açılış görevini ve kaldırıcıyı içerir.

Linux ve macOS derlemeleri GitHub Actions ile üretilir (`.github/workflows/build.yml`, etiket gönderiminde de çalışır): `HeatLoad-linux-x64.tar.gz`, `HeatLoad-macos-arm64.tar.gz`, `HeatLoad-macos-x64.tar.gz`.

## Lisans

[MIT](LICENSE)
