using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using SteamPresenceUI.Services;
using Wpf.Ui.Appearance;

namespace SteamPresenceUI.Views
{
    public partial class DashboardPage : Page
    {
        private PythonRunnerService _runner => PythonRunnerService.Shared;

        public DashboardPage()
        {
            InitializeComponent();
            
            _runner.OutputReceived += Runner_OutputReceived;
            _runner.ErrorReceived += Runner_ErrorReceived;
            _runner.StatusChanged += Runner_StatusChanged;
            _runner.StateUpdated += Runner_StateUpdated;
            
            if (_runner.HasSuccessfullyRun)
                MandatoryLoginInfoBar.IsOpen = false;
            else
                _runner.SuccessfullyRan += (s, e) => Dispatcher.Invoke(() => MandatoryLoginInfoBar.IsOpen = false);
            
            // Restore previous UI state explicitly
            LogTextBox.Text = _runner.LogHistory.ToString();
            LogTextBox.ScrollToEnd();
            Runner_StatusChanged(null, _runner.IsRunning);

            CheckCookies();
        }

        private void CheckCookies()
        {
            try
            {
                string exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
                string cookiesPath = Path.Combine(exeDir, "cookies.txt");

                if (!File.Exists(cookiesPath))
                {
                    CookieProtectionOverlay.Visibility = Visibility.Visible;
                    MainContentGrid.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 15 };
                }
                else
                {
                    CookieProtectionOverlay.Visibility = Visibility.Collapsed;
                    MainContentGrid.Effect = null;
                }
            }
            catch { }
        }

        private void GoToCookies_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Current?.Navigate(typeof(CookiesPage));
        }

        private void Runner_StatusChanged(object? sender, bool isRunning)
        {
            Dispatcher.Invoke(() =>
            {
                if (isRunning)
                {
                    EngineStatusText.Text = "Running";
                    EngineStatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
                    StartStopButton.Content = "Stop Engine";
                    StartStopButton.Appearance = ControlAppearance.Danger;
                    StartStopButton.Icon = new SymbolIcon(SymbolRegular.Stop24);
                    RunningSpinner.Visibility = Visibility.Visible;
                }
                else
                {
                    EngineStatusText.Text = "Stopped";
                    EngineStatusText.Foreground = System.Windows.Media.Brushes.Gray;
                    StartStopButton.Content = "Start Engine";
                    StartStopButton.Appearance = ControlAppearance.Primary;
                    StartStopButton.Icon = new SymbolIcon(SymbolRegular.Play24);
                    RunningSpinner.Visibility = Visibility.Collapsed;
                }
            });
        }

        private void Runner_OutputReceived(object? sender, string output)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText(output + Environment.NewLine);
                LogTextBox.ScrollToEnd();

                // Advanced alert/agentic tracking: Detect RPC or Cookie failures
                if (output.Contains("Discord update failed") || output.Contains("rookiepy extraction failed") || output.ToLower().Contains("cookie") && output.ToLower().Contains("error"))
                {
                    MainWindow.Current?.ShowSnackbar("RPC Error", "Steam RPC failed. You might wanna try refreshing cookies.txt", ControlAppearance.Danger);
                    MainWindow.Current?.ShowCookieError();
                }
            });
        }

        private void Runner_ErrorReceived(object? sender, string error)
        {
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText("[ERROR] " + error + Environment.NewLine);
                LogTextBox.ScrollToEnd();
            });
        }

        private void Runner_StateUpdated(object? sender, string state)
        {
            // For future or immediate use, bubble up state strings to UI card
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_runner.IsRunning)
            {
                _runner.Stop();
            }
            else
            {
                _runner.LogHistory.Clear();
                LogTextBox.Clear();
                _runner.Start();
            }
        }
    }
}
