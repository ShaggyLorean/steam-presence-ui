# Steam Presence Companion - Project Summary / Proje Özeti

## English Version

### Project Overview
Steam Presence Companion is a modern Windows application designed to provide rich Discord Presence for Steam games, including those that don't natively support it. It consists of a WPF-based UI, a Python-based scraping engine, and a self-contained C# installer.

### Architecture & Structure
- **SteamPresenceUI (WPF, .NET 9)**
  - The frontend manager. It handles user configuration (`config.json`), log visualization, and the lifecycle of the Python engine.
  - **Key Service**: `PythonRunnerService` - Manages the background process, captures stdout/stderr, and detects successful state updates to hide UI warnings.
- **Python Engine (`main.py`)**
  - The core "brain" that scrapes `steamcommunity.com` using `cookies.txt` and updates Discord RPC.
- **SteamPresenceInstaller (C#)**
  - A definitive, self-contained setup tool. It embeds `payload.zip` (containing the UI, Engine, and configs) as a Manifest Resource.
  - **One-Click Assistant**: Automatically runs `pip install -r requirements.txt` during installation.

### Key Technical Implementations
- **Icon Rendering**: Uses a custom-generated 32-bit ARGB multi-size ICO (16x16 to 256x256) via `gen_ico.ps1` to ensure perfect transparency on Taskbar and Desktop.
- **Icon Cache Bypass**: The installer extracts a dedicated `appicon.ico` and points the desktop shortcut to it explicitly, forcing Windows to show the new icon immediately.
- **Process Cleanup**: The installer and UI both use robust logic (including `taskkill /F /T`) to ensure no "ghost" engine instances remain.
- **Automated Sync**: The `build_setup.ps1` script handles the entire chain: Build UI -> Package Payload -> Compile Installer.
- **Global Tray Icon (v1.1)**: The `TaskbarIcon` has been restored to `MainWindow.xaml` for maximum compatibility with the visual tree. A refined `ShowInTray` method and optimized window-hiding logic during startup now ensure the icon always registers with the Windows shell, even in the "Start with Windows" (stealth) mode.
- **Installer Visibility (v1.1)**: The `pip install` process remains visible in a `cmd.exe` window to provide real-time progress feedback.

### Developer Notes
- When updating the engine, ensure the `Payload/` folder is updated before running `build_setup.ps1`.
- The `config.json` contains a hardcoded `STEAM_API_KEY` for out-of-the-box functionality.

---

## Türkçe Versiyon

### Proje Genel Bakış
Steam Presence Companion, Steam oyunları (yerel olarak desteklemeyenler dahil) için zengin Discord RPC desteği sağlayan modern bir Windows uygulamasıdır. Proje; WPF tabanlı bir arayüz, Python tabanlı bir çekirdek motor ve tamamen bağımsız bir C# yükleyiciden oluşur.

### Mimari ve Yapı
- **SteamPresenceUI (WPF, .NET 9)**
  - Yönetici arayüzü. Kullanıcı ayarlarını (`config.json`), log görselleştirmeyi ve Python motorunun yaşam döngüsünü yönetir.
  - **Önemli Servis**: `PythonRunnerService` - Arka plan işlemini yönetir, başarıyla güncellenen durumları yakalar ve arayüzdeki uyarıları otomatik gizler.
- **Python Motoru (`main.py`)**
  - `cookies.txt` kullanarak `steamcommunity.com` verilerini çeken ve Discord RPC'yi güncelleyen ana çekirdek.
- **SteamPresenceInstaller (C#)**
  - Tek dosyadan oluşan (self-contained) yükleyici. `payload.zip` dosyasını kaynak olarak gömülü taşır.
  - **Tek Tık Kurulum**: Yükleme sırasında sessizce `pip install -r requirements.txt` çalıştırarak ortamı hazırlar.

### Önemli Teknik Uygulamalar
- **İkon İşleme**: Görev çubuğu ve Masaüstünde kusursuz şeffaflık için `gen_ico.ps1` ile üretilmiş 32-bit ARGB çok boyutlu (16x16 - 256x256) ICO dosyası kullanır.
- **İkon Önbellek Çözümü**: Installer, kısayol için özel bir `appicon.ico` dosyası çıkartır ve kısayolu direkt bu dosyaya yönlendirerek Windows'un eski ikonu göstermesini engeller.
- **Süreç Temizliği**: Installer ve UI, "ghost" (hayalet) işlemlerin kalmaması için `taskkill /F /T` gibi sağlam yöntemlerle temizlik yapar.
- **Otomatik Build**: `build_setup.ps1` betiği tüm zinciri yönetir: UI Build -> Payload Paketleme -> Installer Derleme.
- **Global Tray İkon Yapısı (v1.1)**: `TaskbarIcon`, görsel ağaçla en iyi uyum için tekrar `MainWindow.xaml` içine taşındı. Geliştirilen `ShowInTray` metodu ve optimize edilen başlangıç mantığı sayesinde, uygulama Windows ile arka planda başlasa bile ikonun her zaman Windows kabuğuna (Shell) başarıyla kaydedilmesi sağlandı.
- **Installer Görünürlüğü (v1.1)**: `pip install` işlemi, kullanıcının ilerlemeyi görebilmesi için görünür bir `cmd.exe` penceresinde çalışmaya devam eder.

### Geliştirici Notları
- Motor (Python tarafı) güncellendiğinde, `build_setup.ps1` çalıştırılmadan önce `Payload/` klasörünün güncelliğinden emin olunmalıdır.
- `config.json` dosyası, uygulamanın kurulum sonrası direkt çalışabilmesi için sabit bir `STEAM_API_KEY` içerir.
