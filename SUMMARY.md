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
- **Total Stealth Startup (v1.1.3)**: Resolved the "dark box" issue by statically configuring the `MainWindow` as a 0x0 transparent phantom in XAML. Full UI and Mica effects are only applied upon manual activation.
- **Conditional Maintenance (v1.1.2)**: A smart "Refresh Cookies" reminder appears in the UI **only** if `cookies.txt` is older than 3 days.
- **Stacked Notifications**: Uses a `StackPanel` architecture for the Cookies page to prevent UI element overlapping.

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
### Temel Geliştirmeler
- **Uygulama İçi Steam Girişi (v1.2.0)**: Microsoft Edge WebView2 entegre edildi. Artık tarayıcı eklentilerine ihtiyaç yok, kullanıcı direkt uygulama içinde Steam'e giriş yapabilir ve cookie'ler otomatik çekilir.
- **Sonsuz Cookie Ömrü (v1.2.0)**: Arka planda çalışan `CookieKeepAliveService`, Steam oturumunu taze tutmak için periyodik istekler atarak cookie'lerin süresinin dolmasını sonsuza kadar engeller.
- **Alt+Tab Görünmezliği (v1.2.0)**: Win32 native API (`WS_EX_TOOLWINDOW`) kullanılarak, küçültülmüş uygulamanın Windows Alt+Tab menüsünde bıraktığı kalıntı (ghost window) tamamen silindi.
- **Basitleştirilmiş Kurulum (v1.2.0)**: Uygulamanın ilk açılış ekranı (Setup) sadeleştirildi ve kafa karıştırıcı "Cookies" zorunluluğu başlangıç ekranından kaldırıldı.
- **Sağlam Kurulum Aracı (v1.2.0)**: Kurulum sırasında Python modüllerinin yüklenmesi için `python -m pip install` komutuna geçildi, böylece Path sorunlarından kaynaklı modül kilitlenmelerinin önüne geçildi.
- **Sessiz Başlangıç (v1.1.3)**: Autostart sırasında görünen "siyah kutu" sorunu, `MainWindow`'un XAML seviyesinde 0x0 boyutunda ve şeffaf olarak yapılandırılmasıyla tamamen çözüldü.

### Geliştirici Notları
- Motor (Python tarafı) güncellendiğinde, `build_setup.ps1` çalıştırılmadan önce `Payload/` klasörünün güncelliğinden emin olunmalıdır.
- `config.json` dosyası, uygulamanın kurulum sonrası direkt çalışabilmesi için sabit bir `STEAM_API_KEY` içerir.
