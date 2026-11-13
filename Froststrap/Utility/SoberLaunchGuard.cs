// SPDX-FileCopyrightText: 2026 Bloomstrap
//
// SPDX-License-Identifier: MPL-2.0

namespace Froststrap.Utility;

/// <summary>
/// Stops a repeated browser launch from killing the game it just started.
///
/// Every Sober launch begins with `flatpak kill org.vinegarhq.Sober` (SIGKILL to the whole sandbox),
/// so a game can be switched while another is running. Browsers and the Roblox site can fire the
/// roblox-player link twice in quick succession. Without this guard the second bootstrapper kills
/// the first Sober a few seconds into loading: the log stops mid-line with no clean exit and no
/// core dump. Any launch within <see cref="Window"/> of the previous one is treated as that
/// duplicate and dropped.
/// </summary>
internal static class SoberLaunchGuard
{
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(15);

    private static string StampPath => Path.Combine(Paths.Base, "Locks", "last-sober-launch");

    /// <summary>
    /// Records this launch and returns false if another one started within the window.
    /// Check and record happen under one inter-process lock, so two instances started in the
    /// same millisecond cannot both pass.
    /// </summary>
    public static bool TryBegin()
    {
        using var guard = new InterProcessLock("SoberLaunchGuard", TimeSpan.FromSeconds(3));

        if (!guard.IsAcquired)
        {
            App.Logger.Warn("SoberLaunchGuard: another launch holds the guard, treating this one as a duplicate");
            return false;
        }

        try
        {
            if (File.Exists(StampPath)
                && long.TryParse(File.ReadAllText(StampPath).Trim(), out long ticks))
            {
                var age = DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc);

                if (age >= TimeSpan.Zero && age < Window)
                {
                    App.Logger.Warn($"SoberLaunchGuard: duplicate launch {age.TotalSeconds:F1}s after the previous one, ignoring it");
                    return false;
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(StampPath)!);
            File.WriteAllText(StampPath, DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            // never block a launch because the stamp file misbehaved
            App.Logger.Warn($"SoberLaunchGuard: could not read or write the launch stamp: {ex.Message}");
        }

        return true;
    }
}
