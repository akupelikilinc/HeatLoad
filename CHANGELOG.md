# Sürüm notları / Changelog

Her sürümün değişiklikleri, ekran görüntüleriyle birlikte. Görseller o sürümün tag'inden gelir — yani o sürümde program gerçekten nasıl görünüyorsa öyle. / Changes per version, each with its screenshots, pinned to that version's tag.

| Sürüm | Tarih | Özet |
|:--|:--|:--|
| **1.3.3** | 29.10.2026 | Kurulum sonrası "kod 740" hatası giderildi |
| **1.3.2** | 29.10.2026 | Harici kart yoksa dahili GPU gösteriliyor |
| **1.3.1** | 29.10.2026 | Gösterge odak kaybedince hemen en üste dönüyor |
| **1.3.0** | 28.10.2026 | Linux ve macOS sürümleri (Avalonia) |
| **1.2.1** | 28.10.2026 | Gösterge gerçekten en üstte kalıyor |
| **1.2.0** | 25.10.2026 | Kompakt görünüm (⤡ / ⤢) |
| **1.1.0** | 25.10.2026 | Kart taşınabilir, konum hatırlanır |
| **1.0.0** | 25.10.2026 | İlk sürüm: kullanım + sıcaklık, tepsi, kurulum |

---

## 1.3.3 — 29.10.2026 — Kurulum hatası / Installer fix

- Kurulumun sonundaki "HeatLoad'u başlat" adımı artık yönetici jetonuyla çalışıyor. Önceden yükseltilmemiş kullanıcıyla başlatılmaya çalışıldığı için Windows `CreateProcess ... kod 740` hatası veriyordu; kurulumun kendisi ve açılış görevi zaten sorunsuzdu.
- **EN:** The post-install "launch HeatLoad" step now inherits Setup's elevated token. It used to be started as the original (non-elevated) user, so Windows refused it with `CreateProcess ... code 740`.

---

## 1.3.2 — 29.10.2026 — Dahili ekran kartı / Integrated GPU

- Windows'ta NVIDIA kartı yoksa GPU alanları artık boş kalmıyor: LibreHardwareMonitor'ün bulduğu kart okunuyor — AMD veya Intel, dahili GPU dahil.
- Yük ve sıcaklık aynı karttan geliyor; kart açılışta bir kez seçiliyor, böylece değerler sürüm boyunca tutarlı kalıyor.
- Harici NVIDIA kart varken davranış değişmedi: veriler eskisi gibi NVML'den.
- **EN:** Without an NVIDIA card the widget reads GPU usage and temperature from the card LibreHardwareMonitor found, integrated GPUs included. One card is picked at startup, so both numbers always come from the same device.

[Release v1.3.2](https://github.com/akupelikilinc/HeatLoad/releases/tag/v1.3.2)

---

## 1.3.1 — 29.10.2026 — Her zaman üstte / Always on top

- Gösterge ilk karede ve tepsi simgesinden geri açıldığında en üst katmana açıkça taşınıyor; önceden bu, ilk 2 saniyelik yoklamayı bekliyordu.
- **1.2.0 kuruluysa asıl düzeltme bu sürüm:** 1.2.0 göstergesi hiçbir zaman en üst katmana girmiyordu. Kurulum, çalışan eski sürümü kapatıp yerine bu derlemeyi koyar.
- **EN:** The card is placed in the topmost band on the first frame and whenever it is restored from the tray, instead of waiting for the two-second tick. If you are still on 1.2.0, this is the build that actually fixes the widget staying behind other windows.

[Release v1.3.1](https://github.com/akupelikilinc/HeatLoad/releases/tag/v1.3.1)

---

## 1.3.0 — 28.10.2026 — Linux ve macOS / Linux and macOS

- Arayüz Avalonia'ya taşındı: tek kod tabanından Windows, Linux ve macOS derlemeleri.
- **Linux:** CPU `/proc/stat`, RAM `/proc/meminfo`, sıcaklıklar `/sys/class/hwmon`, NVIDIA GPU NVML, AMD GPU yükü `amdgpu` sysfs.
- **macOS:** CPU `host_statistics`, RAM sysctl. Sıcaklık ve GPU yükü henüz yok (SMC/IOKit ek yetki gerektiriyor) — alanlar `—` kalır.
- Ayarlar kayıt defterinden JSON'a taşındı: `%APPDATA%\HeatLoad\settings.json`, `~/.config/HeatLoad/settings.json`, `~/Library/Application Support/HeatLoad/settings.json`. Eski HKCU değerleri okunmaz.
- Otomatik başlatma: Windows'ta `schtasks` görevi, Linux'ta XDG autostart, macOS'ta launch agent.
- GitHub Actions ile dört derleme: `HeatLoad-linux-x64.tar.gz`, `HeatLoad-macos-arm64.tar.gz`, `HeatLoad-macos-x64.tar.gz`, Windows Setup.
- **EN:** The UI moved to Avalonia, so one codebase now ships Windows, Linux and macOS builds; settings live in a per-OS JSON file and macOS has no temperatures or GPU load yet.

[Release v1.3.0](https://github.com/akupelikilinc/HeatLoad/releases/tag/v1.3.0)

---

## 1.2.1 — 28.10.2026 — Her zaman üstte / Always on top

- Gösterge, `Topmost` bayrağına rağmen açılan pencerelerin altında kalabiliyordu; pencere artık açıkça en üst katmana taşınıyor.
- Üstte olduğu 2 saniyede bir yeniden uygulanıyor (Explorer yeniden başlaması, tam ekran uygulamalar); sağ tık menüsü ve Ayarlar açıkken susuyor.
- **EN:** The widget is explicitly moved into the topmost band and re-asserted every two seconds, so other windows no longer cover it.

[Release v1.2.1](https://github.com/akupelikilinc/HeatLoad/releases/tag/v1.2.1)

---

## 1.2.0 — 25.10.2026 — Kompakt görünüm / Compact view

- **Ölçekleme ikonu (⤡)** — ⇄ okunun yanında. Tıklayınca 382×124 kart, yalnızca renk kodlu üç sayıdan oluşan **236×62** ince şeride iner; ⤢ tam kartı geri getirir.
- Kompakt tercih `HKCU\Software\HeatLoad\Compact` içinde saklanır; yeniden başlatmada ve oturum açılışında korunur.
- Sağ tık menüsüne **Kompakt görünüm** onay kutusu eklendi (işaretli durumu kartla eşzamanlı).
- Kompakt modda da sürükleme, ⇄ ile kullanım ↔ sıcaklık geçişi ve sağ tık menüsü çalışır.
- Kompakt değerler, tam karttaki göstergelere bağlıdır (binding) — tek sensör döngüsü, kopya okuma yok.
- README görselleri (kullanım, sıcaklık, menü) yeni başlık ve menüyle yenilendi.

**EN:** A scale icon next to the ⇄ arrow collapses the card to a 236×62 row with the three colour-coded numbers; ⤢ restores it. The choice is remembered and available in the right-click menu.

Kompakt görünüm / compact view:

<img src="https://raw.githubusercontent.com/akupelikilinc/HeatLoad/v1.2.0/docs/compact.png" alt="Kompakt görünüm" width="236">

Menüdeki yeni onay kutusu / the new menu entry (tam kart / full card):

<img src="https://raw.githubusercontent.com/akupelikilinc/HeatLoad/v1.2.0/docs/menu.png" alt="Kompakt görünüm menüde" width="403">

[Release v1.2.0](https://github.com/akupelikilinc/HeatLoad/releases/tag/v1.2.0)

---

## 1.1.0 — 25.10.2026 — Taşınabilir gösterge / Movable widget

- **Sürükle-bırak** — kartı sol tuşla tutup istediğin yere, istediğin monitöre taşı.
- Konum `HKCU\Software\HeatLoad` içinde `X` / `Y` olarak saklanır, yeniden başlatmada korunur.
- Kayıtlı konum ekranda değilse (monitör kaldırıldı, çözünürlük değişti) kart erişilebilir bir yere döner.
- Sürükleme eşiği 4 px: ⇄ oku ve sağ tık menüsü tıklama olarak çalışmaya devam eder.
- Kart üzerinde taşıma imleci gösterilir.

**EN:** Grab the card and move it anywhere; the position is saved and restored on restart, with a fallback when the monitor layout changes.

Kart, taşındıktan sonra / the card after being moved:

<img src="https://raw.githubusercontent.com/akupelikilinc/HeatLoad/v1.1.0/docs/usage.png" alt="Sürüklenebilir kart" width="382">

[Release v1.1.0](https://github.com/akupelikilinc/HeatLoad/releases/tag/v1.1.0)

---

## 1.0.0 — 25.10.2026 — İlk sürüm / Initial release

- CPU / GPU / RAM kullanımı, sıcaklıklar (LibreHardwareMonitor + PawnIO, NVIDIA için NVML).
- ⇄ ile kullanım ↔ sıcaklık geçişi; büyük değer seçili görünümü izler, diğeri köşe rozetinde kalır.
- Saatin hemen üstünde, çerçevesiz, her zaman üstte, odağı çalmayan, görev çubuğunda görünmeyen kart.
- Tepsi simgesi, sağ tık menüsü, ayarlar penceresi, göstergeyi gizleme.
- Yönetici olarak oturum açılışında başlayan zamanlanmış görev (UAC sormadan sıcaklıklar çalışır).
- Tek dosya **Setup.exe** — kendinden içerikli, .NET çalışma zamanı gerekmez; sessiz kurulum ve temiz kaldırma.

**EN:** First release: live usage and temperatures above the clock, tray icon, settings, logon task and a self-contained Setup.exe.

Kullanım görünümü / usage view:

<img src="https://raw.githubusercontent.com/akupelikilinc/HeatLoad/v1.0.0/docs/usage.png" alt="Kullanım görünümü" width="382">

Sıcaklık görünümü / temperature view:

<img src="https://raw.githubusercontent.com/akupelikilinc/HeatLoad/v1.0.0/docs/temperature.png" alt="Sıcaklık görünümü" width="382">

Sağ tık menüsü ve ayarlar / menu and settings:

<img src="https://raw.githubusercontent.com/akupelikilinc/HeatLoad/v1.0.0/docs/menu.png" alt="Sağ tık menüsü" width="403">
<img src="https://raw.githubusercontent.com/akupelikilinc/HeatLoad/v1.0.0/docs/settings.png" alt="Ayarlar" width="330">

[Release v1.0.0](https://github.com/akupelikilinc/HeatLoad/releases/tag/v1.0.0)
