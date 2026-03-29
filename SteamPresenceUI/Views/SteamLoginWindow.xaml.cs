using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace SteamPresenceUI.Views
{
    /// <summary>
    /// Embedded WebView2 window for Steam login.
    /// After user logs in, cookies are extracted and saved to cookies.txt
    /// in Netscape format — no browser extensions required.
    /// </summary>
    public partial class SteamLoginWindow : Window
    {
        private readonly string _cookiePath;

        /// <summary>True if cookies were successfully extracted and saved.</summary>
        public bool CookiesExtracted { get; private set; }

        public SteamLoginWindow(string basePath)
        {
            InitializeComponent();
            _cookiePath = Path.Combine(basePath, "cookies.txt");
            Loaded += async (_, _) => await InitBrowserAsync();
        }

        private async System.Threading.Tasks.Task InitBrowserAsync()
        {
            try
            {
                // Use a persistent user-data folder so Steam "Remember Me" works
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SteamPresence", "WebView2Profile");

                Directory.CreateDirectory(userDataFolder);

                var env = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: userDataFolder);

                await SteamBrowser.EnsureCoreWebView2Async(env);

                SteamBrowser.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    string url = SteamBrowser.Source?.AbsoluteUri ?? "";
                    UrlDisplay.Text = url.Length > 80 ? url[..80] + "…" : url;

                    // Auto-detect successful login: URL moves away from /login
                    if (url.Contains("steamcommunity.com") && !url.Contains("/login"))
                    {
                        StatusText.Text = "✅ Logged in! Click 'Extract Cookies & Close' to save.";
                        StatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
                    }
                };

                // Navigate to Steam login page
                SteamBrowser.CoreWebView2.Navigate("https://steamcommunity.com/login/home/");
            }
            catch (Exception ex)
            {
                StatusText.Text = $"WebView2 init failed: {ex.Message}";
                MessageBox.Show(
                    $"WebView2 could not be initialized.\n\n{ex.Message}\n\n" +
                    "Make sure Microsoft Edge WebView2 Runtime is installed.\n" +
                    "Download: https://developer.microsoft.com/en-us/microsoft-edge/webview2/",
                    "WebView2 Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExtractButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SteamBrowser.CoreWebView2 == null)
                {
                    MessageBox.Show("Browser not initialized yet.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var cookieManager = SteamBrowser.CoreWebView2.CookieManager;

                // Get ALL cookies from steamcommunity.com and related domains
                var steamCommunityCookies = await cookieManager.GetCookiesAsync("https://steamcommunity.com");
                var storeCookies = await cookieManager.GetCookiesAsync("https://store.steampowered.com");
                var loginCookies = await cookieManager.GetCookiesAsync("https://login.steampowered.com");

                var allCookies = new List<CoreWebView2Cookie>();
                allCookies.AddRange(steamCommunityCookies);
                allCookies.AddRange(storeCookies);
                allCookies.AddRange(loginCookies);

                if (allCookies.Count == 0)
                {
                    MessageBox.Show(
                        "No Steam cookies found. Please log in first.",
                        "No Cookies", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Deduplicate by domain+path+name
                var seen = new HashSet<string>();
                var uniqueCookies = new List<CoreWebView2Cookie>();
                foreach (var c in allCookies)
                {
                    string key = $"{c.Domain}|{c.Path}|{c.Name}";
                    if (seen.Add(key))
                        uniqueCookies.Add(c);
                }

                // Write Netscape format
                var sb = new StringBuilder();
                sb.AppendLine("# Netscape HTTP Cookie File");
                sb.AppendLine("# Extracted by Steam Presence Companion");
                sb.AppendLine();

                foreach (var cookie in uniqueCookies)
                {
                    string domain = cookie.Domain;
                    string flag = domain.StartsWith(".") ? "TRUE" : "FALSE";
                    string path = cookie.Path;
                    string secure = cookie.IsSecure ? "TRUE" : "FALSE";

                    // CoreWebView2Cookie.Expires is a DateTime
                    long expires = 0;
                    if (cookie.Expires != DateTime.MinValue && cookie.Expires > DateTime.UnixEpoch)
                        expires = new DateTimeOffset(cookie.Expires).ToUnixTimeSeconds();

                    sb.AppendLine($"{domain}\t{flag}\t{path}\t{secure}\t{expires}\t{cookie.Name}\t{cookie.Value}");
                }

                File.WriteAllText(_cookiePath, sb.ToString(), Encoding.UTF8);
                CookiesExtracted = true;

                StatusText.Text = $"✅ {uniqueCookies.Count} cookies saved to cookies.txt!";
                StatusText.Foreground = System.Windows.Media.Brushes.LightGreen;

                // Close after a brief delay
                await System.Threading.Tasks.Task.Delay(800);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cookie extraction failed:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
