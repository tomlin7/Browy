using System;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class NavigationHelperTests
    {
        [Theory]
        [InlineData("settings", "browy://settings")]
        [InlineData("browy://settings", "browy://settings")]
        [InlineData("about:settings", "browy://settings")]
        [InlineData("history", "browy://history")]
        [InlineData("browy://history", "browy://history")]
        [InlineData("about:history", "browy://history")]
        [InlineData("bookmarks", "browy://bookmarks")]
        [InlineData("browy://bookmarks", "browy://bookmarks")]
        [InlineData("about:bookmarks", "browy://bookmarks")]
        [InlineData("downloads", "browy://downloads")]
        [InlineData("browy://downloads", "browy://downloads")]
        [InlineData("about:downloads", "browy://downloads")]
        [InlineData("newtab", "browy://newtab")]
        [InlineData("start", "browy://newtab")]
        [InlineData("about:blank", "browy://newtab")]
        public void ResolveInputToUrl_ResolvesInternalAliases(string input, string expected)
        {
            string result = NavigationHelper.ResolveInputToUrl(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("https://github.com", "https://github.com")]
        [InlineData("http://example.com/test", "http://example.com/test")]
        [InlineData("file:///C:/test.txt", "file:///C:/test.txt")]
        [InlineData("view-source:https://google.com", "view-source:https://google.com")]
        public void ResolveInputToUrl_PreservesExplicitSchemes(string input, string expected)
        {
            string result = NavigationHelper.ResolveInputToUrl(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("github.com", "https://github.com")]
        [InlineData("www.reddit.com", "https://www.reddit.com")]
        [InlineData("sub.domain.co.uk/path", "https://sub.domain.co.uk/path")]
        [InlineData("localhost:8080", "https://localhost:8080")]
        [InlineData("127.0.0.1:3000/app", "https://127.0.0.1:3000/app")]
        public void ResolveInputToUrl_PrependsHttpsForDomainsAndLocalhost(string input, string expected)
        {
            string result = NavigationHelper.ResolveInputToUrl(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("how to learn rust", "https://www.google.com/search?q=how%20to%20learn%20rust")]
        [InlineData("weather today", "https://www.google.com/search?q=weather%20today")]
        public void ResolveInputToUrl_RoutesMultiWordQueriesToSearchEngine(string input, string expected)
        {
            string result = NavigationHelper.ResolveInputToUrl(input, "https://www.google.com/search?q=");
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("!yt lo-fi hip hop", "https://www.youtube.com/results?search_query=lo-fi%20hip%20hop")]
        [InlineData("!gh webview2", "https://github.com/search?q=webview2")]
        [InlineData("!w quantum physics", "https://en.wikipedia.org/wiki/Special:Search?search=quantum%20physics")]
        [InlineData("!d privacy browser", "https://duckduckgo.com/?q=privacy%20browser")]
        [InlineData("!r dotnet", "https://www.reddit.com/search/?q=dotnet")]
        [InlineData("!kagi fast search", "https://kagi.com/search?q=fast%20search")]
        [InlineData("!so null reference", "https://stackoverflow.com/search?q=null%20reference")]
        public void ResolveInputToUrl_ExpandsKeywordBangs(string input, string expected)
        {
            string result = NavigationHelper.ResolveInputToUrl(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ResolveInputToUrl_EmptyOrWhitespaceReturnsStartPage()
        {
            string result = NavigationHelper.ResolveInputToUrl("   ");
            Assert.False(string.IsNullOrWhiteSpace(result));
        }

        [Theory]
        [InlineData("https://github.com", true)]
        [InlineData("http://insecure.org", false)]
        [InlineData("browy://settings", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsSecure_IdentifiesHttpsCorrectly(string? url, bool expected)
        {
            bool result = NavigationHelper.IsSecure(url);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("browy://settings", true)]
        [InlineData("browy://newtab", true)]
        [InlineData("about:blank", true)]
        [InlineData("https://github.com", false)]
        [InlineData("http://example.com", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsInternalUrl_IdentifiesInternalSchemesCorrectly(string? url, bool expected)
        {
            bool result = NavigationHelper.IsInternalUrl(url);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("https://www.github.com/torvalds/linux", "www.github.com")]
        [InlineData("http://sub.domain.com:8080/path?arg=1", "sub.domain.com")]
        [InlineData("", "")]
        [InlineData("not a valid url", "not a valid url")]
        public void ExtractDomain_ExtractsHostName(string url, string expected)
        {
            string result = NavigationHelper.ExtractDomain(url);
            Assert.Equal(expected, result);
        }
    }
}
