using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Browy.Models;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class NotificationAndDownloadTests
    {
        [Fact]
        public void NotificationService_OriginPermissions_WorkCorrectly()
        {
            string origin = "https://example.com";
            Assert.True(NotificationService.IsOriginAllowed(origin));

            NotificationService.SetOriginPermission(origin, false);
            Assert.False(NotificationService.IsOriginAllowed(origin));

            NotificationService.SetOriginPermission(origin, true);
            Assert.True(NotificationService.IsOriginAllowed(origin));
        }

        [Fact]
        public void DownloadItem_FormatBytes_FormatsCorrectly()
        {
            Assert.Equal("500 B", DownloadItem.FormatBytes(500));
            Assert.Equal("1.5 KB", DownloadItem.FormatBytes(1536));
            Assert.Equal("10.0 MB", DownloadItem.FormatBytes(10 * 1024 * 1024));
            Assert.Equal("2.50 GB", DownloadItem.FormatBytes((long)(2.5 * 1024 * 1024 * 1024)));
        }

        [Fact]
        public void DownloadItem_DefaultState_InitializesProperties()
        {
            var item = new DownloadItem
            {
                FileName = "test.zip",
                ResultFilePath = @"C:\Downloads\test.zip",
                TotalBytes = 1000,
                BytesReceived = 500
            };

            Assert.Equal("test.zip", item.FileName);
            Assert.Equal("\uF012", item.IconGlyph);
            Assert.False(item.IsCompleted);
        }

        [Fact]
        public void InternalHubService_GetDownloadsHtml_RendersItems()
        {
            var downloads = new ObservableCollection<DownloadItem>
            {
                new DownloadItem
                {
                    FileName = "installer.exe",
                    ResultFilePath = @"C:\Downloads\installer.exe",
                    StatusText = "Downloading...",
                    SpeedText = "1.2 MB/s",
                    EtaText = "15s remaining",
                    PercentComplete = 60
                }
            };

            string html = InternalHubService.GetDownloadsHtml(downloads);
            Assert.Contains("installer.exe", html);
            Assert.Contains("1.2 MB/s", html);
            Assert.Contains("15s remaining", html);
            Assert.Contains("Downloads", html);
        }

        [Fact]
        public void InternalHubService_GetHistoryHtml_RendersBucketsAndGroupControls()
        {
            var history = new ObservableCollection<HistoryItem>
            {
                new HistoryItem { Title = "GitHub", Url = "https://github.com", VisitedAt = DateTime.Now },
                new HistoryItem { Title = "Example", Url = "https://example.com/page", VisitedAt = DateTime.Now.AddDays(-2) }
            };

            string html = InternalHubService.GetHistoryHtml(history);
            Assert.Contains("GitHub", html);
            Assert.Contains("Example", html);
            Assert.Contains("Group by Domain", html);
            Assert.Contains("Delete Selected", html);
            Assert.Contains("Clear Browsing Data", html);
        }

        [Fact]
        public void SplitViewMode_Values_AreValid()
        {
            Assert.Equal(0, (int)SplitViewMode.Single);
            Assert.Equal(1, (int)SplitViewMode.DualVertical);
            Assert.Equal(2, (int)SplitViewMode.DualHorizontal);
            Assert.Equal(3, (int)SplitViewMode.Quad);
        }
    }
}
