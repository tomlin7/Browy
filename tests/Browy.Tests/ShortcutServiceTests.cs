using System.Collections.Generic;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class ShortcutServiceTests
    {
        [Theory]
        [InlineData("ctrl+shift+t", "Ctrl+Shift+T")]
        [InlineData("Ctrl+T", "Ctrl+T")]
        [InlineData("alt+p", "Alt+P")]
        [InlineData("f11", "F11")]
        [InlineData("ctrl+\\", "Ctrl+\\")]
        [InlineData("ctrl+shift+b", "Ctrl+Shift+B")]
        public void NormalizeKeyChord_StandardizesFormatting(string input, string expected)
        {
            string actual = ShortcutService.NormalizeKeyChord(input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetDefaultShortcut_ReturnsKnownActions()
        {
            Assert.Equal("Ctrl+T", ShortcutService.GetDefaultShortcut("new_tab"));
            Assert.Equal("Ctrl+W", ShortcutService.GetDefaultShortcut("close_tab"));
            Assert.Equal("Ctrl+K", ShortcutService.GetDefaultShortcut("command_palette"));
            Assert.Equal("F11", ShortcutService.GetDefaultShortcut("fullscreen"));
            Assert.Equal("Alt+P", ShortcutService.GetDefaultShortcut("pip"));
        }

        [Fact]
        public void GetEffectiveShortcut_PrefersCustomOverDefault()
        {
            var custom = new Dictionary<string, string>
            {
                ["command_palette"] = "Ctrl+Shift+P"
            };

            string effective = ShortcutService.GetEffectiveShortcut("command_palette", custom);
            Assert.Equal("Ctrl+Shift+P", effective);

            string unmodified = ShortcutService.GetEffectiveShortcut("new_tab", custom);
            Assert.Equal("Ctrl+T", unmodified);
        }

        [Fact]
        public void TryMatchChord_MatchesCustomShortcut()
        {
            var custom = new Dictionary<string, string>
            {
                ["find"] = "Ctrl+Shift+F"
            };

            bool matched = ShortcutService.TryMatchChord("Ctrl+Shift+F", custom, out string action);
            Assert.True(matched);
            Assert.Equal("find", action);
        }

        [Fact]
        public void TryMatchChord_FallsBackToDefaultIfNotOverridden()
        {
            var custom = new Dictionary<string, string>();

            bool matched = ShortcutService.TryMatchChord("Ctrl+T", custom, out string action);
            Assert.True(matched);
            Assert.Equal("new_tab", action);
        }

        [Fact]
        public void TryMatchChord_SuppressesDefaultIfActionWasRemapped()
        {
            var custom = new Dictionary<string, string>
            {
                ["new_tab"] = "Ctrl+N"
            };

            // Pressing original Ctrl+T should not trigger new_tab since it was remapped to Ctrl+N
            bool matched = ShortcutService.TryMatchChord("Ctrl+T", custom, out string action);
            Assert.False(matched);

            // Pressing new chord Ctrl+N should trigger new_tab
            bool matchedCustom = ShortcutService.TryMatchChord("Ctrl+N", custom, out string customAction);
            Assert.True(matchedCustom);
            Assert.Equal("new_tab", customAction);
        }
    }
}
