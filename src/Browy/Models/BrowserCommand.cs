using System;

namespace Browy.Models
{
    public class BrowserCommand
    {
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = "\uE712";
        public string Shortcut { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Action<MainWindow> ExecuteAction { get; set; }

        public BrowserCommand(string title, string category, string icon, string shortcut, string description, Action<MainWindow> executeAction)
        {
            Title = title;
            Category = category;
            Icon = icon;
            Shortcut = shortcut;
            Description = description;
            ExecuteAction = executeAction;
        }

        public int GetFuzzyScore(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return 100;

            query = query.Trim();

            if (Title.Equals(query, StringComparison.OrdinalIgnoreCase))
                return 1000;

            if (Title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                return 500;

            int titleIdx = Title.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (titleIdx >= 0)
                return 250 - Math.Min(50, titleIdx);

            if (!string.IsNullOrEmpty(Shortcut) && Shortcut.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return 150;

            if (!string.IsNullOrEmpty(Description) && Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return 100;

            if (!string.IsNullOrEmpty(Category) && Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return 80;

            // Fuzzy subsequence matching
            int patternIdx = 0;
            string target = Title.ToLowerInvariant();
            string pattern = query.ToLowerInvariant();
            for (int i = 0; i < target.Length && patternIdx < pattern.Length; i++)
            {
                if (target[i] == pattern[patternIdx])
                {
                    patternIdx++;
                }
            }

            if (patternIdx == pattern.Length)
                return 50;

            return 0;
        }

        public bool Matches(string query)
        {
            return GetFuzzyScore(query) > 0;
        }
    }
}
