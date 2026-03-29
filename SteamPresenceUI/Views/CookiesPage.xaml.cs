using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using SteamPresenceUI.Services;
using Wpf.Ui.Controls;

namespace SteamPresenceUI.Views
{
    public partial class CookiesPage : Page
    {
        private readonly CookieValidationService _cookieService;
        private readonly string _basePath;

        public CookiesPage()
        {
            InitializeComponent();
            string searchPath = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(searchPath))
            {
                if (File.Exists(Path.Combine(searchPath, "main.py"))) break;
                if (File.Exists(Path.Combine(searchPath, "config.json"))) break;
                string parent = Path.GetDirectoryName(searchPath) ?? "";
                if (parent == searchPath || string.IsNullOrEmpty(parent)) { searchPath = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory; break; }
                searchPath = parent;
            }
            _basePath = searchPath;
            _cookieService = new CookieValidationService(_basePath);
            
            CheckCookieHealth();
        }

        private void CheckCookieHealth()
        {
            var state = _cookieService.ValidateCookies();
            string age = _cookieService.GetCookieAge();

            if (state == CookieValidationService.ValidationState.Missing)
            {
                CookieStatusAlert.Visibility = Visibility.Visible;
                CookieStatusAlert.Severity = InfoBarSeverity.Error;
                CookieStatusAlert.Title = "Cookies Missing";
                CookieStatusAlert.Message = "No cookies.txt found. Use 'Login to Steam' to set up automatically.";
            }
            else if (state == CookieValidationService.ValidationState.Old)
            {
                CookieStatusAlert.Visibility = Visibility.Visible;
                CookieStatusAlert.Severity = InfoBarSeverity.Warning;
                CookieStatusAlert.Title = "Cookies Old";
                CookieStatusAlert.Message = $"Your cookies were last updated {age}. Click 'Login to Steam' to refresh.";
            }
            else
            {
                CookieStatusAlert.Visibility = Visibility.Visible;
                CookieStatusAlert.Severity = InfoBarSeverity.Success;
                CookieStatusAlert.Title = "Cookies Active";
                CookieStatusAlert.Message = $"Cookies are loaded and healthy (Last updated: {age}). Keep-Alive is maintaining them.";
            }
            
            LastUpdateText.Text = $"Last updated: {age}";

            // Show maintenance notice ONLY if older than 3 days
            MaintenanceNotice.IsOpen = _cookieService.GetCookieAgeDays() > 3;
        }

        private void LoginToSteam_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var loginWindow = new SteamLoginWindow(_basePath);
                loginWindow.Owner = Window.GetWindow(this);
                var result = loginWindow.ShowDialog();

                if (loginWindow.CookiesExtracted)
                {
                    // Refresh UI status
                    CheckCookieHealth();
                    MainWindow.Current?.ShowSnackbar("Cookies Saved", 
                        "Steam cookies have been extracted and saved successfully! Keep-Alive will maintain them.",
                        Wpf.Ui.Controls.ControlAppearance.Success);
                }
            }
            catch (Exception ex)
            {
                MainWindow.Current?.ShowSnackbar("Login Error", 
                    $"Could not open Steam login: {ex.Message}",
                    Wpf.Ui.Controls.ControlAppearance.Danger);
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_basePath}\"") { UseShellExecute = true });
            }
            catch { }
        }
    }
}
