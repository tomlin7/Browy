using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Browy.Models;

namespace Browy.Services
{
    public class SessionTabData
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsPinned { get; set; } = false;
        public bool IsActive { get; set; } = false;
    }

    public class BrowserSessionData
    {
        public List<SessionTabData> Tabs { get; set; } = new();
        public int ActiveTabIndex { get; set; } = 0;
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

    public static class SessionService
    {
        private static readonly string SessionFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Browy",
            "session.json");

        private static readonly object _lock = new();

        public static void SaveSession(IEnumerable<BrowserTab> tabs, BrowserTab? activeTab)
        {
            lock (_lock)
            {
                try
                {
                    var tabList = tabs.ToList();
                    if (tabList.Count == 0)
                    {
                        ClearSession();
                        return;
                    }

                    var session = new BrowserSessionData();
                    int activeIndex = 0;

                    for (int i = 0; i < tabList.Count; i++)
                    {
                        var tab = tabList[i];
                        string url = tab.IsReaderModeActive && !string.IsNullOrEmpty(tab.OriginalUrlBeforeReader)
                            ? tab.OriginalUrlBeforeReader
                            : tab.Url;

                        // Skip temporary or blank error tabs
                        if (string.IsNullOrWhiteSpace(url) || url.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
                        {
                            url = "browy://newtab";
                        }

                        bool isActive = tab == activeTab || tab.IsActive;
                        if (isActive)
                        {
                            activeIndex = i;
                        }

                        session.Tabs.Add(new SessionTabData
                        {
                            Url = url,
                            Title = string.IsNullOrWhiteSpace(tab.Title) ? "New Tab" : tab.Title,
                            IsPinned = tab.IsPinned,
                            IsActive = isActive
                        });
                    }

                    session.ActiveTabIndex = activeIndex;

                    string dir = Path.GetDirectoryName(SessionFilePath)!;
                    Directory.CreateDirectory(dir);
                    string json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(SessionFilePath, json);
                }
                catch (Exception ex)
                {
                    try { File.AppendAllText(@"E:\browy\crash.log", $"[{DateTime.Now:O}] SaveSession error: {ex}\n"); } catch { }
                }
            }
        }

        public static BrowserSessionData? LoadSession()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(SessionFilePath)) return null;

                    string json = File.ReadAllText(SessionFilePath);
                    var session = JsonSerializer.Deserialize<BrowserSessionData>(json);
                    if (session != null && session.Tabs != null && session.Tabs.Count > 0)
                    {
                        return session;
                    }
                }
                catch (Exception ex)
                {
                    try { File.AppendAllText(@"E:\browy\crash.log", $"[{DateTime.Now:O}] LoadSession error: {ex}\n"); } catch { }
                }
                return null;
            }
        }

        public static void ClearSession()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(SessionFilePath))
                    {
                        File.Delete(SessionFilePath);
                    }
                }
                catch { }
            }
        }
    }
}
