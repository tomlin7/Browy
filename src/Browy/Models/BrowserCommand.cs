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

        public bool Matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            query = query.Trim();
            return Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   Shortcut.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
