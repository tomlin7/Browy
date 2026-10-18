using System;
using System.Collections.Generic;
using System.Linq;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class SearchEnginesTests
    {
        [Fact]
        public void DefaultSearchEngines_ContainExpectedProviders()
        {
            var defaults = BrowserSettingsData.GetDefaultSearchEngines();

            Assert.NotEmpty(defaults);
            Assert.Contains(defaults, e => e.Name == "Google" && e.IsDefault && e.Keyword == "!g");
            Assert.Contains(defaults, e => e.Name == "DuckDuckGo" && e.Keyword == "!d");
            Assert.Contains(defaults, e => e.Name == "GitHub" && e.Keyword == "!gh");
            Assert.Contains(defaults, e => e.Name == "YouTube" && e.Keyword == "!yt");
            Assert.Contains(defaults, e => e.Name == "Wikipedia" && e.Keyword == "!w");
            Assert.Contains(defaults, e => e.Name == "Stack Overflow" && e.Keyword == "!so");
        }

        [Theory]
        [InlineData("searchengines", "browy://searchengines")]
        [InlineData("browy://searchengines", "browy://searchengines")]
        [InlineData("about:searchengines", "browy://searchengines")]
        [InlineData("search", "browy://searchengines")]
        public void ResolveInputToUrl_ResolvesSearchEnginesAliases(string input, string expectedUrl)
        {
            string resolved = NavigationHelper.ResolveInputToUrl(input);
            Assert.Equal(expectedUrl, resolved);
        }

        [Fact]
        public void GetBangSearchUrl_SubstitutesPercentS_Correctly()
        {
            string? url = NavigationHelper.GetBangSearchUrl("gh", "aspnetcore");
            Assert.NotNull(url);
            Assert.Equal("https://github.com/search?q=aspnetcore", url);
        }

        [Fact]
        public void GetBangSearchUrl_SupportsCustomSearchEngine()
        {
            var custom = new SearchEngineItem
            {
                Name = "Custom Docs",
                Keyword = "!docs",
                QueryUrl = "https://learn.microsoft.com/search/?terms=%s"
            };

            var originalEngines = BrowserSettings.Current.SearchEngines;
            try
            {
                BrowserSettings.Current.SearchEngines = new List<SearchEngineItem> { custom };

                string? url = NavigationHelper.GetBangSearchUrl("docs", "csharp");
                Assert.NotNull(url);
                Assert.Equal("https://learn.microsoft.com/search/?terms=csharp", url);
            }
            finally
            {
                BrowserSettings.Current.SearchEngines = originalEngines;
            }
        }

        [Fact]
        public void GetSearchEnginesHtml_RendersHubPageWithEnginesAndBangs()
        {
            var engines = new List<SearchEngineItem>
            {
                new() { Name = "Google", Keyword = "!g", QueryUrl = "https://www.google.com/search?q=%s", IsDefault = true, IsBuiltIn = true },
                new() { Name = "My Engine", Keyword = "!mine", QueryUrl = "https://example.com?q=%s", IsDefault = false, IsBuiltIn = false }
            };

            string html = InternalHubService.GetSearchEnginesHtml(engines);

            Assert.Contains("Search Engines &amp; Bangs", html);
            Assert.Contains("Google", html);
            Assert.Contains("!g", html);
            Assert.Contains("My Engine", html);
            Assert.Contains("!mine", html);
            Assert.Contains("Test Bang Omnibox Query", html);
            Assert.Contains("add_search_engine", html);
        }
    }
}
