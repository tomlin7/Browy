using System.Collections.Generic;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class FeatureFlagsTests
    {
        [Fact]
        public void AllFlags_ContainsExpectedFlags()
        {
            var flags = FeatureFlagsService.AllFlags;
            Assert.NotEmpty(flags);
            Assert.Contains(flags, f => f.Key == "experimental_adblock_cosmetic");
            Assert.Contains(flags, f => f.Key == "experimental_smart_copy_cleaner");
            Assert.Contains(flags, f => f.Key == "experimental_tab_discarding");
            Assert.Contains(flags, f => f.Key == "experimental_omnibox_calculator");
        }

        [Fact]
        public void GetEffectiveValue_RespectsDefaultAndCustom()
        {
            var custom = new Dictionary<string, bool>
            {
                ["experimental_compact_titlebar"] = true,
                ["experimental_smart_copy_cleaner"] = false
            };

            // Defaults: compact titlebar is false by default, smart copy cleaner is true
            bool compact = FeatureFlagsService.GetEffectiveValue("experimental_compact_titlebar", custom);
            Assert.True(compact);

            bool cleaner = FeatureFlagsService.GetEffectiveValue("experimental_smart_copy_cleaner", custom);
            Assert.False(cleaner);

            // Unmodified flag returns default
            bool adblock = FeatureFlagsService.GetEffectiveValue("experimental_adblock_cosmetic", custom);
            Assert.True(adblock);
        }

        [Fact]
        public void IsModified_DetectsUserChanges()
        {
            var custom = new Dictionary<string, bool>
            {
                ["experimental_compact_titlebar"] = true // default is false
            };

            Assert.True(FeatureFlagsService.IsModified("experimental_compact_titlebar", custom));
            Assert.False(FeatureFlagsService.IsModified("experimental_smart_copy_cleaner", custom));
        }

        [Fact]
        public void GetFlagsHtml_RendersCompletePage()
        {
            var custom = new Dictionary<string, bool>
            {
                ["experimental_smart_copy_cleaner"] = true
            };

            string html = InternalHubService.GetFlagsHtml(custom);
            Assert.Contains("Experiments & Feature Flags", html);
            Assert.Contains("experimental_adblock_cosmetic", html);
            Assert.Contains("relaunchBar", html);
        }

        [Fact]
        public void BrowserSettings_SetAndResetFeatureFlag()
        {
            BrowserSettings.SetFeatureFlag("test_flag_unit", true);
            Assert.True(BrowserSettings.IsFeatureEnabled("test_flag_unit", false));

            BrowserSettings.SetFeatureFlag("test_flag_unit", false);
            Assert.False(BrowserSettings.IsFeatureEnabled("test_flag_unit", true));

            BrowserSettings.ResetFeatureFlags();
            Assert.False(BrowserSettings.IsFeatureEnabled("test_flag_unit", false));
        }
    }
}
