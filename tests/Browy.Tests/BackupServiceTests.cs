using System.Collections.Generic;
using Browy.Models;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class BackupServiceTests
    {
        [Fact]
        public void BookmarkService_ExportAndImportHtml_RoundtripsSuccessfully()
        {
            var original = new List<BookmarkItem>
            {
                new("GitHub", "https://github.com"),
                new("MDN Web Docs", "https://developer.mozilla.org/en-US/"),
                new("Rust Lang", "https://www.rust-lang.org")
            };

            string html = BookmarkService.ExportToHtml(original);

            Assert.Contains("<!DOCTYPE NETSCAPE-Bookmark-file-1>", html);
            Assert.Contains("https://github.com", html);
            Assert.Contains("GitHub", html);

            var imported = BookmarkService.ImportFromHtml(html);

            Assert.Equal(3, imported.Count);
            Assert.Equal("GitHub", imported[0].Title);
            Assert.Equal("https://github.com", imported[0].Url);
            Assert.Equal("https://www.rust-lang.org", imported[2].Url);
        }

        [Fact]
        public void BackupService_CreateAndRestoreBackupBundle_PreservesState()
        {
            var bookmarks = new List<BookmarkItem>
            {
                new("OpenAI", "https://openai.com")
            };
            var settings = new BrowserSettingsData
            {
                SearchEngineName = "DuckDuckGo",
                SearchEngineUrl = "https://duckduckgo.com/?q="
            };
            var spaces = new List<BrowserSpace>
            {
                new()
                {
                    Name = "Dev",
                    AccentColorHex = "#10B981",
                    Tabs = new System.Collections.ObjectModel.ObservableCollection<BrowserTab>
                    {
                        new() { Title = "GitHub PR", Url = "https://github.com/pulls" }
                    }
                }
            };

            string json = BackupService.CreateBackupJson(bookmarks, settings, spaces, spaces[0], null);
            Assert.False(string.IsNullOrWhiteSpace(json));

            var restored = BackupService.RestoreBackupFromJson(json);
            Assert.NotNull(restored);
            Assert.Equal("DuckDuckGo", restored.Settings.SearchEngineName);
            Assert.Single(restored.Bookmarks);
            Assert.Equal("OpenAI", restored.Bookmarks[0].Title);
            Assert.Single(restored.Session.Spaces);
            Assert.Equal("Dev", restored.Session.Spaces[0].Name);
        }
    }
}
