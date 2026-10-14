using System.Collections.Generic;
using System.Linq;
using Browy.Models;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class BookmarkServiceTests
    {
        [Fact]
        public void IsUrlBookmarked_FindsTopLevelAndNestedItems()
        {
            var bookmarks = new List<BookmarkItem>
            {
                new("GitHub", "https://github.com"),
                new("Tools", isFolder: true)
            };

            var subFolder = new BookmarkItem("Dev", isFolder: true);
            subFolder.Children.Add(new BookmarkItem("MDN", "https://developer.mozilla.org"));
            bookmarks[1].Children.Add(subFolder);

            Assert.True(BookmarkService.IsUrlBookmarked(bookmarks, "https://github.com"));
            Assert.True(BookmarkService.IsUrlBookmarked(bookmarks, "https://developer.mozilla.org/"));
            Assert.False(BookmarkService.IsUrlBookmarked(bookmarks, "https://bing.com"));
        }

        [Fact]
        public void HierarchicalConversion_PreservesFolderStructure()
        {
            var root = new BookmarkItem("RootFolder", isFolder: true);
            root.Children.Add(new BookmarkItem("Nested1", "https://example.com/1"));
            root.Children.Add(new BookmarkItem("Nested2", "https://example.com/2"));

            var dto = BookmarkService.ModelToDto(root);
            Assert.True(dto.IsFolder);
            Assert.Equal(2, dto.Children?.Count);

            var reconstructed = BookmarkService.DtoToModel(dto);
            Assert.True(reconstructed.IsFolder);
            Assert.Equal(2, reconstructed.Children.Count);
            Assert.Equal("Nested1", reconstructed.Children[0].Title);
            Assert.Equal("https://example.com/1", reconstructed.Children[0].Url);
        }

        [Fact]
        public void ExportToHtml_IncludesNestedFoldersAndLinks()
        {
            var bookmarks = new List<BookmarkItem>
            {
                new("Work", isFolder: true)
            };
            bookmarks[0].Children.Add(new BookmarkItem("Google", "https://google.com"));

            string html = BookmarkService.ExportToHtml(bookmarks);

            Assert.Contains("<TITLE>Bookmarks</TITLE>", html);
            Assert.Contains("<H3>Work</H3>", html);
            Assert.Contains("HREF=\"https://google.com\"", html);
        }

        [Fact]
        public void ImportFromHtml_ParsesFoldersAndLinks()
        {
            string html = @"<!DOCTYPE NETSCAPE-Bookmark-file-1>
<DL><p>
    <DT><H3>Engineering</H3>
    <DL><p>
        <DT><A HREF=""https://stackoverflow.com"">StackOverflow</A>
    </DL><p>
    <DT><A HREF=""https://github.com"">GitHub</A>
</DL><p>";

            var list = BookmarkService.ImportFromHtml(html);

            Assert.True(list.Count >= 2);
            Assert.Contains(list, b => b.IsFolder && b.Title == "Engineering");
            Assert.Contains(list, b => !b.IsFolder && b.Url == "https://github.com");
        }
    }
}
