using System.Collections.Generic;
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
    }
}
