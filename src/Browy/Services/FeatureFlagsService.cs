using System;
using System.Collections.Generic;
using System.Linq;
using Browy.Models;

namespace Browy.Services
{
    public static class FeatureFlagsService
    {
        private static readonly List<FeatureFlagDefinition> DefinedFlags = new()
        {
            new FeatureFlagDefinition(
                "experimental_adblock_cosmetic",
                "AdBlock Cosmetic Element Hiding",
                "Injects DOM stylesheet rules into web pages to automatically collapse empty ad containers and whitespace placeholders left by blocked banners.",
                "Privacy & Security",
                true),

            new FeatureFlagDefinition(
                "experimental_smart_copy_cleaner",
                "Clean Tracking Parameters from Copied URLs",
                "Automatically strips marketing and analytics tracking parameters (utm_source, fbclid, gclid, etc.) when copying links to the clipboard.",
                "Privacy & Security",
                true),

            new FeatureFlagDefinition(
                "experimental_tab_discarding",
                "Aggressive Tab Memory Discarding",
                "Automatically discards rendered web view memory and resources for sleeping background tabs to maximize system RAM efficiency.",
                "Performance",
                true),

            new FeatureFlagDefinition(
                "experimental_omnibox_calculator",
                "Omnibox Math Calculator",
                "Evaluates arithmetic calculations directly inside the address bar dropdown (e.g. 42 * 1337 or 15% of 240) before executing search navigation.",
                "Productivity",
                true),

            new FeatureFlagDefinition(
                "experimental_smooth_scroll",
                "Smooth Viewport Scrolling",
                "Applies modern interpolated physics scrolling to web documents for ultra-smooth navigation on high refresh rate displays.",
                "Browsing Experience",
                true),

            new FeatureFlagDefinition(
                "experimental_pip_auto",
                "Automatic Picture-in-Picture on Tab Switch",
                "Automatically pops out playing videos into a compact Picture-in-Picture overlay whenever switching away from the active media tab.",
                "Media & Audio",
                false),

            new FeatureFlagDefinition(
                "experimental_force_dark",
                "Force Web Dark Mode",
                "Applies high-contrast dark color schemes to all light-themed websites using smart CSS inversion heuristics.",
                "Browsing Experience",
                false),

            new FeatureFlagDefinition(
                "experimental_compact_titlebar",
                "Ultra-Compact Window Chrome",
                "Reduces tab strip and omnibox padding by 20% to maximize screen real estate for web content.",
                "User Interface",
                false)
        };

        public static IReadOnlyList<FeatureFlagDefinition> AllFlags => DefinedFlags;

        public static FeatureFlagDefinition? GetFlag(string key)
        {
            return DefinedFlags.FirstOrDefault(f => f.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        }

        public static bool GetEffectiveValue(string key, IDictionary<string, bool>? customFlags)
        {
            if (customFlags != null && customFlags.TryGetValue(key, out bool customVal))
            {
                return customVal;
            }

            var def = GetFlag(key);
            return def?.DefaultValue ?? false;
        }

        public static bool IsModified(string key, IDictionary<string, bool>? customFlags)
        {
            if (customFlags == null || !customFlags.TryGetValue(key, out bool customVal))
            {
                return false;
            }

            var def = GetFlag(key);
            return def != null && def.DefaultValue != customVal;
        }
    }
}
