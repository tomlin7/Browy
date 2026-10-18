using Browy.Models;
using Xunit;

namespace Browy.Tests
{
    public class BrowserTabTests
    {
        [Fact]
        public void TabSleeping_UpdatesOpacityAndStatus()
        {
            var tab = new BrowserTab();
            Assert.False(tab.IsSleeping);
            Assert.Equal(1.0, tab.TabOpacity);
            Assert.False(tab.ShowsSleepIcon);
            Assert.Empty(tab.SleepStatusToolTip);

            tab.IsSleeping = true;
            Assert.True(tab.IsSleeping);
            Assert.Equal(0.6, tab.TabOpacity);
            Assert.True(tab.ShowsSleepIcon);
            Assert.Contains("Sleeping tab", tab.SleepStatusToolTip);

            tab.IsSleeping = false;
            Assert.False(tab.IsSleeping);
            Assert.Equal(1.0, tab.TabOpacity);
            Assert.False(tab.ShowsSleepIcon);
        }

        [Fact]
        public void TabAudio_ShowsAudioButtonWhenPlayingOrMuted()
        {
            var tab = new BrowserTab();
            Assert.False(tab.ShowsAudioButton);

            tab.IsPlayingAudio = true;
            Assert.True(tab.ShowsAudioButton);
            Assert.Equal("\uE767", tab.AudioIconGlyph);
            Assert.Equal("Mute tab", tab.AudioToolTip);

            tab.IsMuted = true;
            Assert.True(tab.ShowsAudioButton);
            Assert.Equal("\uE74F", tab.AudioIconGlyph);
            Assert.Equal("Unmute tab", tab.AudioToolTip);

            tab.IsPlayingAudio = false;
            // Still shows button because it is muted
            Assert.True(tab.ShowsAudioButton);

            tab.IsMuted = false;
            Assert.False(tab.ShowsAudioButton);
        }

        [Fact]
        public void TabGrouping_UpdatesHasGroupAndBrushes()
        {
            var tab = new BrowserTab();
            Assert.False(tab.HasGroup);
            Assert.Null(tab.GroupName);
            Assert.Null(tab.GroupColorHex);

            tab.GroupName = "Research";
            tab.GroupColorHex = "#8B5CF6";

            Assert.True(tab.HasGroup);
            Assert.Equal("Research", tab.GroupName);
            Assert.Equal("#8B5CF6", tab.GroupColorHex);
            Assert.NotNull(tab.GroupBrush);
            Assert.NotNull(tab.GroupBorderBrush);
            Assert.NotNull(tab.GroupForegroundBrush);

            tab.GroupName = null;
            tab.GroupColorHex = null;
            Assert.False(tab.HasGroup);
        }
    }
}
