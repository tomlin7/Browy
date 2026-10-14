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
        public bool IsFolder { get; set; } = false;
        public List<BookmarkDto>? Children { get; set; }
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
                        return dtos.Select(DtoToModel).ToList();
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
                var dtos = bookmarks.Select(ModelToDto).ToList();

                string json = JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // Ignore persistence errors gracefully
            }
        }

        public static BookmarkItem DtoToModel(BookmarkDto dto)
        {
            var item = new BookmarkItem
            {
                Title = dto.Title,
                Url = dto.Url,
                IsFolder = dto.IsFolder
            };

            if (dto.Children != null && dto.Children.Count > 0)
            {
                foreach (var childDto in dto.Children)
                {
                    item.Children.Add(DtoToModel(childDto));
                }
            }
            else if (!item.IsFolder && !string.IsNullOrWhiteSpace(item.Url))
            {
                _ = item.LoadFaviconAsync();
            }

            return item;
        }

        public static BookmarkDto ModelToDto(BookmarkItem item)
        {
            var dto = new BookmarkDto
            {
                Title = item.Title,
                Url = item.Url,
                IsFolder = item.IsFolder
            };

            if (item.Children.Count > 0)
            {
                dto.Children = item.Children.Select(ModelToDto).ToList();
            }

            return dto;
        }

        public static bool IsUrlBookmarked(IEnumerable<BookmarkItem> bookmarks, string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            string normalizedUrl = NormalizeUrl(url);

            bool CheckRecursive(IEnumerable<BookmarkItem> items)
            {
                foreach (var b in items)
                {
                    if (!b.IsFolder && NormalizeUrl(b.Url) == normalizedUrl)
                        return true;
                    if (b.Children.Count > 0 && CheckRecursive(b.Children))
                        return true;
                }
                return false;
            }

            return CheckRecursive(bookmarks);
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

        public static string ExportToHtml(IEnumerable<BookmarkItem> bookmarks)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<!DOCTYPE NETSCAPE-Bookmark-file-1>");
            sb.AppendLine("<!-- This is an automatically generated file. It will be read and overwritten. Do Not Edit! -->");
            sb.AppendLine("<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=UTF-8\">");
            sb.AppendLine("<TITLE>Bookmarks</TITLE>");
            sb.AppendLine("<H1>Bookmarks</H1>");
            sb.AppendLine("<DL><p>");

            void AppendItems(IEnumerable<BookmarkItem> items, int indent)
            {
                string pad = new string(' ', indent * 4);
                foreach (var b in items)
                {
                    string title = System.Net.WebUtility.HtmlEncode(b.Title);
                    if (b.IsFolder)
                    {
                        sb.AppendLine($"{pad}<DT><H3>{title}</H3>");
                        sb.AppendLine($"{pad}<DL><p>");
                        AppendItems(b.Children, indent + 1);
                        sb.AppendLine($"{pad}</DL><p>");
                    }
                    else
                    {
                        string url = System.Net.WebUtility.HtmlEncode(b.Url);
                        sb.AppendLine($"{pad}<DT><A HREF=\"{url}\">{title}</A>");
                    }
                }
            }

            AppendItems(bookmarks, 1);
            sb.AppendLine("</DL><p>");
            return sb.ToString();
        }

        public static List<BookmarkItem> ImportFromHtml(string html)
        {
            var list = new List<BookmarkItem>();
            if (string.IsNullOrWhiteSpace(html)) return list;

            // Matches <DT><H3>Title</H3> or <DT><A HREF="...">Title</A>
            var matches = System.Text.RegularExpressions.Regex.Matches(
                html,
                @"<DT>(?:<H3[^>]*>(?<folder>.*?)</H3>|<A\s+(?:[^>]*?\s+)?HREF=""(?<url>[^""]*)""[^>]*>(?<title>.*?)</A>)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups["folder"].Success)
                {
                    string folderName = System.Net.WebUtility.HtmlDecode(match.Groups["folder"].Value.Trim());
                    if (!string.IsNullOrEmpty(folderName))
                    {
                        list.Add(new BookmarkItem(folderName, isFolder: true));
                    }
                }
                else if (match.Groups["url"].Success)
                {
                    string url = System.Net.WebUtility.HtmlDecode(match.Groups["url"].Value.Trim());
                    string title = System.Net.WebUtility.HtmlDecode(match.Groups["title"].Value.Trim());
                    if (!string.IsNullOrEmpty(url))
                    {
                        if (string.IsNullOrWhiteSpace(title)) title = url;
                        list.Add(new BookmarkItem(title, url));
                    }
                }
            }
            return list;
        }
    }
}
