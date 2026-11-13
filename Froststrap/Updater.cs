// SPDX-FileCopyrightText: 2026 Froststrap
//
// SPDX-License-Identifier: MPL-2.0

using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

namespace Froststrap;

internal class Updater
{
    public static async Task RunMigrations(string? previousVersion = null)
    {
        if (OperatingSystem.IsLinux())
            SetupSoberSymlink();

        string currentVer = App.InternalVersion;
        string? existingVer = previousVersion ?? App.State.Prop.LastMigratedVersion;

        if (existingVer is null && !App.Settings.IsSaved)
        {
            App.Logger.Info($"Fresh install detected — stamping LastMigratedVersion as {currentVer}");
            App.State.Prop.LastMigratedVersion = currentVer;
            App.State.Save();
            return;
        }

        if (existingVer is null)
        {
            var legacyStateCheck = new JsonManager<RobloxState>();
            if (!legacyStateCheck.IsSaved)
            {
                App.Logger.Info("No LastMigratedVersion but no legacy data found — treating as already migrated");
                App.State.Prop.LastMigratedVersion = currentVer;
                App.State.Save();
                return;
            }

            App.Logger.Info("Legacy RobloxState data found — treating as pre-migration install");
            existingVer = "0.0.0";
        }

        App.Logger.Info($"Comparing migration versions: existing='{existingVer}', current='{currentVer}'");
        if (Utility.Versioning.CompareVersions(existingVer, currentVer) != VersionComparison.LessThan)
        {
            App.Logger.Info($"Migrations up to date (last={existingVer}, current={currentVer})");
            return;
        }

        App.Logger.Info($"Running migrations: {existingVer} -> {currentVer}");

        if (Utility.Versioning.CompareVersions(existingVer, "1.4.0.0") == VersionComparison.LessThan)
        {
            JsonManager<RobloxState> legacyRobloxState = new();

            if (legacyRobloxState.IsSaved)
            {
                if (legacyRobloxState.Load(false))
                {
                    App.PlayerState.Prop.VersionGuid = legacyRobloxState.Prop.Player.VersionGuid;
                    App.PlayerState.Prop.PackageHashes = legacyRobloxState.Prop.Player.PackageHashes;
                    App.PlayerState.Prop.ModManifest = legacyRobloxState.Prop.ModManifest;

                    App.StudioState.Prop.VersionGuid = legacyRobloxState.Prop.Studio.VersionGuid;
                    App.StudioState.Prop.PackageHashes = legacyRobloxState.Prop.Studio.PackageHashes;
                }

                legacyRobloxState.Delete();
            }

            if (App.Settings.Prop.Theme == Theme.Custom)
                App.Settings.Prop.Theme = Theme.Default;

            TryDelete(Path.Combine(Paths.Cache, "GameHistory.json"));
        }
        if (Utility.Versioning.CompareVersions(existingVer, "1.4.2") == VersionComparison.LessThan)
        {
            string genCacheDir = Path.Combine(Path.GetTempPath(), App.ProjectName, "mod-generator");
            string pluginCacheDir = Path.Combine(Paths.Roblox, "Plugins", "FroststrapStudioRPC.rbxmx");

            if (Directory.Exists(genCacheDir))
            {
                Directory.Delete(genCacheDir, true);
                App.Logger.Info("Deleted mod-generator cache for migration.");
            }

            if (Directory.Exists(pluginCacheDir))
            {
                Directory.Delete(pluginCacheDir, true);
                App.Logger.Info("Deleted studio plugin for migration.");
            }

            TryDelete(Path.Combine(Paths.Cache, "channelCache.json"));
            TryDelete(Path.Combine(Paths.Cache, "channelCacheMeta.json"));
            TryDelete(Path.Combine(Paths.Cache, "datacenters_cache.json"));
        }

        if (Utility.Versioning.CompareVersions(existingVer, "1.5.1") == VersionComparison.LessThan)
        {
            App.Settings.Prop.BootstrapperStyle = BootstrapperStyle.FluentAeroDialog;
            App.Settings.Prop.SelectedBackdrop = WindowsBackdrops.None;
        }

        App.State.Prop.LastMigratedVersion = currentVer;
        App.State.Save();

        if (App.Settings.Loaded) App.Settings.Save();
        if (App.PlayerState.Loaded) App.PlayerState.Save();
        if (App.StudioState.Loaded) App.StudioState.Save();

        App.Logger.Info($"Migrations complete — LastMigratedVersion set to {currentVer}");
    }

    [SupportedOSPlatform("linux")]
    internal static async void SetupSoberSymlink()
    {
        string flatpakId = "org.vinegarhq.Sober";
        string flatpakDataPath = Path.Combine(Paths.UserProfile, ".var", "app", flatpakId);
        string soberTarget = Path.Combine(Paths.Versions, "Sober");

        if (IsSymlinkPointingAt(flatpakDataPath, soberTarget))
        {
            App.Logger.Info("Sober symlink already in place, skipping.");
            return;
        }

        // Bloomstrap: if Sober's data link already points at a real data folder (for example the one
        // Froststrap created), share it rather than re-pointing the link at an empty folder of our own.
        // Re-pointing loses the Roblox login and client, and the two apps would keep stealing the link.
        string? existingData = ResolveFinalDirectory(flatpakDataPath);
        if (IsSymlink(flatpakDataPath) && existingData is not null
            && existingData != ResolveFinalDirectory(soberTarget) && HoldsNoFiles(soberTarget))
        {
            App.Logger.Info($"Sharing existing Sober data: {soberTarget} -> {existingData}");

            // other startup code may already have made an empty folder skeleton here; it holds no files
            if (Directory.Exists(soberTarget) && !IsSymlink(soberTarget))
                Directory.Delete(soberTarget, recursive: true);

            Directory.CreateDirectory(Path.GetDirectoryName(soberTarget)!);
            Directory.CreateSymbolicLink(soberTarget, existingData);
            return;
        }

        App.Logger.Info($"Setting up Sober symlink: {flatpakDataPath} -> {soberTarget}");

        Directory.CreateDirectory(soberTarget);

        if (Directory.Exists(flatpakDataPath) && !IsSymlink(flatpakDataPath))
        {
            App.Logger.Info($"Copying existing Sober data from {flatpakDataPath} to {soberTarget}");
            await Utility.Threading.RunAsync("cp", $"-a \"{flatpakDataPath}/.\" \"{soberTarget}/\"");

            App.Logger.Info($"Removing original Sober data directory at {flatpakDataPath}");
            await Utility.Threading.RunAsync("rm", $"-rf \"{flatpakDataPath}\"");
        }
        else if (IsSymlink(flatpakDataPath))
        {
            App.Logger.Info($"Removing stale symlink at {flatpakDataPath}");
            Directory.Delete(flatpakDataPath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(flatpakDataPath)!);

        Directory.CreateSymbolicLink(flatpakDataPath, soberTarget);
        App.Logger.Info($"Created symlink: {flatpakDataPath} -> {soberTarget}");
    }

    [SupportedOSPlatform("linux")]
    private static bool IsSymlink(string path)
    {
        if (!Path.Exists(path))
            return false;

        try
        {
            var attributes = File.GetAttributes(path);
            return attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch { return false; }
    }

    [SupportedOSPlatform("linux")]
    private static bool IsSymlinkPointingAt(string path, string expectedTarget)
    {
        if (!IsSymlink(path))
            return false;

        try
        {
            // compare where both paths really end up, so a shared data folder reached through
            // another symlink (see SetupSoberSymlink) counts as already in place
            string? actual = ResolveFinalDirectory(path);
            string? expected = ResolveFinalDirectory(expectedTarget) ?? expectedTarget;
            return actual is not null && actual == expected.TrimEnd('/');
        }
        catch { return false; }
    }

    /// <summary>True when the path holds no real Sober data (missing, empty, or only our own GlobalBasicSettings).</summary>
    private static bool HoldsNoFiles(string path)
    {
        try
        {
            if (!Path.Exists(path))
                return true;

            if (IsSymlink(path))
                return false;

            // the only thing a bootstrapper writes here on its own is GlobalBasicSettings under appData
            string appData = Path.Combine(path, "data", "sober", "appData");
            return !Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Any(f => !f.StartsWith(appData, StringComparison.Ordinal));
        }
        catch { return false; }
    }

    /// <summary>Follows every symlink in the chain and returns the real directory, or null if it is missing.</summary>
    [SupportedOSPlatform("linux")]
    private static string? ResolveFinalDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
                return null;

            string? final = IsSymlink(path)
                ? Directory.ResolveLinkTarget(path, returnFinalTarget: true)?.FullName
                : Path.GetFullPath(path);

            return final is not null && Directory.Exists(final) ? final.TrimEnd('/') : null;
        }
        catch { return null; }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best-effort */ }
    }
}
