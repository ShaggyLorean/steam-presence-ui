using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;

namespace SteamPresenceUI
{
    public partial class MainWindow : FluentWindow
    {
        private static MainWindow? _current;
        public static MainWindow? Current => _current;

        private uint _taskbarCreatedMsg;
        private DispatcherTimer? _trayHeartbeatTimer;
        private int _heartbeatCount = 0;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        public MainWindow()
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);
            InitializeComponent();
            _current = this;

            // v1.1.3: Safe Phantom State (Off-screen + Minimized, avoids property conflicts)
            if (App.IsMinimizedStartup || GetStartMinimized())
            {
                ApplySafePhantomState();
                StartTrayHeartbeat();
            }
            else
            {
                TrayIcon.Visibility = Visibility.Visible;
            }

            string basePath = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            string tempPath = basePath;
            while (!string.IsNullOrEmpty(tempPath))
            {
                if (File.Exists(Path.Combine(tempPath, "main.py"))) { basePath = tempPath; break; }
                if (File.Exists(Path.Combine(tempPath, "config.json"))) { basePath = tempPath; break; }
                string parent = Path.GetDirectoryName(tempPath);
                if (parent == null || parent == tempPath) break;
                tempPath = parent;
            }
            SteamPresenceUI.Services.PythonRunnerService.Shared = new SteamPresenceUI.Services.PythonRunnerService(basePath);
            
            InitializeStartupLogic(basePath);

            RootNavigation.Loaded += (_, _) => RootNavigation.Navigate(typeof(Views.DashboardPage));
        }

        private void ApplySafePhantomState()
        {
            // v1.1.3: Total Invisibility (Static XAML is the main defense)
            this.Left = -32000;
            this.Top = -32000;
            this.Opacity = 0; 
            
            this.Show(); // Creates HWND
        }

        private void StartTrayHeartbeat()
        {
            _trayHeartbeatTimer = new DispatcherTimer();
            _trayHeartbeatTimer.Interval = TimeSpan.FromSeconds(5);
            _trayHeartbeatTimer.Tick += (s, e) =>
            {
                _heartbeatCount++;
                if (_heartbeatCount > 6)
                {
                    _trayHeartbeatTimer.Stop();
                    return;
                }
                
                Dispatcher.Invoke(() =>
                {
                    if (TrayIcon.Visibility != Visibility.Visible)
                        TrayIcon.Visibility = Visibility.Visible;
                    else
                    {
                        TrayIcon.Visibility = Visibility.Collapsed;
                        TrayIcon.Visibility = Visibility.Visible;
                    }
                });
            };
            _trayHeartbeatTimer.Start();
        }

        private void InitializeStartupLogic(string basePath)
        {
            bool hasSeenPrompt = false;
            string userId = "";
            try 
            {
                string cfgPath = System.IO.Path.Combine(basePath, "config.json");
                if (System.IO.File.Exists(cfgPath)) 
                {
                    var node = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(cfgPath));
                    hasSeenPrompt = node?["HAS_SEEN_LOGIN_PROMPT"]?.GetValue<bool>() ?? false;
                    
                    var userIdsArray = node?["USER_IDS"]?.AsArray();
                    if (userIdsArray != null && userIdsArray.Count > 0)
                        userId = userIdsArray[0]?.GetValue<string>() ?? "";
                    else if (node?["USER_IDS"] != null)
                        userId = node["USER_IDS"]?.GetValue<string>() ?? "";

                    if (node?["AUTO_START_ENGINE"]?.GetValue<bool>() == true && userId != "ENTER_YOURS" && !string.IsNullOrEmpty(userId)) 
                        SteamPresenceUI.Services.PythonRunnerService.Shared.Start();
                }
            } 
            catch { }

            if (!hasSeenPrompt || string.IsNullOrEmpty(userId) || userId == "ENTER_YOURS")
            {
                LoginOverlay.Visibility = Visibility.Visible;
                RootNavigation.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 20, KernelType = System.Windows.Media.Effects.KernelType.Gaussian };
                ShowInTray(true);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _taskbarCreatedMsg = RegisterWindowMessage("TaskbarCreated");
            HwndSource source = (HwndSource)HwndSource.FromVisual(this);
            if (source != null) source.AddHook(HandleMessages);
        }

        private IntPtr HandleMessages(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == _taskbarCreatedMsg)
            {
                Dispatcher.Invoke(() =>
                {
                    TrayIcon.Visibility = Visibility.Collapsed;
                    TrayIcon.Visibility = Visibility.Visible;
                });
            }
            return IntPtr.Zero;
        }

        public void ShowInTray(bool show)
        {
            Dispatcher.Invoke(() => {
                if (show)
                {
                    // v1.1.3: Dynamic Restoration of UI from "Phantom" state
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.Background = System.Windows.Media.Brushes.Transparent;
                    this.WindowBackdropType = WindowBackdropType.Mica;

                    this.Opacity = 1;
                    this.Width = 900;
                    this.Height = 600;
                    this.Left = (SystemParameters.PrimaryScreenWidth - 900) / 2;
                    this.Top = (SystemParameters.PrimaryScreenHeight - 600) / 2;
                    
                    this.Visibility = Visibility.Visible;
                    this.ShowInTaskbar = true;
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                    this.Topmost = true;
                    this.Topmost = false;
                    TrayIcon.Visibility = Visibility.Visible;
                    
                    if (_trayHeartbeatTimer != null) _trayHeartbeatTimer.Stop();
                }
                else
                {
                    this.ShowInTaskbar = false;
                    this.Visibility = Visibility.Hidden;
                    this.Hide(); 
                }
            });
        }

        private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowInTray(true);
        }

        private void MenuShow_Click(object sender, RoutedEventArgs e)
        {
            ShowInTray(true);
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            SteamPresenceUI.Services.PythonRunnerService.Shared?.Stop();
            Environment.Exit(0);
        }

        public void Navigate(Type pageType)
        {
            RootNavigation.Navigate(pageType);
        }

        public void ShowSnackbar(string title, string message, Wpf.Ui.Controls.ControlAppearance appearance = Wpf.Ui.Controls.ControlAppearance.Primary)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var snackbar = new Snackbar(SnackbarPresenter)
                {
                    Title = title,
                    Content = message,
                    Appearance = appearance,
                    Timeout = TimeSpan.FromSeconds(5)
                };
                snackbar.Show();
            });
        }

        private void FluentWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CloseOverlay.Visibility == Visibility.Visible)
            {
                e.Cancel = true;
                return;
            }

            e.Cancel = true;
            CloseOverlay.Visibility = Visibility.Visible;
            RootNavigation.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 20, KernelType = System.Windows.Media.Effects.KernelType.Gaussian };
        }

        private void CloseMinimize_Click(object sender, RoutedEventArgs e)
        {
            CloseOverlay.Visibility = Visibility.Collapsed;
            RootNavigation.Effect = null;
            ShowInTray(false);
        }

        private void CloseQuit_Click(object sender, RoutedEventArgs e)
        {
            SteamPresenceUI.Services.PythonRunnerService.Shared?.Stop();
            Environment.Exit(0);
        }

        private void CloseCancel_Click(object sender, RoutedEventArgs e)
        {
            CloseOverlay.Visibility = Visibility.Collapsed;
            RootNavigation.Effect = null;
        }

        public void ShowCookieError()
        {
            Dispatcher.Invoke(() =>
            {
                CookieErrorOverlay.Visibility = Visibility.Visible;
                RootNavigation.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 20, KernelType = System.Windows.Media.Effects.KernelType.Gaussian };
            });
        }

        private void CookieErrorClose_Click(object sender, RoutedEventArgs e)
        {
            CookieErrorOverlay.Visibility = Visibility.Collapsed;
            RootNavigation.Effect = null;
        }

        private void OverlayOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string basePath = System.IO.Path.GetDirectoryName(Environment.ProcessPath) ?? System.AppContext.BaseDirectory;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"\"{basePath}\"") { UseShellExecute = true });
            }
            catch { }
        }

        private void OverlayContinue_Click(object sender, RoutedEventArgs e)
        {
            string id = OverlaySteamIdInput.Text.Trim();
            if (id.Length != 17 || !long.TryParse(id, out _))
            {
                OverlayErrorText.Visibility = Visibility.Visible;
                return;
            }

            OverlayErrorText.Visibility = Visibility.Collapsed;
            LoginOverlay.Visibility = Visibility.Collapsed;
            RootNavigation.Effect = null;
            try
            {
                string basePath = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
                string tempPath = basePath;
                while (!string.IsNullOrEmpty(tempPath))
                {
                    if (File.Exists(Path.Combine(tempPath, "config.json"))) { basePath = tempPath; break; }
                    if (File.Exists(Path.Combine(tempPath, "main.py"))) { basePath = tempPath; break; }
                    string parent = Path.GetDirectoryName(tempPath);
                    if (parent == null || parent == tempPath) break;
                    tempPath = parent;
                }
                string cfgPath = Path.Combine(basePath, "config.json");
                System.Text.Json.Nodes.JsonNode node = new System.Text.Json.Nodes.JsonObject();
                if (System.IO.File.Exists(cfgPath))
                {
                    var parsed = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(cfgPath));
                    if (parsed != null) node = parsed;
                }
                node["HAS_SEEN_LOGIN_PROMPT"] = true;
                node["USER_IDS"] = new System.Text.Json.Nodes.JsonArray { id };
                File.WriteAllText(cfgPath, node.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                
                ShowSnackbar("Setup Complete", "Steam ID has been saved to " + cfgPath);
                
                if (node["AUTO_START_ENGINE"]?.GetValue<bool>() == true)
                    SteamPresenceUI.Services.PythonRunnerService.Shared.Start();
            }
            catch { }
        }

        private bool GetStartMinimized()
        {
            try
            {
                string basePath = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
                string tempPath = basePath;
                while (!string.IsNullOrEmpty(tempPath))
                {
                    if (File.Exists(Path.Combine(tempPath, "config.json"))) { basePath = tempPath; break; }
                    if (File.Exists(Path.Combine(tempPath, "main.py"))) { basePath = tempPath; break; }
                    string parent = Path.GetDirectoryName(tempPath);
                    if (parent == null || parent == tempPath) break;
                    tempPath = parent;
                }
                string cfgPath = Path.Combine(basePath, "config.json");
                if (File.Exists(cfgPath))
                {
                    var node = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(cfgPath));
                    return node?["START_MINIMIZED"]?.GetValue<bool>() ?? false;
                }
            }
            catch { }
            return false;
        }
    }
}