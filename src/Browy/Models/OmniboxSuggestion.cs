using System;

namespace Browy.Models
{
    public enum SuggestionType
    {
        Search,
        History,
        Bookmark,
        Calculator,
        OpenTab
    }

    public class OmniboxSuggestion
    {
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public SuggestionType Type { get; set; }
        public BrowserTab? AssociatedTab { get; set; }
        public BrowserSpace? AssociatedSpace { get; set; }

        public string IconGlyph => Type switch
        {
            SuggestionType.Search => "\uE721",
            SuggestionType.History => "\uE81C",
            SuggestionType.Bookmark => "\uE734",
            SuggestionType.Calculator => "\uE947",
            SuggestionType.OpenTab => "\uE7C3",
            _ => "\uE721"
        };

        public string TypeBadge => Type switch
        {
            SuggestionType.Search => "Search",
            SuggestionType.History => "History",
            SuggestionType.Bookmark => "Bookmark",
            SuggestionType.Calculator => "Calculation",
            SuggestionType.OpenTab => "Switch to Tab",
            _ => ""
        };

        public bool HasUrl => !string.IsNullOrEmpty(Url);

        public OmniboxSuggestion(string title, string? url = null, SuggestionType type = SuggestionType.Search)
        {
            Title = title;
            Url = url;
            Type = type;
        }
    }
}
