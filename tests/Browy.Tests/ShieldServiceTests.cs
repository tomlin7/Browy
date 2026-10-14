using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class ShieldServiceTests
    {
        [Theory]
        [InlineData("https://doubleclick.net/ad.js", true)]
        [InlineData("https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js", true)]
        [InlineData("https://adnxs.com/seg?add=1", true)]
        [InlineData("https://criteo.com/delivery/ck.php", true)]
        [InlineData("https://hotjar.com/analytics.js", true)]
        [InlineData("https://clarity.ms/s/0.7.20/clarity.js", true)]
        [InlineData("https://segment.io/v1/p", true)]
        [InlineData("https://popads.net/serve.js", true)]
        public void IsBlocked_BlocksKnownAdAndTrackingDomains(string url, bool expected)
        {
            bool isBlocked = ShieldService.IsBlocked(url, out string reason);
            Assert.Equal(expected, isBlocked);
            if (expected)
            {
                Assert.False(string.IsNullOrWhiteSpace(reason));
            }
        }

        [Theory]
        [InlineData("https://example.com/pagead/js/something.js", true)]
        [InlineData("https://example.com/ads/google/banner.png", true)]
        [InlineData("https://connect.facebook.net/en_US/fbevents.js", true)]
        [InlineData("https://test.org/assets/ads.js", true)]
        public void IsBlocked_BlocksKnownAdPatterns(string url, bool expected)
        {
            bool isBlocked = ShieldService.IsBlocked(url, out string reason);
            Assert.Equal(expected, isBlocked);
        }

        [Theory]
        [InlineData("https://github.com", false)]
        [InlineData("https://wikipedia.org/wiki/Main_Page", false)]
        [InlineData("https://stackoverflow.com/questions", false)]
        [InlineData("https://microsoft.com", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsBlocked_AllowsBenignWebsites(string? url, bool expected)
        {
            bool isBlocked = ShieldService.IsBlocked(url!, out _);
            Assert.Equal(expected, isBlocked);
        }

        [Fact]
        public void ShieldService_WhitelistOverridesBlocking()
        {
            string url = "https://doubleclick.net/ad.js";
            Assert.True(ShieldService.IsBlocked(url, out _));

            ShieldService.AddToWhitelist("doubleclick.net");
            Assert.True(ShieldService.IsWhitelisted("doubleclick.net"));
            Assert.False(ShieldService.IsBlocked(url, out _));

            // Remove from whitelist
            ShieldService.RemoveFromWhitelist("doubleclick.net");
            Assert.False(ShieldService.IsWhitelisted("doubleclick.net"));
            Assert.True(ShieldService.IsBlocked(url, out _));
        }

        [Fact]
        public void ShieldService_CustomFilterDomainsAndPatterns()
        {
            string customAdUrl = "https://my-custom-annoying-tracker.com/pixel.gif";
            Assert.False(ShieldService.IsBlocked(customAdUrl, out _));

            ShieldService.AddCustomFilterDomain("my-custom-annoying-tracker.com");
            Assert.True(ShieldService.IsBlocked(customAdUrl, out string reason));
            Assert.Contains("my-custom-annoying-tracker.com", reason);

            string customPatternUrl = "https://news.example.com/telemetry/tracker-beacon.json";
            ShieldService.AddCustomFilterPattern("/telemetry/tracker-beacon");
            Assert.True(ShieldService.IsBlocked(customPatternUrl, out string patternReason));
            Assert.Equal("Custom Filter Pattern", patternReason);
        }
    }
}
