// SPDX-FileCopyrightText: 2026 Bloomstrap
//
// SPDX-License-Identifier: MPL-2.0

namespace Froststrap.Integrations
{
    internal enum GraphicsPreset
    {
        Performance,
        Quality
    }

    /// <summary>
    /// One-click graphics presets. Each sets three places at once:
    /// the graphics fast flags (all on Roblox's allowlist), Roblox's own graphics settings
    /// (GlobalBasicSettings) and Sober's renderer options. Changes stay in memory until the user
    /// presses Save, like every other settings page.
    /// </summary>
    internal static class GraphicsPresets
    {
        /// <summary>Roblox's highest saved graphics level (the slider's top notch).</summary>
        private const string MaxQualityLevel = "10";

        /// <summary>
        /// Frame rate cap meaning "uncapped" in practice. Sober is started windowed, so the
        /// compositor doesn't hold it to the monitor's refresh rate.
        /// </summary>
        private const int UncappedFramerate = 2_000_000;

        /// <returns>false when Roblox's global settings couldn't be changed (read-only or not loaded)</returns>
        public static bool Apply(GraphicsPreset preset)
        {
            App.Logger.Info($"Loading the {preset} graphics preset");

            if (preset == GraphicsPreset.Performance)
            {
                App.Settings.Prop.SoberHighPriority = true;
                App.Settings.Prop.SoberGamingScheduler = true;
            }

            ApplyFastFlags(preset);
            ApplySober(preset);
            return ApplyGlobalSettings(preset);
        }

        private static void ApplyFastFlags(GraphicsPreset preset)
        {
            var flags = App.FastFlags;
            bool performance = preset == GraphicsPreset.Performance;

            // one undo step for the whole preset in the fast flag editor
            flags.SaveUndoSnapshot();
            flags.suspendUndoSnapshot = true;

            try
            {
                // lighting (Future/ShadowMap/Voxel) quality: 1 is the cheapest, 21 the best
                flags.SetPreset("Rendering.FrmQuality", performance ? "1" : "21");

                // textures: 0 is the lowest level, 3 the highest
                flags.SetPreset("Rendering.TextureQuality.OverrideEnabled", "True");
                flags.SetPreset("Rendering.TextureQuality.Level", performance ? "0" : "3");

                // anti-aliasing samples
                flags.SetPreset("Rendering.MSAA1", performance ? "1" : "4");

                // mesh detail: 0 makes every mesh use its lowest detail level; removing the flags restores Roblox's distances
                foreach (string level in (string[])["1", "2", "3", "4"])
                    flags.SetPreset($"Rendering.LowPolyMeshes{level}", performance ? "0" : null);

                // grass
                foreach (string part in (string[])["1", "2", "3"])
                    flags.SetPreset($"Rendering.RemoveGrass{part}", performance ? "0" : null);

                flags.SetPreset("Graphic.GraySky", performance ? "True" : null);
                flags.SetPreset("Rendering.PauseVoxerlizer", performance ? "True" : null);

                // the renderer is Sober's choice (use_opengl), not a Roblox flag
                flags.SetPresetEnum("Rendering.Mode", "None", null);
            }
            finally
            {
                flags.suspendUndoSnapshot = false;
            }
        }

        private static void ApplySober(GraphicsPreset preset)
        {
            var sober = App.SoberSettings;

            if (!sober.Loaded)
            {
                App.Logger.Warn("Sober's config isn't loaded; skipping its part of the preset");
                return;
            }

            sober.SetPreset("GraphicsOptimizationMode", preset == GraphicsPreset.Performance ? "performance" : "quality");

            // Vulkan for both: it's Sober's faster and better-looking path. OpenGL is only a crash workaround.
            sober.SetPreset("UseOpengl", "false");
            sober.SetPreset("EnableGamemode", "true");
        }

        private static bool ApplyGlobalSettings(GraphicsPreset preset)
        {
            var gbs = App.GlobalSettings;

            if (!gbs.Loaded || GBSEditor.GetReadOnly())
            {
                App.Logger.Warn("Roblox's global settings are read-only or not loaded; skipping their part of the preset");
                return false;
            }

            bool performance = preset == GraphicsPreset.Performance;

            gbs.SetPreset("Rendering.SavedQualityLevel", performance ? "1" : MaxQualityLevel);
            gbs.SetPreset("Rendering.MaxQualityEnabled", !performance);

            if (performance)
            {
                // the vignette is a full-screen post effect; quality leaves it as the user set it
                gbs.SetPreset("Rendering.VignetteEnabled", false);
                gbs.SetPreset("Rendering.VignetteEnableOption", false);
                gbs.SetPreset("Rendering.FramerateCap", UncappedFramerate);
            }

            return true;
        }
    }
}
