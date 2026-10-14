using System;
using System.Text.RegularExpressions;

namespace Browy.Services
{
    public static class NavigationHelper
    {
        private static readonly Regex DomainRegex = new(
            @"^([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}(:\d+)?(/.*)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex LocalhostRegex = new(
            @"^(localhost|127\.0\.0\.1)(:\d+)?(/.*)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string ResolveInputToUrl(string input, string? searchEngineUrl = null)
        {
            searchEngineUrl ??= BrowserSettings.Current.SearchEngineUrl;

            if (string.IsNullOrWhiteSpace(input))
                return BrowserSettings.Current.StartPageUrl;

            input = input.Trim();

            // Settings page aliases
            if (input.Equals("settings", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("browy://settings", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("about:settings", StringComparison.OrdinalIgnoreCase))
            {
                return "browy://settings";
            }

            // History aliases
            if (input.Equals("history", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("browy://history", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("about:history", StringComparison.OrdinalIgnoreCase))
            {
                return "browy://history";
            }

            // Bookmarks aliases
            if (input.Equals("bookmarks", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("browy://bookmarks", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("about:bookmarks", StringComparison.OrdinalIgnoreCase))
            {
                return "browy://bookmarks";
            }

            // Downloads aliases
            if (input.Equals("downloads", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("browy://downloads", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("about:downloads", StringComparison.OrdinalIgnoreCase))
            {
                return "browy://downloads";
            }

            // Extensions aliases
            if (input.Equals("extensions", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("browy://extensions", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("about:extensions", StringComparison.OrdinalIgnoreCase))
            {
                return "browy://extensions";
            }

            // Version & About aliases
            if (input.Equals("version", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("about", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("browy://version", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("browy://about", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("about:version", StringComparison.OrdinalIgnoreCase))
            {
                return "browy://version";
            }

            // Shortcuts aliases
            if (input.Equals("shortcuts", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("browy://shortcuts", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("about:shortcuts", StringComparison.OrdinalIgnoreCase))
            {
                return "browy://shortcuts";
            }

            // New Tab / Start page aliases
            if (input.Equals("newtab", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("start", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("home", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("browy://newtab", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("browy://start", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("about:newtab", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
            {
                return "browy://newtab";
            }

            // Direct scheme matches
            if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("browy://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("view-source:", StringComparison.OrdinalIgnoreCase))
            {
                return input;
            }

            // Keyword Bangs support (e.g. !yt lo-fi, !gh dotnet, !w alan turing)
            if (input.StartsWith("!") && input.Length > 2)
            {
                int spaceIndex = input.IndexOf(' ');
                if (spaceIndex > 1)
                {
                    string bang = input.Substring(1, spaceIndex - 1).ToLowerInvariant();
                    string query = input.Substring(spaceIndex + 1).Trim();
                    if (!string.IsNullOrEmpty(query))
                    {
                        string? bangUrl = GetBangSearchUrl(bang, query);
                        if (bangUrl != null)
                        {
                            return bangUrl;
                        }
                    }
                }
            }

            // If it contains spaces, it's definitely a search query
            if (input.Contains(' '))
            {
                return $"{searchEngineUrl}{Uri.EscapeDataString(input)}";
            }

            // Check if it's a domain name or localhost/IP
            if (DomainRegex.IsMatch(input) || LocalhostRegex.IsMatch(input))
            {
                return $"https://{input}";
            }

            // Default to search engine
            return $"{searchEngineUrl}{Uri.EscapeDataString(input)}";
        }

        public static bool IsSecure(string? url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        public static string ExtractDomain(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            try
            {
                var uri = new Uri(url);
                return uri.Host;
            }
            catch
            {
                return url;
            }
        }

        public static string? GetBangSearchUrl(string bang, string query)
        {
            string encoded = Uri.EscapeDataString(query);
            return bang switch
            {
                "g" or "google" => $"https://www.google.com/search?q={encoded}",
                "d" or "ddg" or "duck" => $"https://duckduckgo.com/?q={encoded}",
                "b" or "bing" => $"https://www.bing.com/search?q={encoded}",
                "yt" or "youtube" => $"https://www.youtube.com/results?search_query={encoded}",
                "w" or "wiki" or "wikipedia" => $"https://en.wikipedia.org/wiki/Special:Search?search={encoded}",
                "gh" or "github" => $"https://github.com/search?q={encoded}",
                "r" or "reddit" => $"https://www.reddit.com/search/?q={encoded}",
                "k" or "kagi" => $"https://kagi.com/search?q={encoded}",
                "brave" => $"https://search.brave.com/search?q={encoded}",
                "eco" or "ecosia" => $"https://www.ecosia.org/search?q={encoded}",
                "so" or "stackoverflow" => $"https://stackoverflow.com/search?q={encoded}",
                "npm" => $"https://www.npmjs.com/search?q={encoded}",
                "nuget" => $"https://www.nuget.org/packages?q={encoded}",
                "a" or "amz" or "amazon" => $"https://www.amazon.com/s?k={encoded}",
                _ => null
            };
        }
    }
}
