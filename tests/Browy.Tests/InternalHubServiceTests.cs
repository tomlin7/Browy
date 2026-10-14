using System.Collections.Generic;
using Browy.Models;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class InternalHubServiceTests
    {
        [Fact]
        public void GetHistoryHtml_GeneratesValidHtmlWithEmptyState()
        {
            var emptyHistory = new List<HistoryItem>();
            string html = InternalHubService.GetHistoryHtml(emptyHistory);

            Assert.Contains("<!DOCTYPE html>", html);
            Assert.Contains("History - Browy", html);
            Assert.Contains("No browsing history yet", html);
        }

        [Fact]
        public void GetHistoryHtml_EscapesXssInTitleAndUrl()
        {
            var history = new List<HistoryItem>
            {
                new HistoryItem
                {
                    Title = "<script>alert('xss')</script>",
                    Url = "https://example.com/test?a=<test>&b=\"quote\"",
                    VisitedAt = System.DateTime.UtcNow
                }
            };

            string html = InternalHubService.GetHistoryHtml(history);

            Assert.DoesNotContain("<script>alert('xss')</script>", html);
            Assert.Contains("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", html);
        }

        [Fact]
        public void GetBookmarksHtml_GeneratesCardsForBookmarks()
        {
            var bookmarks = new List<BookmarkItem>
            {
                new BookmarkItem
                {
                    Title = "GitHub",
                    Url = "https://github.com"
                },
                new BookmarkItem
                {
                    Title = "Reddit",
                    Url = "https://reddit.com"
                }
            };

            string html = InternalHubService.GetBookmarksHtml(bookmarks);

            Assert.Contains("Bookmarks - Browy", html);
            Assert.Contains("GitHub", html);
            Assert.Contains("https://github.com", html);
            Assert.Contains("Reddit", html);
        }

        [Fact]
        public void GetDownloadsHtml_GeneratesValidHtmlWithEmptyState()
        {
            var emptyDownloads = new List<DownloadItem>();
            string html = InternalHubService.GetDownloadsHtml(emptyDownloads);

            Assert.Contains("Downloads - Browy", html);
            Assert.Contains("No downloads yet", html);
        }

        [Fact]
        public void GetDownloadsHtml_RendersDownloadItems()
        {
            var downloads = new List<DownloadItem>
            {
                new DownloadItem
                {
                    FileName = "setup.exe",
                    StatusText = "Completed",
                    ResultFilePath = @"C:\Downloads\setup.exe"
                }
            };

            string html = InternalHubService.GetDownloadsHtml(downloads);

            Assert.Contains("setup.exe", html);
            Assert.Contains("Completed", html);
            Assert.Contains("Open", html);
        }
    }
}
