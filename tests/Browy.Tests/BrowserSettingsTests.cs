using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class BrowserSettingsTests
    {
        [Fact]
        public void SiteZoomLevel_DefaultsToOne()
        {
            double zoom = BrowserSettings.GetSiteZoomLevel("https://unknown-domain-test.com/page");
            Assert.Equal(1.0, zoom);
        }

        [Fact]
        public void SiteZoomLevel_SavesAndRetrievesPerDomain()
        {
            string url = "https://developer.mozilla.org/en-US/docs/Web";
            BrowserSettings.SetSiteZoomLevel(url, 1.25);

            double zoom = BrowserSettings.GetSiteZoomLevel(url);
            Assert.Equal(1.25, zoom);

            // Same domain different path should return same zoom
            double zoomDifferentPath = BrowserSettings.GetSiteZoomLevel("https://developer.mozilla.org/about");
            Assert.Equal(1.25, zoomDifferentPath);

            // Clean up: Reset back to 1.0
            BrowserSettings.SetSiteZoomLevel(url, 1.0);
            Assert.Equal(1.0, BrowserSettings.GetSiteZoomLevel(url));
        }

        [Fact]
        public void SiteZoomLevel_HandlesInvalidOrEmptyUrlGracefully()
        {
            double zoomEmpty = BrowserSettings.GetSiteZoomLevel("");
            Assert.Equal(1.0, zoomEmpty);

            BrowserSettings.SetSiteZoomLevel("", 1.5);
            // Should not crash
        }
    }
}
