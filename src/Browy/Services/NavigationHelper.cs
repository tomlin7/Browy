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
    }
}
