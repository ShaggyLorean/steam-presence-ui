using Microsoft.Win32;
using System.IO;
using System.Reflection;

namespace SteamPresenceUI.Services
{
    public class StartupService
    {
        private const string AppName = "SteamPresenceCompanion";
        private const string RunKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

        public bool IsRegistered()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            if (key == null) return false;

            var val = key.GetValue(AppName);
            return val != null;
        }

        public bool IsMinimized()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            if (key == null) return false;

            var val = key.GetValue(AppName) as string;
            return val != null && val.Contains("--minimized");
        }

        public void Register(bool startMinimized = false)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key == null) return;

            string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
            string cmd = startMinimized ? $"\"{appPath}\" --minimized" : $"\"{appPath}\"";
            key.SetValue(AppName, cmd);
        }

        public void Unregister()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key == null) return;

            if (key.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName);
            }
        }
    }
}
