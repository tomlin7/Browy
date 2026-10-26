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
        public string? GroupName { get; set; }
        public string? GroupColorHex { get; set; }
    }

    public class SpaceSessionData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = "Personal";
        public string IconGlyph { get; set; } = "\uE80F";
        public string AccentColorHex { get; set; } = "#6366F1";
        public bool IsActive { get; set; } = false;
        public bool IsIsolated { get; set; } = false;
        public int ActiveTabIndex { get; set; } = 0;
        public List<SessionTabData> Tabs { get; set; } = new();
    }

    public class BrowserSessionData
    {
        public List<SpaceSessionData> Spaces { get; set; } = new();
        public int ActiveSpaceIndex { get; set; } = 0;

        // Legacy / active space tabs fallback
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

        public static void SaveSpacesSession(
            IEnumerable<BrowserSpace> spaces,
            BrowserSpace? activeSpace,
            BrowserTab? activeTab,
            IEnumerable<BrowserTab>? activeTabs = null)
        {
            lock (_lock)
            {
                try
                {
                    var spaceList = spaces.ToList();
                    if (spaceList.Count == 0)
                    {
                        ClearSession();
                        return;
                    }

                    var session = new BrowserSessionData();
                    int activeSpaceIdx = 0;

                    for (int s = 0; s < spaceList.Count; s++)
                    {
                        var space = spaceList[s];
                        bool isSpaceActive = (space == activeSpace) || space.IsActive;
                        if (isSpaceActive) activeSpaceIdx = s;

                        var spaceData = new SpaceSessionData
                        {
                            Id = space.Id,
                            Name = space.Name,
                            IconGlyph = space.IconGlyph,
                            AccentColorHex = space.AccentColorHex,
                            IsActive = isSpaceActive,
                            IsIsolated = space.IsIsolated,
                            ActiveTabIndex = space.ActiveTabIndex
                        };

                        var tabList = (isSpaceActive && activeTabs != null) ? activeTabs.ToList() : space.Tabs.ToList();
                        for (int t = 0; t < tabList.Count; t++)
                        {
                            var tab = tabList[t];
                            string url = tab.IsReaderModeActive && !string.IsNullOrEmpty(tab.OriginalUrlBeforeReader)
                                ? tab.OriginalUrlBeforeReader
                                : tab.Url;

                            if (string.IsNullOrWhiteSpace(url) || url.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
                            {
                                url = "browy://newtab";
                            }

                            string title = tab.Title;
                            if (string.IsNullOrWhiteSpace(title) || title == "Navigation error" || title == "Loading...")
                            {
                                title = NavigationHelper.IsInternalUrl(url) 
                                    ? NavigationHelper.GetInternalPageTitle(url) 
                                    : NavigationHelper.ExtractDomain(url);
                                if (string.IsNullOrWhiteSpace(title)) title = "New Tab";
                            }

                            bool isTabActive = (tab == activeTab) || tab.IsActive;
                            if (isTabActive) spaceData.ActiveTabIndex = t;

                            spaceData.Tabs.Add(new SessionTabData
                            {
                                Url = url,
                                Title = title,
                                IsPinned = tab.IsPinned,
                                IsActive = isTabActive,
                                GroupName = tab.GroupName,
                                GroupColorHex = tab.GroupColorHex
                            });
                        }

                        if (spaceData.Tabs.Count == 0)
                        {
                            spaceData.Tabs.Add(new SessionTabData
                            {
                                Url = "browy://newtab",
                                Title = "New Tab",
                                IsActive = true
                            });
                        }

                        session.Spaces.Add(spaceData);
                    }

                    session.ActiveSpaceIndex = activeSpaceIdx;

                    // Fallback legacy Tabs from active space
                    if (session.Spaces.Count > activeSpaceIdx)
                    {
                        session.Tabs = session.Spaces[activeSpaceIdx].Tabs;
                        session.ActiveTabIndex = session.Spaces[activeSpaceIdx].ActiveTabIndex;
                    }

                    string dir = Path.GetDirectoryName(SessionFilePath)!;
                    Directory.CreateDirectory(dir);
                    string json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
                    string tempFile = $"{SessionFilePath}.tmp";
                    File.WriteAllText(tempFile, json);
                    File.Move(tempFile, SessionFilePath, overwrite: true);
                }
                catch (Exception ex)
                {
                    try { File.AppendAllText(@"E:\browy\crash.log", $"[{DateTime.Now:O}] SaveSpacesSession error: {ex}\n"); } catch { }
                }
            }
        }

        public static void SaveSession(IEnumerable<BrowserTab> tabs, BrowserTab? activeTab)
        {
            var defaultSpace = new BrowserSpace
            {
                Name = "Personal",
                IconGlyph = "\uE80F",
                AccentColorHex = "#6366F1",
                IsActive = true
            };
            foreach (var t in tabs) defaultSpace.Tabs.Add(t);
            SaveSpacesSession(new[] { defaultSpace }, defaultSpace, activeTab);
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
                    if (session != null)
                    {
                        // Backwards compatibility migration
                        if ((session.Spaces == null || session.Spaces.Count == 0) && session.Tabs != null && session.Tabs.Count > 0)
                        {
                            session.Spaces = new List<SpaceSessionData>
                            {
                                new SpaceSessionData
                                {
                                    Id = Guid.NewGuid().ToString("N"),
                                    Name = "Personal",
                                    IconGlyph = "\uE80F",
                                    AccentColorHex = "#6366F1",
                                    IsActive = true,
                                    ActiveTabIndex = session.ActiveTabIndex,
                                    Tabs = session.Tabs
                                }
                            };
                            session.ActiveSpaceIndex = 0;
                        }

                        if (session.Spaces != null && session.Spaces.Count > 0)
                        {
                            for (int i = 0; i < session.Spaces.Count; i++)
                            {
                                var space = session.Spaces[i];
                                if (space.Tabs == null || space.Tabs.Count == 0)
                                {
                                    space.Tabs = new List<SessionTabData>
                                    {
                                        new SessionTabData { Url = "browy://newtab", Title = "New Tab", IsActive = true }
                                    };
                                }
                                space.ActiveTabIndex = Math.Clamp(space.ActiveTabIndex, 0, space.Tabs.Count - 1);
                            }
                            session.ActiveSpaceIndex = Math.Clamp(session.ActiveSpaceIndex, 0, session.Spaces.Count - 1);
                            return session;
                        }
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
