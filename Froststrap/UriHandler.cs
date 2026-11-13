using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Froststrap
{
    internal static class UriHandler
    {
        static readonly string[] Schemes =
        [
            "roblox",
            "roblox-player",
            "roblox-studio",
            "roblox-studio-auth"
        ];

        static readonly IHandler? Handler =
            OperatingSystem.IsWindows() ? new WindowsHandler() :
            OperatingSystem.IsLinux() ? new LinuxHandler() :
            null; // macOS registers through the app bundle's Info.plist

        public static void RegisterProtocolHandlers() => Safely(() => Handler?.Register());

        public static void UnregisterProtocolHandlers() => Safely(() => Handler?.Unregister());

        public static void EnsureRegistered() => Safely(() =>
        {
            if (Handler is not null && !Handler.IsRegistered())
                Handler.Register();
        });

        // Velopack no likey likey excptions in install/uninstall phase
        // So let's handle it properly
        static void Safely(Action action)
        {
            try { action(); }
            catch (Exception ex) { App.Logger.Error(ex, "UriHandler failed"); }
        }

        interface IHandler
        {
            void Register();
            void Unregister();
            bool IsRegistered();
        }

        [SupportedOSPlatform("windows")]
        sealed class WindowsHandler : IHandler
        {
            public void Register()
            {
                string exe = Environment.ProcessPath!;
                foreach (string scheme in Schemes)
                {
                    using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{scheme}");
                    key.SetValue("", $"URL: {scheme} Protocol");
                    key.SetValue("URL Protocol", "");
                    using var command = key.CreateSubKey(@"shell\open\command");
                    command.SetValue("", $"\"{exe}\" \"%1\"");
                }
            }

            public void Unregister()
            {
                foreach (string scheme in Schemes)
                    Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{scheme}", throwOnMissingSubKey: false);
            }

            public bool IsRegistered()
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\roblox-player\shell\open\command");
                return key?.GetValue("") as string == $"\"{Environment.ProcessPath}\" \"%1\"";
            }
        }

        [SupportedOSPlatform("linux")]
        sealed class LinuxHandler : IHandler
        {
            static string DesktopFile => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "applications", "bloomstrap.desktop");

            // Inside an AppImage, ProcessPath is a temporary mount, APPIMAGE is the real file.
            static string ExePath => Environment.GetEnvironmentVariable("APPIMAGE") ?? Environment.ProcessPath!;

            static string IconFile => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "icons", "hicolor", "512x512", "apps", "bloomstrap.png");

            // The AppImage carries its icon at the root of its mount; packages install theirs system-wide.
            static string? AppImageIcon =>
                Environment.GetEnvironmentVariable("APPIMAGE") is not null
                && Environment.GetEnvironmentVariable("APPDIR") is { Length: > 0 } appDir
                && File.Exists(Path.Combine(appDir, "Bloomstrap.png"))
                    ? Path.Combine(appDir, "Bloomstrap.png")
                    : null;

            public void Register()
            {
                string mime = string.Concat(Schemes.Select(s => $"x-scheme-handler/{s};"));

                // This file is both the link handler and the app menu entry. It must stay visible:
                // it shadows a package's /usr/share/applications/bloomstrap.desktop of the same name.
                Directory.CreateDirectory(Path.GetDirectoryName(DesktopFile)!);
                File.WriteAllText(DesktopFile,
                    "[Desktop Entry]\nType=Application\nName=Bloomstrap\nComment=Roblox bootstrapper for Sober on Linux\n" +
                    $"Exec=\"{ExePath}\" %u\nIcon=bloomstrap\nTerminal=false\nCategories=Game;\nStartupWMClass=bloomstrap\nMimeType={mime}\n");

                if (AppImageIcon is { } icon)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(IconFile)!);
                    File.Copy(icon, IconFile, overwrite: true);
                    Run("gtk-update-icon-cache", $"-q -t \"{Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(IconFile)))}\"");
                }

                foreach (string scheme in Schemes)
                    Run("xdg-mime", $"default bloomstrap.desktop x-scheme-handler/{scheme}");
                Run("update-desktop-database", $"\"{Path.GetDirectoryName(DesktopFile)}\"");
            }

            public void Unregister()
            {
                if (File.Exists(DesktopFile)) File.Delete(DesktopFile);
                if (AppImageIcon is not null && File.Exists(IconFile)) File.Delete(IconFile);
            }

            public bool IsRegistered()
            {
                if (!File.Exists(DesktopFile)) return false;

                string entry = File.ReadAllText(DesktopFile);
                if (!entry.Contains($"Exec=\"{ExePath}\"", StringComparison.Ordinal)) return false;
                // 1.0.0 and 1.0.1 wrote a hidden entry, which kept Bloomstrap out of the app menu
                if (entry.Contains("NoDisplay=true", StringComparison.Ordinal)) return false;
                if (AppImageIcon is not null && !File.Exists(IconFile)) return false;

                using var query = Process.Start(new ProcessStartInfo("xdg-mime", "query default x-scheme-handler/roblox-player")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                });
                string current = query?.StandardOutput.ReadToEnd().Trim() ?? "";
                query?.WaitForExit(5000);
                return current == "bloomstrap.desktop";
            }

            static void Run(string file, string args)
            {
                try { Process.Start(new ProcessStartInfo(file, args) { UseShellExecute = false })?.WaitForExit(5000); }
                catch { /* tool may be missing, not fatal */ }
            }
        }
    }
}
