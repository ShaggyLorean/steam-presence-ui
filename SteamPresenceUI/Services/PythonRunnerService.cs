using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading.Tasks;

namespace SteamPresenceUI.Services
{
    public class PythonRunnerService
    {
        private Process? _process;
        private readonly string _pythonExe = "python";
        private readonly string _scriptName = "main.py";
        private readonly string _workingDirectory;
        
        public static PythonRunnerService Shared { get; set; } = null!;
        public bool HasEverStarted { get; private set; } = false;
        public System.Text.StringBuilder LogHistory { get; } = new System.Text.StringBuilder();

        public string CurrentGame { get; private set; } = "";
        public bool HasRpcFailed { get; private set; } = false;
        public bool HasSuccessfullyRun { get; private set; } = false;

        public event EventHandler<string>? OutputReceived;
        public event EventHandler<string>? ErrorReceived;
        public event EventHandler<bool>? StatusChanged;
        public event EventHandler<string>? StateUpdated;
        public event EventHandler<bool>? SuccessfullyRan;

        public bool IsRunning => _process != null && !_process.HasExited;

        public PythonRunnerService(string basePath)
        {
            _workingDirectory = basePath;
            try {
                string cfgPath = System.IO.Path.Combine(_workingDirectory, "config.json");
                if (System.IO.File.Exists(cfgPath)) {
                    var node = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(cfgPath));
                    HasSuccessfullyRun = node?["HAS_SUCCESSFULLY_RUN"]?.GetValue<bool>() ?? false;
                }
            } catch { }

            // Detect existing Python engines running our main.py or engine.exe
            try
            {
                string query = "SELECT ProcessId, CommandLine, Name FROM Win32_Process WHERE Name = 'python.exe' OR Name = 'pythonw.exe' OR Name = 'engine.exe'";
                using var searcher = new ManagementObjectSearcher(query);
                using var results = searcher.Get();
                foreach (ManagementObject mo in results)
                {
                    var cmdLine = mo["CommandLine"]?.ToString() ?? "";
                    var procName = mo["Name"]?.ToString()?.ToLower() ?? "";
                    if (cmdLine.Contains("main.py") || procName == "engine.exe")
                    {
                        try
                        {
                            int pid = Convert.ToInt32(mo["ProcessId"]);
                            var p = Process.GetProcessById(pid);
                            // We now kill any ghost instances unconditionally, instead of attaching.
                            // If AUTO_START_ENGINE is enabled, it will naturally start a fresh one with working stdout.
                            p.Kill(true);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void MarkAsSuccessfullyRun()
        {
            HasSuccessfullyRun = true;
            SuccessfullyRan?.Invoke(this, true);
            try {
                string cfgPath = System.IO.Path.Combine(_workingDirectory, "config.json");
                System.Text.Json.Nodes.JsonNode node = new System.Text.Json.Nodes.JsonObject();
                if (System.IO.File.Exists(cfgPath)) {
                    var parsed = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(cfgPath));
                    if (parsed != null) node = parsed;
                }
                node["HAS_SUCCESSFULLY_RUN"] = true;
                System.IO.File.WriteAllText(cfgPath, node.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            } catch { }
        }

        public void Start()
        {
            if (IsRunning) return;
            HasEverStarted = true;

            // Check if user has compiled engine or standard python
            string engineExe = System.IO.Path.Combine(_workingDirectory, "engine.exe");
            bool useExe = System.IO.File.Exists(engineExe);

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = useExe ? engineExe : _pythonExe,
                    Arguments = useExe ? "" : _scriptName,
                    WorkingDirectory = _workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                }
            };

            _process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    LogHistory.AppendLine(e.Data);
                    OutputReceived?.Invoke(this, e.Data);
                    
                    if (e.Data.Contains("DEBUG steam current_game="))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(e.Data, @"current_game='(.*?)'");
                        if (match.Success) 
                        {
                            CurrentGame = match.Groups[1].Value;
                            if (!string.IsNullOrEmpty(CurrentGame) && !HasSuccessfullyRun)
                            {
                                MarkAsSuccessfullyRun();
                            }
                        }
                    }
                    else if (e.Data.Contains("Discord update failed") || e.Data.Contains("Exception"))
                    {
                        HasRpcFailed = true;
                    }
                    else if (e.Data.Contains("State updated:"))
                    {
                        HasRpcFailed = false;
                        StateUpdated?.Invoke(this, e.Data);
                    }
                    else if (e.Data.Contains("Game detected:"))
                        StateUpdated?.Invoke(this, e.Data);
                    else if (e.Data.Contains("No game detected."))
                    {
                        CurrentGame = "";
                        HasRpcFailed = false;
                        StateUpdated?.Invoke(this, "No Game");
                    }
                }
            };

            _process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    LogHistory.AppendLine("[ERROR] " + e.Data);
                    ErrorReceived?.Invoke(this, e.Data);
                }
            };

            _process.Exited += (s, e) =>
            {
                StatusChanged?.Invoke(this, false);
            };

            _process.EnableRaisingEvents = true;

            try
            {
                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                StatusChanged?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke(this, "Failed to start python process: " + ex.Message);
                StatusChanged?.Invoke(this, false);
            }
        }

        public void Stop()
        {
            if (!IsRunning || _process == null) return;

            var p = _process;
            _process = null;
            StatusChanged?.Invoke(this, false);

            Task.Run(() =>
            {
                try
                {
                    // Using taskkill /f /t /pid is often more reliable on Windows than p.Kill(true)
                    // as it ensures the entire process tree is terminated immediately.
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/F /T /PID {p.Id}",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    })?.WaitForExit(3000);
                }
                catch { }

                try { if (!p.HasExited) p.Kill(true); } catch { }
                try { p.Dispose(); } catch { }
            });
        }
    }
}
