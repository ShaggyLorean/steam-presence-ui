using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace SteamPresenceUI.Services
{
    /// <summary>
    /// Background service that instantiates an off-screen WebView2 periodically.
    /// By visiting steamcommunity.com with WebView2, the actual Steam Javascript executes,
    /// triggering their OAuth/JWT token refresh naturally. The fresh cookies are then
    /// dumped to cookies.txt, bypassing the severe session expiration limits.
    /// WebView2 is aggressively disposed after each run to maintain ~0MB idle RAM.
    /// </summary>
    public sealed class CookieKeepAliveService : Window, IDisposable
    {
        private DispatcherTimer? _timer;
        private readonly string _cookiePath;
        private readonly string _userDataFolder;
        private WebView2? _webView;
        private bool _disposed;
        private bool _isNavigating;

        public event EventHandler<string>? LogMessage;

        public CookieKeepAliveService(string basePath)
        {
            _cookiePath = Path.Combine(basePath, "cookies.txt");
            _userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SteamPresence", "WebView2Profile");

            // Setup a phantom window properties
            this.Width = 0;
            this.Height = 0;
            this.WindowStyle = WindowStyle.None;
            this.ShowInTaskbar = false;
            this.ShowActivated = false;
            this.Visibility = Visibility.Collapsed;
            this.Left = -32000;
            this.Top = -32000;
            this.Opacity = 0;

            // HWND must be created
            this.Show();
            this.Hide();
        }

        public void Start()
        {
            _timer?.Stop();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromHours(1);
            _timer.Tick += async (s, e) => await RefreshAsync();
            _timer.Start();
            
            // Fire first refresh rapidly after startup
            Task.Delay(TimeSpan.FromMinutes(2)).ContinueWith(_ => Dispatcher.InvokeAsync(RefreshAsync));

            Log("Smart WebView2 Keep-Alive armed (interval: 1h).");
        }

        public void Stop()
        {
            _timer?.Stop();
        }

        public async Task RefreshNowAsync()
        {
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            if (_isNavigating || !File.Exists(_cookiePath)) return;
            _isNavigating = true;

            try
            {
                Directory.CreateDirectory(_userDataFolder);

                _webView = new WebView2();
                this.Content = _webView;

                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: _userDataFolder);
                await _webView.EnsureCoreWebView2Async(env);

                EventHandler<CoreWebView2NavigationCompletedEventArgs>? completedHandler = null;
                
                completedHandler = async (s, e) =>
                {
                    if (_webView == null) return;
                    _webView.CoreWebView2.NavigationCompleted -= completedHandler;
                    
                    // Wait enough time for Steam's React logic to negotiate new JWT tokens via localstorage
                    await Task.Delay(8000);
                    
                    try 
                    {
                        var steamCookies = await _webView.CoreWebView2.CookieManager.GetCookiesAsync("https://steamcommunity.com");
                        var storeCookies = await _webView.CoreWebView2.CookieManager.GetCookiesAsync("https://store.steampowered.com");

                        var allCookies = new List<CoreWebView2Cookie>();
                        allCookies.AddRange(steamCookies);
                        allCookies.AddRange(storeCookies);

                        // Deduplicate
                        var seen = new HashSet<string>();
                        var uniqueCookies = new List<CoreWebView2Cookie>();
                        foreach (var c in allCookies)
                        {
                            string key = $"{c.Domain}|{c.Path}|{c.Name}";
                            if (seen.Add(key))
                                uniqueCookies.Add(c);
                        }

                        var sb = new StringBuilder();
                        sb.AppendLine("# Netscape HTTP Cookie File");
                        sb.AppendLine("# Auto-refreshed via Smart WebView2 Memory");
                        sb.AppendLine();

                        foreach (var cookie in uniqueCookies)
                        {
                            string domain = cookie.Domain;
                            string flag = domain.StartsWith(".") ? "TRUE" : "FALSE";
                            string path = cookie.Path;
                            string secure = cookie.IsSecure ? "TRUE" : "FALSE";

                            long expires = 0;
                            if (cookie.Expires != DateTime.MinValue && cookie.Expires > DateTime.UnixEpoch)
                                expires = new DateTimeOffset(cookie.Expires).ToUnixTimeSeconds();

                            sb.AppendLine($"{domain}\t{flag}\t{path}\t{secure}\t{expires}\t{cookie.Name}\t{cookie.Value}");
                        }

                        // Write string strictly without BOM marker
                        File.WriteAllText(_cookiePath, sb.ToString(), new UTF8Encoding(false));
                        File.SetLastWriteTime(_cookiePath, DateTime.Now);
                        
                        Log("Keep-Alive: WebView2 hardware token refresh successful.");
                    }
                    catch (Exception cookieEx)
                    {
                        Log($"Keep-Alive: Extract Failed - {cookieEx.Message}");
                    }
                    finally
                    {
                        // Clean up RAM aggressively
                        this.Content = null;
                        _webView.Dispose();
                        _webView = null;
                        _isNavigating = false;
                    }
                };

                _webView.CoreWebView2.NavigationCompleted += completedHandler;
                _webView.CoreWebView2.Navigate("https://steamcommunity.com/");
            }
            catch (Exception ex)
            {
                Log($"Keep-Alive: Critical Error — {ex.Message}");
                if (_webView != null)
                {
                    this.Content = null;
                    _webView.Dispose();
                    _webView = null;
                }
                _isNavigating = false;
            }
        }

        private void Log(string message) => LogMessage?.Invoke(this, message);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer?.Stop();
            if (_webView != null)
            {
                this.Content = null;
                _webView.Dispose();
            }
            this.Close();
        }
    }
}
