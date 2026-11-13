// SPDX-FileCopyrightText: 2026 Froststrap
//
// SPDX-License-Identifier: MPL-2.0

namespace Froststrap.Utility;

static class Processes
{
    public static Process[] GetProcessesSafe()
    {
        try
        {
            return Process.GetProcesses();
        }
        catch (ArithmeticException ex) // thanks microsoft
        {
            App.Logger.Error($"Unable to fetch processes! {ex}");
            return []; // can we retry?
        }
    }

    /// <summary>
    /// Cheap liveness check for one PID. On Linux this is a single stat of /proc/&lt;pid&gt;
    /// (zombies count as dead) instead of enumerating every process on the system.
    /// </summary>
    public static bool IsProcessAlive(int pid)
    {
        if (pid <= 0)
            return false;

        if (OperatingSystem.IsLinux())
        {
            try
            {
                // field 3 of /proc/<pid>/stat is the state; 'Z' (zombie) and 'X' (dead) mean it is gone
                string stat = File.ReadAllText($"/proc/{pid}/stat");
                int close = stat.LastIndexOf(')');
                return close < 0 || close + 2 >= stat.Length || stat[close + 2] is not ('Z' or 'X');
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return Directory.Exists($"/proc/{pid}"); }
        }

        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (System.ComponentModel.Win32Exception) { return true; } // exists but we may not query it
    }

    /// <summary>
    /// Returns the PID of a running process with this exact name, or 0. Every Process object is disposed.
    /// </summary>
    public static int FindProcessId(string processName)
    {
        int found = 0;

        foreach (var process in GetProcessesSafe())
        {
            try
            {
                if (found == 0 && process.ProcessName == processName && IsProcessAlive(process.Id))
                    found = process.Id;
            }
            catch { }
            finally { process.Dispose(); }
        }

        return found;
    }

    public static bool IsRobloxRunning()
    {
        Process[] processes = GetProcessesSafe();
        string processName = Path.GetFileNameWithoutExtension(App.RobloxPlayerAppName);

        if (OperatingSystem.IsLinux())
            return processes.Any(x => x.ProcessName == "sober");
        else
            return processes.Any(x => x.ProcessName == processName);
    }

    public static void KillSober()
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = "flatpak",
                Arguments = "kill org.vinegarhq.Sober",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process?.WaitForExit(1000);
        }
        catch
        {
        }
    }

    public static void KillBackgroundUpdater()
    {
        using EventWaitHandle handle = new(false, EventResetMode.AutoReset, "Froststrap-BackgroundUpdaterKillEvent");
        handle.Set();
    }
}
