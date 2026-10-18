using Browy.Models;
using Xunit;

namespace Browy.Tests
{
    public class SpaceTabMovementTests
    {
        [Fact]
        public void MoveTabBetweenSpaces_UpdatesCollections()
        {
            var space1 = new BrowserSpace { Name = "Work", Id = "space-work" };
            var space2 = new BrowserSpace { Name = "Personal", Id = "space-personal" };

            var tab1 = new BrowserTab { Title = "Project Roadmap", Url = "https://example.com/roadmap" };
            var tab2 = new BrowserTab { Title = "Music Stream", Url = "https://example.com/music" };

            space1.Tabs.Add(tab1);
            space2.Tabs.Add(tab2);

            Assert.Single(space1.Tabs);
            Assert.Single(space2.Tabs);

            // Simulate moving tab1 from space1 to space2
            space1.Tabs.Remove(tab1);
            space2.Tabs.Add(tab1);

            Assert.Empty(space1.Tabs);
            Assert.Equal(2, space2.Tabs.Count);
            Assert.Contains(tab1, space2.Tabs);
            Assert.Contains(tab2, space2.Tabs);
        }
    }
}
