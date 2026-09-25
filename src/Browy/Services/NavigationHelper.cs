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

        public static string ResolveInputToUrl(string input, string searchEngineUrl = "https://www.google.com/search?q=")
        {
            if (string.IsNullOrWhiteSpace(input))
                return "https://www.google.com";

            input = input.Trim();

            // Direct scheme matches
            if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
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
    }
}
