using System;
using System.Collections.Generic;
using Browy.Models;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class FeaturesBatch4Tests
    {
        [Theory]
        [InlineData("extensions", "browy://extensions")]
        [InlineData("browy://extensions", "browy://extensions")]
        [InlineData("about:extensions", "browy://extensions")]
        [InlineData("version", "browy://version")]
        [InlineData("about", "browy://version")]
        [InlineData("browy://version", "browy://version")]
        [InlineData("browy://about", "browy://version")]
        [InlineData("about:version", "browy://version")]
        [InlineData("shortcuts", "browy://shortcuts")]
        [InlineData("browy://shortcuts", "browy://shortcuts")]
        [InlineData("about:shortcuts", "browy://shortcuts")]
        public void NavigationHelper_Resolves_NewInternalPages(string input, string expected)
        {
            string resolved = NavigationHelper.ResolveInputToUrl(input);
            Assert.Equal(expected, resolved);
        }

        [Fact]
        public void NavigationHelper_Resolves_CaseInsensitiveAndTrimmed()
        {
            Assert.Equal("browy://version", NavigationHelper.ResolveInputToUrl("  VERSION  "));
            Assert.Equal("browy://version", NavigationHelper.ResolveInputToUrl("  browy://ABOUT  "));
            Assert.Equal("browy://shortcuts", NavigationHelper.ResolveInputToUrl("   SHORTCUTS   "));
            Assert.Equal("browy://extensions", NavigationHelper.ResolveInputToUrl("   Extensions "));
        }

        [Fact]
        public void InternalHubService_GetAboutHtml_ContainsSystemDiagnosticsAndActions()
        {
            string html = InternalHubService.GetAboutHtml(tabsCount: 4, spacesCount: 2);

            Assert.Contains("About Browy", html);
            Assert.Contains("v0.3.0", html);
            Assert.Contains("Operating System & Shell", html);
            Assert.Contains("Runtime & Web Engine", html);
            Assert.Contains("Memory & Performance", html);
            Assert.Contains("Process Working Set", html);
            Assert.Contains("GC Heap Memory", html);
            Assert.Contains("Open Tabs Count", html);
            Assert.Contains("Active Spaces Count", html);
            Assert.Contains("copyDiagnostics", html);
            Assert.Contains("browy://settings", html);
            Assert.Contains("browy://shortcuts", html);
        }

        [Fact]
        public void InternalHubService_GetShortcutsHtml_ContainsAllCategoriesAndKeyShortcuts()
        {
            string html = InternalHubService.GetShortcutsHtml();

            Assert.Contains("Keyboard Shortcuts", html);
            Assert.Contains("Tabs &amp; Windows", html);
            Assert.Contains("Navigation &amp; Webpage", html);
            Assert.Contains("Views, Spaces &amp; Tools", html);
            Assert.Contains("Mouse Gestures", html);
            Assert.Contains("scSearch", html);
            Assert.Contains("filterShortcuts", html);
            Assert.Contains("Ctrl", html);
            Assert.Contains("Shift", html);
            Assert.Contains("Middle Click", html);
        }

        [Fact]
        public void InternalHubService_GetBookmarksHtml_ContainsExportAndImportActions()
        {
            var bookmarks = new List<BookmarkItem>
            {
                new("Example", "https://example.com")
            };
            string html = InternalHubService.GetBookmarksHtml(bookmarks);

            Assert.Contains("Export HTML", html);
            Assert.Contains("Import HTML", html);
            Assert.Contains("export_bookmarks_html", html);
            Assert.Contains("import_bookmarks_html", html);
        }

        [Fact]
        public void InternalHubService_GetHistoryHtml_ContainsExportJsonAction()
        {
            var history = new List<HistoryItem>
            {
                new("Example", "https://example.com")
            };
            string html = InternalHubService.GetHistoryHtml(history);

            Assert.Contains("Export JSON", html);
            Assert.Contains("export_history_json", html);
        }

        [Fact]
        public void InternalHubService_GetDownloadsHtml_ContainsStatusFilterBar()
        {
            var downloads = new List<DownloadItem>
            {
                new() { FileName = "test.zip", TotalBytes = 1000, BytesReceived = 500 }
            };
            string html = InternalHubService.GetDownloadsHtml(downloads);

            Assert.Contains("filter-bar", html);
            Assert.Contains("In Progress", html);
            Assert.Contains("Completed", html);
            Assert.Contains("Paused / Other", html);
            Assert.Contains("data-status=", html);
            Assert.Contains("setStatusFilter", html);
        }

        [Fact]
        public void BrowserSettings_AccentColorAndThemeMode_DefaultsAndPersistence()
        {
            var settings = BrowserSettings.Current;
            Assert.NotNull(settings.AccentColorHex);
            Assert.StartsWith("#", settings.AccentColorHex);
            Assert.Equal("Dark", settings.ThemeMode);

            BrowserSettings.SetAccentColor("#10B981");
            Assert.Equal("#10B981", BrowserSettings.Current.AccentColorHex);

            BrowserSettings.SetThemeMode("System");
            Assert.Equal("System", BrowserSettings.Current.ThemeMode);

            // Restore defaults
            BrowserSettings.SetAccentColor("#3B82F6");
            BrowserSettings.SetThemeMode("Dark");
            Assert.Equal("#3B82F6", BrowserSettings.Current.AccentColorHex);
            Assert.Equal("Dark", BrowserSettings.Current.ThemeMode);
        }

        [Fact]
        public void SettingsPageService_GetSettingsHtml_ContainsThemeModeAndAccentPalette()
        {
            string html = SettingsPageService.GetSettingsHtml();

            Assert.Contains("Theme Mode", html);
            Assert.Contains("themeModeSelect", html);
            Assert.Contains("Accent Color &amp; Space Palette", html);
            Assert.Contains("accent-swatch", html);
            Assert.Contains("Electric Blue", html);
            Assert.Contains("Arc Purple", html);
            Assert.Contains("Emerald Green", html);
            Assert.Contains("Sunset Amber", html);
            Assert.Contains("Active Tab Memory Optimizer", html);
            Assert.Contains("snooze_inactive_tabs", html);
            Assert.Contains("wake_all_tabs", html);
            Assert.Contains("browy://version", html);
        }

        [Fact]
        public void NewTabPageService_GetNewTabHtml_IncludesConfiguredAccentColor()
        {
            BrowserSettings.SetAccentColor("#8B5CF6");
            try
            {
                string html = NewTabPageService.GetNewTabHtml();
                Assert.Contains("--accent: #8B5CF6;", html);
            }
            finally
            {
                BrowserSettings.SetAccentColor("#3B82F6");
            }
        }
    }
}
