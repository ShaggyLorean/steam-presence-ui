using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Reflection;

Console.WriteLine("Installing Steam Presence...");
string installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SteamPresence");

try
{
    if (Directory.Exists(installPath))
    {
        Console.WriteLine("Cleaning old installation...");
        foreach (var process in Process.GetProcessesByName("SteamPresenceUI")) try { process.Kill(); } catch { }
        foreach (var process in Process.GetProcessesByName("engine")) try { process.Kill(); } catch { }
        System.Threading.Thread.Sleep(1000);
        try { Directory.Delete(installPath, true); } catch { }
    }
    Directory.CreateDirectory(installPath);

    // Extract from Embedded Resource
    var assembly = Assembly.GetExecutingAssembly();
    using (Stream stream = assembly.GetManifestResourceStream("payload.zip"))
    {
        if (stream != null)
        {
            Console.WriteLine("Extracting payload...");
            using (ZipArchive archive = new ZipArchive(stream))
            {
                archive.ExtractToDirectory(installPath);
            }
            Console.WriteLine("Extraction complete.");
        }
        else
        {
            Console.WriteLine("Error: Embedded payload.zip not found!");
            Console.ReadKey();
            return;
        }
    }

    // Create Desktop Shortcut
    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    string shortcutPath = Path.Combine(desktopPath, "SteamPresence Companion.lnk");
    string exePath = Path.Combine(installPath, "SteamPresenceUI.exe");

    string script = $"$s=(New-Object -ComObject WScript.Shell).CreateShortcut('{shortcutPath}');" +
                    $"$s.TargetPath='{exePath}';" +
                    $"$s.WorkingDirectory='{installPath}';" +
                    $"$s.IconLocation='{exePath},0';" +
                    "$s.Save()";
    
    Process.Start(new ProcessStartInfo("powershell", $"-ExecutionPolicy Bypass -Command \"{script}\"") { CreateNoWindow = true, UseShellExecute = false });
    
    Console.WriteLine("Installation successful!");
    Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
    Console.ReadKey();
}
