// SPDX-FileCopyrightText: 2026 Froststrap
//
// SPDX-License-Identifier: MPL-2.0

namespace Froststrap.Extensions
{
    internal static class ThemeEx
    {
        public static Theme GetFinal(this Theme dialogTheme)
        {
            // Bloomstrap is designed dark-first (Catppuccin Mocha). "Default" no longer follows the
            // platform, which on niri often reports Light because no settings portal answers.
            return dialogTheme == Theme.Default ? Theme.Dark : dialogTheme;
        }
    }
}