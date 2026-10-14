using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Browy.Models;

namespace Browy.Services
{
    public class BrowyBackupBundle
    {
        public string Version { get; set; } = "1.0";
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public BrowserSettingsData Settings { get; set; } = new();
        public List<BookmarkDto> Bookmarks { get; set; } = new();
        public BrowserSessionData Session { get; set; } = new();
    }

    public static class BackupService
    {
        public static string CreateBackupJson(
            IEnumerable<BookmarkItem> bookmarks,
            BrowserSettingsData settings,
            IEnumerable<BrowserSpace> spaces,
            BrowserSpace? activeSpace,
            BrowserTab? activeTab)
        {
            var bundle = new BrowyBackupBundle
            {
                Settings = settings,
                Bookmarks = bookmarks.Select(b => new BookmarkDto { Title = b.Title, Url = b.Url }).ToList(),
                Session = new BrowserSessionData
                {
                    Spaces = spaces.Select(s => new SpaceSessionData
                    {
                        Id = s.Id,
                        Name = s.Name,
                        IconGlyph = s.IconGlyph,
                        AccentColorHex = s.AccentColorHex,
                        IsActive = (s == activeSpace),
                        ActiveTabIndex = s.ActiveTabIndex,
                        Tabs = s.Tabs.Select(t => new SessionTabData
                        {
                            Title = t.Title,
                            Url = t.Url,
                            IsPinned = t.IsPinned,
                            IsActive = (t == activeTab)
                        }).ToList()
                    }).ToList()
                }
            };

            return JsonSerializer.Serialize(bundle, new JsonSerializerOptions { WriteIndented = true });
        }

        public static BrowyBackupBundle? RestoreBackupFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<BrowyBackupBundle>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
