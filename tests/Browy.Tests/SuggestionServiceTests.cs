using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Browy.Models;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class SuggestionServiceTests
    {
        [Fact]
        public async Task GetSuggestionsAsync_EmptyOrWhitespaceQuery_ReturnsEmptyList()
        {
            var res1 = await SuggestionService.GetSuggestionsAsync("", Enumerable.Empty<HistoryItem>(), Enumerable.Empty<BookmarkItem>());
            var res2 = await SuggestionService.GetSuggestionsAsync("   ", Enumerable.Empty<HistoryItem>(), Enumerable.Empty<BookmarkItem>());

            Assert.Empty(res1);
            Assert.Empty(res2);
        }

        [Fact]
        public async Task GetSuggestionsAsync_OpenTabs_ReturnsSwitchToTabSuggestions()
        {
            var tab1 = new BrowserTab { Title = "GitHub - Browy Project", Url = "https://github.com/tomlin7/Browy", IsActive = false };
            var tab2 = new BrowserTab { Title = "Active Google Tab", Url = "https://google.com", IsActive = true };
            var openTabs = new List<BrowserTab> { tab1, tab2 };

            var results = await SuggestionService.GetSuggestionsAsync(
                "browy",
                Enumerable.Empty<HistoryItem>(),
                Enumerable.Empty<BookmarkItem>(),
                openTabs: openTabs);

            Assert.NotEmpty(results);
            // First item is always standard search query
            Assert.Equal(SuggestionType.Search, results[0].Type);

            // Open tab suggestion
            var openTabSuggestion = results.FirstOrDefault(s => s.Type == SuggestionType.OpenTab);
            Assert.NotNull(openTabSuggestion);
            Assert.Equal("GitHub - Browy Project", openTabSuggestion.Title);
            Assert.Same(tab1, openTabSuggestion.AssociatedTab);
            Assert.Equal("Switch to Tab", openTabSuggestion.TypeBadge);
        }

        [Fact]
        public async Task GetSuggestionsAsync_AllSpaces_MatchesTabsAcrossSpaces()
        {
            var personalSpace = new BrowserSpace
            {
                Name = "Personal",
                IsActive = true
            };
            var currentTab = new BrowserTab { Title = "Active Search", Url = "https://bing.com", IsActive = true };
            personalSpace.Tabs.Add(currentTab);

            var workSpace = new BrowserSpace
            {
                Name = "Work",
                IsActive = false
            };
            var workTab = new BrowserTab { Title = "Internal Dashboard", Url = "https://work.corp/dashboard", IsActive = false };
            workSpace.Tabs.Add(workTab);

            var allSpaces = new List<BrowserSpace> { personalSpace, workSpace };

            var results = await SuggestionService.GetSuggestionsAsync(
                "dashboard",
                Enumerable.Empty<HistoryItem>(),
                Enumerable.Empty<BookmarkItem>(),
                allSpaces: allSpaces);

            var tabSuggestion = results.FirstOrDefault(s => s.Type == SuggestionType.OpenTab);
            Assert.NotNull(tabSuggestion);
            Assert.Same(workTab, tabSuggestion.AssociatedTab);
            Assert.Same(workSpace, tabSuggestion.AssociatedSpace);
        }

        [Fact]
        public async Task GetSuggestionsAsync_MatchesBookmarksAndHistory()
        {
            var bookmarks = new List<BookmarkItem>
            {
                new BookmarkItem("Microsoft Learn", "https://learn.microsoft.com")
            };
            var history = new List<HistoryItem>
            {
                new HistoryItem("Stack Overflow Question", "https://stackoverflow.com/questions/123")
            };

            var resultsLearn = await SuggestionService.GetSuggestionsAsync(
                "learn",
                history,
                bookmarks);

            Assert.Contains(resultsLearn, r => r.Type == SuggestionType.Bookmark && r.Title == "Microsoft Learn");

            var resultsStack = await SuggestionService.GetSuggestionsAsync(
                "stackoverflow",
                history,
                bookmarks);

            Assert.Contains(resultsStack, r => r.Type == SuggestionType.History && r.Title == "Stack Overflow Question");
        }

        [Fact]
        public async Task GetSuggestionsAsync_MathExpression_EvaluatesCalculatorFirst()
        {
            var results = await SuggestionService.GetSuggestionsAsync(
                "15 * 4",
                Enumerable.Empty<HistoryItem>(),
                Enumerable.Empty<BookmarkItem>());

            var calc = results.FirstOrDefault(r => r.Type == SuggestionType.Calculator);
            Assert.NotNull(calc);
            Assert.Equal("= 60", calc.Title);
            Assert.Equal("Calculation", calc.TypeBadge);
        }
    }
}
