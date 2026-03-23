using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json.Nodes;
using SteamPresenceUI.Services;

namespace SteamPresenceUI.Views
{
    public partial class SettingsPage : Page
    {
        private readonly StartupService _startupService;
        private readonly string _basePath;

        public SettingsPage()
        {
            InitializeComponent();
            _startupService = new StartupService();
            
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
            
            // Sync toggle UI
            StartupToggle.IsChecked = _startupService.IsRegistered();
            StartMinimizedToggle.IsChecked = GetStartMinimized();
            StartMinimizedRow.Visibility = StartupToggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            
            AutoStartEngineToggle.IsChecked = GetAutoStart();
            ExcludedGamesInput.Text = GetExcludedGames();
        }

        private bool GetStartMinimized()
        {
            try
            {
                string cfgPath = Path.Combine(_basePath, "config.json");
                if (!File.Exists(cfgPath)) return false;
                var node = JsonNode.Parse(File.ReadAllText(cfgPath));
                return node?["START_MINIMIZED"]?.GetValue<bool>() ?? false;
            }
            catch { return false; }
        }

        private void SetStartMinimized(bool value)
        {
            try
            {
                string cfgPath = Path.Combine(_basePath, "config.json");
                if (!File.Exists(cfgPath)) return;
                var node = JsonNode.Parse(File.ReadAllText(cfgPath));
                if (node != null)
                {
                    node["START_MINIMIZED"] = value;
                    File.WriteAllText(cfgPath, node.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch { }
        }

        private string GetExcludedGames()
        {
            try
            {
                string cfgPath = Path.Combine(_basePath, "config.json");
                if (!File.Exists(cfgPath)) return "";
                var node = JsonNode.Parse(File.ReadAllText(cfgPath));
                var games = node?["EXCLUDED_GAMES"]?.AsArray();
                if (games == null) return "";
                return string.Join(", ", System.Text.Json.JsonSerializer.Deserialize<string[]>(games.ToJsonString()) ?? Array.Empty<string>());
            }
            catch { return ""; }
        }

        private void SaveExcludedGames(string raw)
        {
            try
            {
                string cfgPath = Path.Combine(_basePath, "config.json");
                if (!File.Exists(cfgPath)) return;
                var node = JsonNode.Parse(File.ReadAllText(cfgPath));
                if (node != null)
                {
                    var list = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var array = new JsonArray();
                    foreach (var item in list) array.Add(item.Trim());
                    node["EXCLUDED_GAMES"] = array;
                    File.WriteAllText(cfgPath, node.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch { }
        }

        private bool GetAutoStart()
        {
            try
            {
                string cfgPath = Path.Combine(_basePath, "config.json");
                if (!File.Exists(cfgPath)) return false;
                var node = JsonNode.Parse(File.ReadAllText(cfgPath));
                return node?["AUTO_START_ENGINE"]?.GetValue<bool>() ?? false;
            }
            catch { return false; }
        }

        private void SetAutoStart(bool value)
        {
            try
            {
                string cfgPath = Path.Combine(_basePath, "config.json");
                if (!File.Exists(cfgPath)) return;
                var node = JsonNode.Parse(File.ReadAllText(cfgPath));
                if (node != null)
                {
                    node["AUTO_START_ENGINE"] = value;
                    File.WriteAllText(cfgPath, node.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch { }
        }

        private void AutoStartEngineToggle_Checked(object sender, RoutedEventArgs e)
        {
            SetAutoStart(true);
        }

        private void AutoStartEngineToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            SetAutoStart(false);
        }

        private void StartupToggle_Checked(object sender, RoutedEventArgs e)
        {
            _startupService.Register(StartMinimizedToggle.IsChecked == true);
            StartMinimizedRow.Visibility = Visibility.Visible;
        }

        private void StartupToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _startupService.Unregister();
            StartMinimizedRow.Visibility = Visibility.Collapsed;
        }

        private void StartMinimizedToggle_Changed(object sender, RoutedEventArgs e)
        {
            bool isChecked = StartMinimizedToggle.IsChecked == true;
            SetStartMinimized(isChecked);
            if (StartupToggle.IsChecked == true)
            {
                _startupService.Register(isChecked);
            }
        }

        private void ExcludedGamesInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveExcludedGames(ExcludedGamesInput.Text);
        }

        private void OpenConfig_Click(object sender, RoutedEventArgs e)
        {
            string configPath = Path.Combine(_basePath, "config.json");
            if (File.Exists(configPath))
            {
                Process.Start(new ProcessStartInfo("notepad.exe", $"\"{configPath}\"") { UseShellExecute = true });
            }
            else
            {
                MainWindow.Current?.ShowSnackbar("Error", "config.json not found at " + configPath, Wpf.Ui.Controls.ControlAppearance.Danger);
            }
        }
    }
}
