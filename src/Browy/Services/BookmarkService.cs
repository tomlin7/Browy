using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Browy.Models;

namespace Browy.Services
{
    public class BookmarkDto
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public static class BookmarkService
    {
        private static readonly string StorageDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Browy");

        private static readonly string FilePath = Path.Combine(StorageDirectory, "bookmarks.json");

        public static List<BookmarkItem> LoadBookmarks()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var dtos = JsonSerializer.Deserialize<List<BookmarkDto>>(json);
                    if (dtos != null && dtos.Count > 0)
                    {
                        return dtos.Select(d => new BookmarkItem(d.Title, d.Url)).ToList();
                    }
                }
            }
            catch
            {
                // Fallback to default bookmarks if reading fails
            }

            // Default seed bookmarks
            var defaults = new List<BookmarkItem>
            {
                new("GitHub", "https://github.com"),
                new("MDN Docs", "https://developer.mozilla.org"),
                new("YouTube", "https://youtube.com")
            };

            SaveBookmarks(defaults);
            return defaults;
        }

        public static void SaveBookmarks(IEnumerable<BookmarkItem> bookmarks)
        {
            try
            {
                Directory.CreateDirectory(StorageDirectory);
                var dtos = bookmarks.Select(b => new BookmarkDto
                {
                    Title = b.Title,
                    Url = b.Url
                }).ToList();

                string json = JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // Ignore persistence errors gracefully
            }
        }

        public static bool IsUrlBookmarked(IEnumerable<BookmarkItem> bookmarks, string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            string normalizedUrl = NormalizeUrl(url);
            return bookmarks.Any(b => NormalizeUrl(b.Url) == normalizedUrl);
        }

        private static string NormalizeUrl(string url)
        {
            url = url.Trim().TrimEnd('/');
            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                url = url.Substring(8);
            else if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                url = url.Substring(7);

            return url.ToLowerInvariant();
        }
    }
}
