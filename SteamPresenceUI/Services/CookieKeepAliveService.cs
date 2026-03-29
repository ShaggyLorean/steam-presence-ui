using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SteamPresenceUI.Services
{
    /// <summary>
    /// Background service that periodically pings steamcommunity.com using
    /// the existing cookies.txt to keep the session alive and prevent expiration.
    /// Refreshed cookies from the response are written back to cookies.txt.
    /// </summary>
    public sealed class CookieKeepAliveService : IDisposable
    {
        private Timer? _timer;
        private readonly string _cookiePath;
        private readonly TimeSpan _interval = TimeSpan.FromHours(10);
        private bool _disposed;

        // Endpoints that return Set-Cookie headers and refresh session tokens
        private static readonly string[] RefreshEndpoints =
        {
            "https://steamcommunity.com/",
            "https://steamcommunity.com/chat/clientjstoken"
        };

        private static readonly string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

        public event EventHandler<string>? LogMessage;

        public CookieKeepAliveService(string basePath)
        {
            _cookiePath = Path.Combine(basePath, "cookies.txt");
        }

        /// <summary>Start the periodic keep-alive timer.</summary>
        public void Start()
        {
            _timer?.Dispose();
            _timer = new Timer(async _ => await RefreshAsync(), null, TimeSpan.FromMinutes(5), _interval);
            Log("Cookie Keep-Alive armed (interval: 10h).");
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        /// <summary>
        /// Manually trigger a refresh right now (e.g., after WebView2 login).
        /// </summary>
        public async Task RefreshNowAsync()
        {
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            if (!File.Exists(_cookiePath))
            {
                Log("Keep-Alive: No cookies.txt found, skipping.");
                return;
            }

            try
            {
                var cookieContainer = new CookieContainer();
                LoadNetscapeCookies(cookieContainer);

                var handler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    UseCookies = true,
                    AllowAutoRedirect = true
                };

                using var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                client.Timeout = TimeSpan.FromSeconds(20);

                bool anyRefreshed = false;

                foreach (var endpoint in RefreshEndpoints)
                {
                    try
                    {
                        var response = await client.GetAsync(endpoint);
                        if (response.IsSuccessStatusCode)
                        {
                            anyRefreshed = true;
                        }
                    }
                    catch
                    {
                        // Individual endpoint failure is non-fatal
                    }
                }

                if (anyRefreshed)
                {
                    // Write refreshed cookies back to Netscape format
                    SaveNetscapeCookies(cookieContainer);
                    // Touch the file so CookieValidationService sees a fresh timestamp
                    File.SetLastWriteTime(_cookiePath, DateTime.Now);
                    Log("Keep-Alive: Session refreshed successfully.");
                }
                else
                {
                    Log("Keep-Alive: No endpoints responded. Cookies may have expired — re-login required.");
                }
            }
            catch (Exception ex)
            {
                Log($"Keep-Alive: Error — {ex.Message}");
            }
        }

        /// <summary>
        /// Parse a Netscape-format cookies.txt into a CookieContainer.
        /// </summary>
        private void LoadNetscapeCookies(CookieContainer container)
        {
            foreach (var line in File.ReadAllLines(_cookiePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('\t');
                if (parts.Length < 7) continue;

                try
                {
                    string domain = parts[0].TrimStart('.');
                    string path = parts[2];
                    bool secure = parts[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                    string name = parts[5];
                    string value = parts[6];

                    var cookie = new Cookie(name, value, path, domain)
                    {
                        Secure = secure,
                        HttpOnly = true
                    };
                    container.Add(cookie);
                }
                catch
                {
                    // Skip malformed lines
                }
            }
        }

        /// <summary>
        /// Write a CookieContainer back to Netscape cookies.txt format,
        /// preserving only steamcommunity.com cookies.
        /// </summary>
        private void SaveNetscapeCookies(CookieContainer container)
        {
            // Python's MozillaCookieJar fails if there is a UTF-8 BOM, so we must write without BOM.
            using var writer = new StreamWriter(_cookiePath, false, new System.Text.UTF8Encoding(false));
            writer.WriteLine("# Netscape HTTP Cookie File");
            writer.WriteLine("# Auto-refreshed by Steam Presence Companion");
            writer.WriteLine();

            var steamCookies = container.GetCookies(new Uri("https://steamcommunity.com"));
            foreach (Cookie cookie in steamCookies)
            {
                string domain = cookie.Domain.StartsWith(".") ? cookie.Domain : "." + cookie.Domain;
                string flag = domain.StartsWith(".") ? "TRUE" : "FALSE";
                string secure = cookie.Secure ? "TRUE" : "FALSE";
                long expires = cookie.Expires == DateTime.MinValue
                    ? 0
                    : new DateTimeOffset(cookie.Expires).ToUnixTimeSeconds();

                writer.WriteLine($"{domain}\t{flag}\t{cookie.Path}\t{secure}\t{expires}\t{cookie.Name}\t{cookie.Value}");
            }

            // Also grab store.steampowered.com cookies if present
            var storeCookies = container.GetCookies(new Uri("https://store.steampowered.com"));
            foreach (Cookie cookie in storeCookies)
            {
                string domain = cookie.Domain.StartsWith(".") ? cookie.Domain : "." + cookie.Domain;
                string flag = domain.StartsWith(".") ? "TRUE" : "FALSE";
                string secure = cookie.Secure ? "TRUE" : "FALSE";
                long expires = cookie.Expires == DateTime.MinValue
                    ? 0
                    : new DateTimeOffset(cookie.Expires).ToUnixTimeSeconds();

                writer.WriteLine($"{domain}\t{flag}\t{cookie.Path}\t{secure}\t{expires}\t{cookie.Name}\t{cookie.Value}");
            }
        }

        private void Log(string message) => LogMessage?.Invoke(this, message);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer?.Dispose();
        }
    }
}
