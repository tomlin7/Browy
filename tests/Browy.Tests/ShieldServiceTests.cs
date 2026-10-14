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
    }
}
