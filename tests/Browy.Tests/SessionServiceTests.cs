using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class SessionServiceTests
    {
        [Fact]
        public void SpaceSessionData_SerializesAndDeserializesCorrectly()
        {
            var session = new BrowserSessionData
            {
                ActiveSpaceIndex = 1,
                Spaces = new List<SpaceSessionData>
                {
                    new SpaceSessionData
                    {
                        Id = "space-1",
                        Name = "Personal",
                        AccentColorHex = "#6366F1",
                        IsActive = false,
                        ActiveTabIndex = 0,
                        Tabs = new List<SessionTabData>
                        {
                            new SessionTabData { Url = "https://github.com", Title = "GitHub", IsActive = true }
                        }
                    },
                    new SpaceSessionData
                    {
                        Id = "space-2",
                        Name = "Work",
                        AccentColorHex = "#EC4899",
                        IsActive = true,
                        ActiveTabIndex = 1,
                        Tabs = new List<SessionTabData>
                        {
                            new SessionTabData { Url = "https://slack.com", Title = "Slack", IsActive = false },
                            new SessionTabData { Url = "https://azure.com", Title = "Azure Portal", IsActive = true }
                        }
                    }
                }
            };

            string json = JsonSerializer.Serialize(session);
            Assert.False(string.IsNullOrWhiteSpace(json));

            var restored = JsonSerializer.Deserialize<BrowserSessionData>(json);
            Assert.NotNull(restored);
            Assert.Equal(1, restored.ActiveSpaceIndex);
            Assert.Equal(2, restored.Spaces.Count);
            Assert.Equal("Work", restored.Spaces[1].Name);
            Assert.Equal(2, restored.Spaces[1].Tabs.Count);
            Assert.Equal("https://azure.com", restored.Spaces[1].Tabs[1].Url);
        }

        [Fact]
        public void BrowserSessionData_HandlesLegacyTabsGracefully()
        {
            string legacyJson = @"{
                ""Tabs"": [
                    { ""Url"": ""https://duckduckgo.com"", ""Title"": ""DuckDuckGo"", ""IsActive"": true }
                ],
                ""ActiveTabIndex"": 0
            }";

            var restored = JsonSerializer.Deserialize<BrowserSessionData>(legacyJson);
            Assert.NotNull(restored);
            Assert.Single(restored.Tabs);
            Assert.Equal("https://duckduckgo.com", restored.Tabs[0].Url);
            Assert.Empty(restored.Spaces);
        }

        [Fact]
        public void SessionTabData_SerializesAndRestoresGroupInfo()
        {
            var tab = new SessionTabData
            {
                Url = "https://dotnet.microsoft.com",
                Title = ".NET Documentation",
                IsActive = true,
                GroupName = "Dev",
                GroupColorHex = "#10B981"
            };

            string json = JsonSerializer.Serialize(tab);
            var restored = JsonSerializer.Deserialize<SessionTabData>(json);

            Assert.NotNull(restored);
            Assert.Equal("Dev", restored.GroupName);
            Assert.Equal("#10B981", restored.GroupColorHex);
        }

        [Fact]
        public void SaveSpacesSession_WithActiveTabsOverride_PersistsAndSanitizesTitles()
        {
            var space = new Browy.Models.BrowserSpace
            {
                Id = "test-space-1",
                Name = "Work",
                IsActive = true
            };

            var activeTabs = new List<Browy.Models.BrowserTab>
            {
                new Browy.Models.BrowserTab
                {
                    Url = "https://github.com/torvalds/linux",
                    Title = "Navigation error",
                    IsActive = true
                },
                new Browy.Models.BrowserTab
                {
                    Url = "https://news.ycombinator.com",
                    Title = "Loading...",
                    IsActive = false
                },
                new Browy.Models.BrowserTab
                {
                    Url = "browy://settings",
                    Title = "Navigation error",
                    IsActive = false
                },
                new Browy.Models.BrowserTab
                {
                    Url = "browy://newtab",
                    Title = "Navigation error",
                    IsActive = false
                }
            };

            try
            {
                SessionService.SaveSpacesSession(new[] { space }, space, activeTabs[0], activeTabs);
                var loaded = SessionService.LoadSession();

                Assert.NotNull(loaded);
                Assert.Single(loaded.Spaces);
                Assert.Equal(4, loaded.Spaces[0].Tabs.Count);

                // Titles should be sanitized away from error/loading states
                Assert.Equal("github.com", loaded.Spaces[0].Tabs[0].Title);
                Assert.Equal("news.ycombinator.com", loaded.Spaces[0].Tabs[1].Title);
                Assert.Equal("Settings - Browy", loaded.Spaces[0].Tabs[2].Title);
                Assert.Equal("New Tab", loaded.Spaces[0].Tabs[3].Title);
            }
            finally
            {
                SessionService.ClearSession();
            }
        }

        [Fact]
        public void LoadSession_SanitizesEmptySpacesAndOutOfBoundsIndices()
        {
            var rawSession = new BrowserSessionData
            {
                ActiveSpaceIndex = 99,
                Spaces = new List<SpaceSessionData>
                {
                    new SpaceSessionData
                    {
                        Id = "empty-space",
                        Name = "Empty Space",
                        ActiveTabIndex = 10,
                        Tabs = new List<SessionTabData>()
                    }
                }
            };

            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Browy");
                Directory.CreateDirectory(dir);
                string sessionPath = Path.Combine(dir, "session.json");
                File.WriteAllText(sessionPath, System.Text.Json.JsonSerializer.Serialize(rawSession));

                var loaded = SessionService.LoadSession();

                Assert.NotNull(loaded);
                Assert.Equal(0, loaded.ActiveSpaceIndex);
                Assert.Single(loaded.Spaces);
                Assert.Single(loaded.Spaces[0].Tabs);
                Assert.Equal("browy://newtab", loaded.Spaces[0].Tabs[0].Url);
                Assert.Equal(0, loaded.Spaces[0].ActiveTabIndex);
            }
            finally
            {
                SessionService.ClearSession();
            }
        }

        [Fact]
        public void SaveSpacesSession_PreservesOriginalUrlBeforeReader()
        {
            var space = new Browy.Models.BrowserSpace
            {
                Id = "test-reader-space",
                Name = "Personal",
                IsActive = true
            };

            var tab = new Browy.Models.BrowserTab
            {
                Url = "browy://reader",
                OriginalUrlBeforeReader = "https://blog.rust-lang.org/2026/article",
                IsReaderModeActive = true,
                Title = "Rust Blog"
            };

            try
            {
                SessionService.SaveSpacesSession(new[] { space }, space, tab, new[] { tab });
                var loaded = SessionService.LoadSession();

                Assert.NotNull(loaded);
                Assert.Single(loaded.Spaces[0].Tabs);
                Assert.Equal("https://blog.rust-lang.org/2026/article", loaded.Spaces[0].Tabs[0].Url);
            }
            finally
            {
                SessionService.ClearSession();
            }
        }
    }
}
