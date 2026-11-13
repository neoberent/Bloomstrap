// SPDX-FileCopyrightText: 2026 Bloomstrap
//
// SPDX-License-Identifier: MPL-2.0

using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.Styling;

namespace Froststrap.UI.AppThemes.Bloom;

/// <summary>
/// Repaints FluentAvalonia's dark theme with the Bloom palette (Palette.axaml).
/// FluentAvalonia resolves its control brushes through StaticResource aliases, so overriding
/// keys alone does not reach the control templates. Instead we recolour the shared brush
/// instances in place, and every alias that points at them follows.
/// </summary>
internal static class BloomTheme
{
    // FluentAvalonia brush key -> (Bloom colour key, alpha 0..1)
    private static readonly (string FaKey, string BloomKey, double Alpha)[] Map =
    [
        // Text
        ("TextFillColorPrimaryBrush", "BloomText", 1),
        ("TextFillColorSecondaryBrush", "BloomSubtext", 1),
        ("TextFillColorTertiaryBrush", "BloomHint", 1),
        ("TextFillColorDisabledBrush", "BloomDisabled", 1),
        ("TextOnAccentFillColorPrimaryBrush", "BloomOnAccent", 1),
        ("TextOnAccentFillColorSecondaryBrush", "BloomOnAccent", 0.7),
        ("TextOnAccentFillColorDisabledBrush", "BloomDisabled", 1),
        ("AccentTextFillColorPrimaryBrush", "BloomAccent", 1),
        ("AccentTextFillColorSecondaryBrush", "BloomAccent", 1),
        ("AccentTextFillColorTertiaryBrush", "BloomAccentPressed", 1),

        // Control fills: surface0 capsules at 60 %, like the noctalia bar groups
        ("ControlFillColorDefaultBrush", "BloomSurface0", 0.6),
        ("ControlFillColorSecondaryBrush", "BloomSurface1", 0.6),
        ("ControlFillColorTertiaryBrush", "BloomSurface0", 0.4),
        ("ControlFillColorDisabledBrush", "BloomSurface0", 0.3),
        ("ControlFillColorInputActiveBrush", "BloomCrust", 1),
        ("ControlSolidFillColorDefaultBrush", "BloomSurface1", 1),
        ("ControlStrongFillColorDefaultBrush", "BloomHint", 1),
        ("ControlStrongFillColorDisabledBrush", "BloomSurface2", 1),
        ("SubtleFillColorSecondaryBrush", "BloomSurface0", 0.5),
        ("SubtleFillColorTertiaryBrush", "BloomSurface0", 0.3),
        ("ControlAltFillColorSecondaryBrush", "BloomCrust", 0.4),
        ("ControlAltFillColorTertiaryBrush", "BloomSurface0", 0.3),
        ("ControlAltFillColorQuarternaryBrush", "BloomSurface0", 0.5),

        // Strokes: one quiet 1px line everywhere (#45475a, as on the bar and panels)
        ("ControlStrokeColorDefaultBrush", "BloomSurface1", 0.7),
        ("ControlStrokeColorSecondaryBrush", "BloomSurface1", 1),
        ("ControlStrongStrokeColorDefaultBrush", "BloomHint", 1),
        ("ControlStrongStrokeColorDisabledBrush", "BloomSurface2", 1),
        ("CardStrokeColorDefaultBrush", "BloomSurface1", 0.7),
        ("CardStrokeColorDefaultSolidBrush", "BloomSurface1", 1),
        ("DividerStrokeColorDefaultBrush", "BloomSurface1", 0.7),
        ("SurfaceStrokeColorDefaultBrush", "BloomSurface1", 1),
        ("SurfaceStrokeColorFlyoutBrush", "BloomSurface1", 1),
        ("FocusStrokeColorOuterBrush", "BloomAccent", 1),
        ("FocusStrokeColorInnerBrush", "BloomBase", 1),
        ("ControlElevationBorderBrush", "BloomSurface1", 0.7),
        ("CircleElevationBorderBrush", "BloomSurface1", 0.7),
        ("AccentControlElevationBorderBrush", "BloomAccentPressed", 1),

        // Layers and backgrounds
        ("CardBackgroundFillColorDefaultBrush", "BloomSurface0", 0.6),
        ("CardBackgroundFillColorSecondaryBrush", "BloomSurface0", 0.4),
        ("LayerFillColorDefaultBrush", "BloomMantle", 0.5),
        ("LayerFillColorAltBrush", "BloomSurface0", 0.4),
        ("SolidBackgroundFillColorBaseBrush", "BloomBase", 1),
        ("SolidBackgroundFillColorSecondaryBrush", "BloomMantle", 1),
        ("SolidBackgroundFillColorTertiaryBrush", "BloomSurface0", 1),
        ("SolidBackgroundFillColorQuarternaryBrush", "BloomSurface1", 1),
        ("SmokeFillColorDefaultBrush", "BloomCrust", 0.6),

        // Accent
        ("AccentFillColorDefaultBrush", "BloomAccent", 1),
        ("AccentFillColorSecondaryBrush", "BloomAccentHover", 1),
        ("AccentFillColorTertiaryBrush", "BloomAccentPressed", 1),
        ("AccentFillColorDisabledBrush", "BloomSurface1", 0.5),
        ("AccentFillColorSelectedTextBackgroundBrush", "BloomAccent", 0.4),

        // Fluent's "transparent" fills are #00FFFFFF; any fade from them blends through white.
        // Same transparency, Mocha tint.
        ("SubtleFillColorTransparentBrush", "BloomSurface0", 0),
        ("SubtleFillColorDisabledBrush", "BloomSurface0", 0),
        ("ControlFillColorTransparentBrush", "BloomSurface0", 0),
        ("ControlAltFillColorTransparentBrush", "BloomSurface0", 0),
        ("ControlAltFillColorDisabledBrush", "BloomSurface0", 0),
        ("LayerOnMicaBaseAltFillColorTransparentBrush", "BloomSurface0", 0),

        // Status
        ("SystemFillColorSuccessBrush", "BloomSuccess", 1),
        ("SystemFillColorCautionBrush", "BloomWarning", 1),
        ("SystemFillColorCriticalBrush", "BloomDanger", 1),
        ("SystemFillColorAttentionBrush", "BloomAccent", 1),
        ("SystemFillColorNeutralBrush", "BloomHint", 1),
        ("SystemFillColorSuccessBackgroundBrush", "BloomSuccess", 0.12),
        ("SystemFillColorCautionBackgroundBrush", "BloomWarning", 0.12),
        ("SystemFillColorCriticalBackgroundBrush", "BloomDanger", 0.12),
        ("SystemFillColorAttentionBackgroundBrush", "BloomAccent", 0.10),
        ("SystemFillColorNeutralBackgroundBrush", "BloomSurface0", 0.6),
        ("SystemFillColorSolidAttentionBackgroundBrush", "BloomSurface0", 1),
        ("SystemFillColorSolidNeutralBackgroundBrush", "BloomSurface0", 1),
    ];

    public static Color Get(string bloomKey)
    {
        if (Application.Current?.TryGetResource(bloomKey, ThemeVariant.Dark, out var value) == true && value is Color c)
            return c;

        App.Logger.Warn($"Bloom palette key missing: {bloomKey}");
        return Colors.Magenta;
    }

    public static void Apply(FluentAvaloniaTheme faTheme)
    {
        faTheme.PreferSystemTheme = false;
        faTheme.PreferUserAccentColor = false;
        faTheme.CustomAccentColor = Get("BloomAccent");

        foreach (var (faKey, bloomKey, alpha) in Map)
        {
            if (!faTheme.TryGetResource(faKey, ThemeVariant.Dark, out var resource))
                continue;

            var color = Get(bloomKey);
            color = Color.FromArgb((byte)Math.Round(color.A * alpha), color.R, color.G, color.B);

            switch (resource)
            {
                case SolidColorBrush solid:
                    solid.Color = color;
                    solid.Opacity = 1;
                    break;

                // Elevation borders are subtle gradients in Fluent; Bloom keeps them flat.
                case GradientBrush gradient:
                    foreach (var stop in gradient.GradientStops)
                        stop.Color = color;
                    break;
            }
        }
    }
}
