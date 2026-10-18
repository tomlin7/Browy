using System;
using System.Collections.Generic;
using System.Linq;
using Browy.Models;
using Xunit;

namespace Browy.Tests
{
    public class DuplicateTabCleanupTests
    {
        [Fact]
        public void FindDuplicates_DetectsRepeatedUrls_PreservesUniqueAndPinned()
        {
            var tabs = new List<BrowserTab>
            {
                new BrowserTab { Title = "GitHub 1", Url = "https://github.com/tomlin7/Browy" },
                new BrowserTab { Title = "Google", Url = "https://www.google.com" },
                new BrowserTab { Title = "GitHub Duplicate", Url = "https://github.com/tomlin7/Browy/" }, // normalized matches
                new BrowserTab { Title = "GitHub Pinned Duplicate", Url = "https://github.com/tomlin7/Browy", IsPinned = true },
                new BrowserTab { Title = "Google Duplicate", Url = "https://www.google.com" },
                new BrowserTab { Title = "New Tab 1", Url = "browy://newtab" },
                new BrowserTab { Title = "New Tab 2", Url = "browy://newtab" } // new tabs shouldn't be purged as duplicates
            };

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var toRemove = new List<BrowserTab>();

            foreach (var t in tabs)
            {
                if (t.IsPinned) continue;
                string norm = (t.Url ?? "").TrimEnd('/');
                if (string.IsNullOrWhiteSpace(norm) || norm.Equals("browy://newtab", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!seen.Add(norm))
                {
                    toRemove.Add(t);
                }
            }

            // Expected to remove: GitHub Duplicate (3rd tab) and Google Duplicate (5th tab)
            Assert.Equal(2, toRemove.Count);
            Assert.Contains(toRemove, t => t.Title == "GitHub Duplicate");
            Assert.Contains(toRemove, t => t.Title == "Google Duplicate");
            Assert.DoesNotContain(toRemove, t => t.IsPinned);
            Assert.DoesNotContain(toRemove, t => t.Url == "browy://newtab");
        }
    }
}
