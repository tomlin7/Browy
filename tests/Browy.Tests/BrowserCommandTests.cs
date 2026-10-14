using Browy.Models;
using Xunit;

namespace Browy.Tests
{
    public class BrowserCommandTests
    {
        [Fact]
        public void FuzzyScore_ExactMatchScoresHighest()
        {
            var cmd = new BrowserCommand("Toggle Split View", "View", "\uE7FB", "Ctrl+\\", "Dual-pane", _ => {});

            int exactScore = cmd.GetFuzzyScore("Toggle Split View");
            int prefixScore = cmd.GetFuzzyScore("Toggle");
            int subseqScore = cmd.GetFuzzyScore("spl");

            Assert.Equal(1000, exactScore);
            Assert.True(exactScore > prefixScore);
            Assert.True(prefixScore > subseqScore);
            Assert.True(subseqScore > 0);
        }

        [Fact]
        public void FuzzyScore_SubsequenceMatchesAbbreviatedQueries()
        {
            var cmd = new BrowserCommand("Developer Tools", "Tools", "\uEC7A", "F12", "Inspect DOM", _ => {});

            // "dev" is prefix
            Assert.True(cmd.GetFuzzyScore("dev") >= 500);

            // "dt" matches [D]eveloper [T]ools as subsequence
            Assert.True(cmd.GetFuzzyScore("dt") > 0);
            Assert.True(cmd.Matches("dt"));
        }

        [Fact]
        public void FuzzyScore_NonMatchingReturnsZero()
        {
            var cmd = new BrowserCommand("Open Settings", "Settings", "\uE713", "Ctrl+,", "Preferences", _ => {});

            Assert.Equal(0, cmd.GetFuzzyScore("xyznotfound"));
            Assert.False(cmd.Matches("xyznotfound"));
        }
    }
}
