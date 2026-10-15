using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class TrackingCleanerTests
    {
        [Theory]
        [InlineData("https://example.com/article?utm_source=twitter&utm_medium=social", "https://example.com/article")]
        [InlineData("https://store.com/item?id=123&fbclid=abcdef9876", "https://store.com/item?id=123")]
        [InlineData("https://search.com?q=dotnet&gclid=xyz123&utm_campaign=winter", "https://search.com/?q=dotnet")]
        [InlineData("https://news.ycombinator.com/item?id=42", "https://news.ycombinator.com/item?id=42")]
        [InlineData("https://github.com/billy-browy/browy", "https://github.com/billy-browy/browy")]
        public void CleanTrackingParameters_StripsTrackersAndPreservesPayload(string original, string expected)
        {
            string actual = NavigationHelper.CleanTrackingParameters(original);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CleanTrackingParameters_HandlesInvalidAndEmptyUrls()
        {
            Assert.Equal("", NavigationHelper.CleanTrackingParameters(""));
            Assert.Null(NavigationHelper.CleanTrackingParameters(null!));
            Assert.Equal("not a valid url", NavigationHelper.CleanTrackingParameters("not a valid url"));
        }
    }
}
