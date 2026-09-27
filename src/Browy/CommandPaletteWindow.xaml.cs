using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Browy.Models;
using Browy.Services;

namespace Browy
{
    public partial class CommandPaletteWindow : Window
    {
        private readonly MainWindow _mainWindow;
        private readonly List<BrowserCommand> _allCommands;
        private readonly ObservableCollection<BrowserCommand> _filteredCommands = new();

        public CommandPaletteWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
            Owner = mainWindow;

            _allCommands = BuildCommandRegistry();
            CommandsListBox.ItemsSource = _filteredCommands;
            FilterCommands(string.Empty);
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // Apply full hardware-accelerated Windows 11 Desktop Acrylic backdrop
            WindowsBackdropService.ApplyBackdrop(this, BackdropType.Acrylic, isDarkMode: true);

            // Position gracefully in upper third of owner window
            if (Owner != null)
            {
                Left = Owner.Left + (Owner.ActualWidth - Width) / 2;
                Top = Owner.Top + Math.Max(50, Owner.ActualHeight * 0.16);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Focus();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // Auto dismiss on click outside
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                ExecuteSelectedCommand();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                MoveSelection(1);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveSelection(-1);
                e.Handled = true;
            }
        }

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                MoveSelection(1);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveSelection(-1);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                ExecuteSelectedCommand();
                e.Handled = true;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchTextBox.Text;
            SearchPlaceholderText.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Collapsed;
            ClearSearchBtn.Visibility = string.IsNullOrEmpty(query) ? Visibility.Collapsed : Visibility.Visible;

            FilterCommands(query);
        }

        private void ClearSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchTextBox.Focus();
        }

        private void CommandsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ExecuteSelectedCommand();
        }

        private void FilterCommands(string query)
        {
            _filteredCommands.Clear();
            var matches = _allCommands.Where(c => c.Matches(query)).ToList();

            foreach (var cmd in matches)
            {
                _filteredCommands.Add(cmd);
            }

            EmptyResultsText.Visibility = _filteredCommands.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (_filteredCommands.Count > 0)
            {
                CommandsListBox.SelectedIndex = 0;
            }
        }

        private void MoveSelection(int delta)
        {
            if (_filteredCommands.Count == 0) return;

            int newIndex = CommandsListBox.SelectedIndex + delta;
            if (newIndex < 0) newIndex = _filteredCommands.Count - 1;
            else if (newIndex >= _filteredCommands.Count) newIndex = 0;

            CommandsListBox.SelectedIndex = newIndex;
            CommandsListBox.ScrollIntoView(CommandsListBox.SelectedItem);
        }

        private void ExecuteSelectedCommand()
        {
            if (CommandsListBox.SelectedItem is BrowserCommand selected)
            {
                Close();
                selected.ExecuteAction?.Invoke(_mainWindow);
            }
        }

        private List<BrowserCommand> BuildCommandRegistry()
        {
            return new List<BrowserCommand>
            {
                // Tabs
                new("New Tab", "Tabs", "\uE710", "Ctrl+T", "Open a new browser tab", w => w.AddNewTab()),
                new("Close Tab", "Tabs", "\uE711", "Ctrl+W", "Close the current active tab", w => w.CloseCurrentTab()),
                new("Reopen Closed Tab", "Tabs", "\uE7A7", "Ctrl+Shift+T", "Restore the most recently closed tab", w => w.ReopenClosedTab()),
                new("Duplicate Tab", "Tabs", "\uE7C3", "", "Duplicate the current tab in a new tab", w => w.DuplicateCurrentTab()),
                new("Next Tab", "Tabs", "\uE76C", "Ctrl+Tab", "Switch to the next tab", w => w.SelectNextTab()),
                new("Previous Tab", "Tabs", "\uE76B", "Ctrl+Shift+Tab", "Switch to the previous tab", w => w.SelectPreviousTab()),

                // Navigation
                new("Reload Page", "Navigation", "\uE72C", "Ctrl+R", "Reload the current webpage", w => w.ReloadCurrentPage()),
                new("Hard Reload (Bypass Cache)", "Navigation", "\uE895", "Ctrl+Shift+R", "Reload page ignoring local browser cache", w => w.HardReloadCurrentPage()),
                new("Go Back", "Navigation", "\uE76B", "Alt+Left", "Navigate back in browser history", w => w.GoBack()),
                new("Go Forward", "Navigation", "\uE76C", "Alt+Right", "Navigate forward in browser history", w => w.GoForward()),
                new("Open Home / Start Page", "Navigation", "\uE80F", "Alt+Home", "Return to the Browy start page", w => w.OpenStartPage()),
                new("Focus Address Bar", "Navigation", "\uE721", "Ctrl+L", "Select and edit the Omnibox address bar", w => w.FocusAddressBar()),
                new("Copy Page URL", "Navigation", "\uE8C8", "", "Copy current web address to clipboard", w => w.CopyCurrentUrl()),

                // Panels & Views
                new("Toggle Left Sidebar (Tabs)", "View", "\uE8A0", "Ctrl+S", "Collapse or expand vertical tab sidebar", w => w.ToggleSidebar()),
                new("Toggle Right Sidebar (Agents)", "View", "\uF4A5", "Ctrl+J", "Collapse or expand intelligent AI copilot panel", w => w.ToggleAgentsPanel()),
                new("Toggle Bookmarks Bar", "View", "\uE734", "Ctrl+B", "Show or hide the horizontal bookmarks strip", w => w.ToggleBookmarksBar()),
                new("Zoom In", "View", "\uE71F", "Ctrl++", "Magnify web page view", w => w.ZoomIn()),
                new("Zoom Out", "View", "\uE71F", "Ctrl+-", "Reduce web page view scale", w => w.ZoomOut()),
                new("Reset Zoom", "View", "\uE895", "Ctrl+0", "Reset webpage magnification to 100%", w => w.ResetZoom()),

                // Tools & Developer
                new("Developer Tools", "Tools", "\uEC7A", "F12", "Inspect DOM, network requests, and console logs", w => w.OpenDevTools()),
                new("View Page Source", "Tools", "\uE943", "Ctrl+U", "View the HTML source code of this page", w => w.ViewPageSource()),
                new("Print Page", "Tools", "\uE749", "Ctrl+P", "Print or export current page to PDF", w => w.PrintCurrentPage()),
                new("Bookmark This Page", "Tools", "\uE734", "Ctrl+D", "Save active page to your bookmarks", w => w.BookmarkCurrentPage()),
                new("Browsing History", "Tools", "\uE81C", "Ctrl+H", "Open browsing history drawer", w => w.ToggleHistory()),
                new("Clear Browsing History", "Tools", "\uE74D", "", "Erase all saved history entries", w => w.ClearAllHistory()),

                // AI & Agents
                new("New Agent Chat Session", "AI & Agents", "\uF4A5", "", "Start a fresh AI assistant conversation", w => w.StartNewAgentChat()),
                new("Summarize Webpage", "AI & Agents", "\uE890", "", "Generate a concise summary of the active site", w => w.TriggerAgentPrompt("Summarize current page")),
                new("Explain Webpage", "AI & Agents", "\uE8BD", "", "Explain the key concepts of the active site", w => w.TriggerAgentPrompt("Explain this webpage")),
                new("Extract Key Takeaways", "AI & Agents", "\uE721", "", "List actionable insights from this article", w => w.TriggerAgentPrompt("Find key takeaways"))
            };
        }
    }
}
