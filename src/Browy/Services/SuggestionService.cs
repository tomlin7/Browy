using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Browy.Models;

namespace Browy.Services
{
    public static class SuggestionService
    {
        private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(3) };

        public static async Task<List<OmniboxSuggestion>> GetSuggestionsAsync(
            string query,
            IEnumerable<HistoryItem> history,
            IEnumerable<BookmarkItem> bookmarks,
            CancellationToken cancellationToken = default,
            IEnumerable<BrowserTab>? openTabs = null,
            IEnumerable<BrowserSpace>? allSpaces = null)
        {
            var results = new List<OmniboxSuggestion>();
            if (string.IsNullOrWhiteSpace(query))
                return results;

            string cleanQuery = query.Trim();

            // 0. Check Omnibox Instant Math Calculator if enabled
            if (BrowserSettings.IsFeatureEnabled("experimental_omnibox_calculator", true) &&
                OmniboxCalculator.TryEvaluate(cleanQuery, out string calcResult, out _))
            {
                results.Add(new OmniboxSuggestion(calcResult, null, SuggestionType.Calculator));
            }

            // 1. Add current search query as first item
            results.Add(new OmniboxSuggestion(cleanQuery, null, SuggestionType.Search));

            // 2. Match Open Tabs (Switch to Tab)
            if (allSpaces != null)
            {
                foreach (var space in allSpaces)
                {
                    foreach (var tab in space.Tabs)
                    {
                        if (tab.IsActive && space.IsActive) continue;
                        if ((tab.Title?.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (tab.Url?.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ?? false))
                        {
                            results.Add(new OmniboxSuggestion(tab.Title ?? tab.Url ?? "Open Tab", tab.Url, SuggestionType.OpenTab)
                            {
                                AssociatedTab = tab,
                                AssociatedSpace = space
                            });
                            if (results.Count(r => r.Type == SuggestionType.OpenTab) >= 2) break;
                        }
                    }
                    if (results.Count(r => r.Type == SuggestionType.OpenTab) >= 2) break;
                }
            }
            else if (openTabs != null)
            {
                var matchingTabs = openTabs
                    .Where(t => !t.IsActive &&
                               ((t.Title?.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                (t.Url?.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ?? false)))
                    .Take(2);

                foreach (var t in matchingTabs)
                {
                    results.Add(new OmniboxSuggestion(t.Title ?? t.Url ?? "Open Tab", t.Url, SuggestionType.OpenTab)
                    {
                        AssociatedTab = t
                    });
                }
            }

            // 3. Match local Bookmarks
            var matchingBookmarks = bookmarks
                .Where(b => (b.Title?.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (b.Url?.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ?? false))
                .Take(2);

            foreach (var b in matchingBookmarks)
            {
                results.Add(new OmniboxSuggestion(b.Title, b.Url, SuggestionType.Bookmark));
            }

            // 3. Match local History
            var matchingHistory = history
                .Where(h => (h.Title?.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (h.Url?.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ?? false))
                .Take(3);

            foreach (var h in matchingHistory)
            {
                // Avoid duplicating if already in bookmarks
                if (!results.Any(r => r.Url == h.Url))
                {
                    results.Add(new OmniboxSuggestion(h.Title, h.Url, SuggestionType.History));
                }
            }

            // 4. Fetch web search suggestions from DuckDuckGo AC / Google Suggest
            try
            {
                string url = $"https://duckduckgo.com/ac/?q={Uri.EscapeDataString(cleanQuery)}&type=list";
                using var response = await HttpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 1)
                    {
                        var list = doc.RootElement[1];
                        if (list.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in list.EnumerateArray())
                            {
                                string? suggestion = item.GetString();
                                if (!string.IsNullOrWhiteSpace(suggestion) &&
                                    !results.Any(r => r.Title.Equals(suggestion, StringComparison.OrdinalIgnoreCase)))
                                {
                                    results.Add(new OmniboxSuggestion(suggestion, null, SuggestionType.Search));
                                    if (results.Count >= 7) break;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Network unavailable or cancelled -> gracefully fall back to local results
            }

            return results;
        }
    }
}
