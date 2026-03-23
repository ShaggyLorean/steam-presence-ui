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
                CookieStatusAlert.Message = "The 'cookies.txt' file was not found. Please follow the instructions below to add it.";
            }
            else if (state == CookieValidationService.ValidationState.Old)
            {
                CookieStatusAlert.Visibility = Visibility.Visible;
                CookieStatusAlert.Severity = InfoBarSeverity.Warning;
                CookieStatusAlert.Title = "Cookies Old";
                CookieStatusAlert.Message = $"Your cookies were last updated {age}. If presence data is missing, consider refreshing them.";
            }
            else
            {
                CookieStatusAlert.Visibility = Visibility.Visible;
                CookieStatusAlert.Severity = InfoBarSeverity.Success;
                CookieStatusAlert.Title = "Cookies Active";
                CookieStatusAlert.Message = $"Cookies are loaded and healthy (Last updated: {age}).";
            }
            
            LastUpdateText.Text = $"Last updated: {age}";
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
