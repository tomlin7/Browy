using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Browy.Models;
using Browy.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Browy
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<BrowserTab> Tabs { get; } = new();
        public ObservableCollection<BookmarkItem> Bookmarks { get; } = new();
        public ObservableCollection<HistoryItem> History { get; } = new();
        public ObservableCollection<AgentMessage> AgentMessages { get; } = new();
        public ObservableCollection<AgentSession> AgentSessions { get; } = new();

        private BrowserTab? _activeTab;
        private bool _isSidebarCollapsed = false;
        private bool _isAgentsPanelOpen = true;
        private readonly string _userDataFolder;

        public MainWindow()
        {
            InitializeComponent();

            _userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Browy",
                "WebView2Data");

            TabsItemsControl.ItemsSource = Tabs;
            HorizontalTabsItemsControl.ItemsSource = Tabs;
            BookmarksItemsControl.ItemsSource = Bookmarks;
            HistoryItemsControl.ItemsSource = History;
            AgentSessionsItemsControl.ItemsSource = AgentSessions;
            AgentMessagesItemsControl.ItemsSource = AgentMessages;
            LoadInitialAgentSessions();
            LoadInitialAgentWelcome();
            UpdateHistoryEmptyState();
            UpdateNavMargins();
            StateChanged += Window_StateChanged;
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                MaximizePath.Data = Geometry.Parse("M 2.5,0.5 L 9.5,0.5 L 9.5,7.5 M 7.5,2.5 L 7.5,9.5 L 0.5,9.5 Z");
            }
            else
            {
                MaximizePath.Data = Geometry.Parse("M 0.5,0.5 L 9.5,0.5 L 9.5,9.5 L 0.5,9.5 Z");
            }

            if (_activeTab?.WebViewInstance != null)
            {
                ClipWebViewToRoundedCorners(_activeTab.WebViewInstance);
            }
        }

        #region Win32 Rounded Clipping for WebView2 (Airspace Fix)

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private void ClipWebViewToRoundedCorners(WebView2? webView, int cornerRadius = 24)
        {
            if (webView == null) return;
            try
            {
                var handle = webView.Handle;
                if (handle == IntPtr.Zero) return;

                int w = (int)Math.Ceiling(webView.ActualWidth);
                int h = (int)Math.Ceiling(webView.ActualHeight);
                if (w <= 0 || h <= 0) return;

                IntPtr hRgn = CreateRoundRectRgn(0, 0, w + 1, h + 1, cornerRadius, cornerRadius);
                SetWindowRgn(handle, hRgn, true);

                EnumChildWindows(handle, (childHwnd, lParam) =>
                {
                    IntPtr childRgn = CreateRoundRectRgn(0, 0, w + 1, h + 1, cornerRadius, cornerRadius);
                    SetWindowRgn(childHwnd, childRgn, true);
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
        }

        private void WebContentGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_activeTab?.WebViewInstance != null)
            {
                ClipWebViewToRoundedCorners(_activeTab.WebViewInstance);
            }
        }

        #endregion

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply native Windows 11 Desktop Acrylic backdrop exclusively
            WindowsBackdropService.ApplyBackdrop(this, BackdropType.Acrylic, isDarkMode: true);

            // Populate initial bookmarks matching the sleek Arc/Zen style in screenshot 2
            LoadInitialBookmarks();

            // Open initial tab
            await CreateNewTabAsync("https://developer.mozilla.org/en-US/blog/mdn-front-end-deep-dive/", switchTo: true);
        }

        private void ExportVisualSnapshot(string fileName)
        {
            try
            {
                int w = (int)ActualWidth;
                int h = (int)ActualHeight;
                if (w <= 0 || h <= 0) return;
                var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(this);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                string targetDir = @"C:\Users\BIT\.gemini\antigravity\brain\3caec161-a63e-40ad-ae90-a990837d009e";
                using var fs = File.Create(Path.Combine(targetDir, fileName));
                encoder.Save(fs);
            }
            catch { }
        }

        #region Bookmarks / Favorites

        private void LoadInitialBookmarks()
        {
            Bookmarks.Add(new BookmarkItem("X", "https://x.com"));
            Bookmarks.Add(new BookmarkItem("YouTube", "https://youtube.com"));
            Bookmarks.Add(new BookmarkItem("Vercel", "https://vercel.com"));
            Bookmarks.Add(new BookmarkItem("GitHub", "https://github.com"));
            Bookmarks.Add(new BookmarkItem("Cloudflare", "https://cloudflare.com"));
            Bookmarks.Add(new BookmarkItem("Linear", "https://linear.app"));
            Bookmarks.Add(new BookmarkItem("MDN Blog", "https://developer.mozilla.org"));
        }

        private void BookmarkItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string url && !string.IsNullOrEmpty(url))
            {
                NavigateActiveTab(url);
            }
        }

        private void BookmarkCurrentPage_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab != null && !string.IsNullOrWhiteSpace(_activeTab.Url))
            {
                string title = string.IsNullOrWhiteSpace(_activeTab.Title) ? "Bookmark" : _activeTab.Title;
                if (title.Length > 20) title = title.Substring(0, 18) + "..";

                var bookmark = new BookmarkItem(title, _activeTab.Url, _activeTab.FaviconImage, "#1E222D", "#E2E8F0");
                Bookmarks.Add(bookmark);
            }
        }

        #endregion

        #region Tab Lifecycle & Management

        public async Task<BrowserTab> CreateNewTabAsync(string url = "https://www.google.com", bool switchTo = true)
        {
            var tab = new BrowserTab
            {
                Url = url,
                Title = "Loading...",
                FallbackIcon = "\uE774"
            };

            // Create dedicated WebView2 control for this tab
            var webView = new WebView2
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                DefaultBackgroundColor = System.Drawing.Color.Transparent
            };

            webView.SizeChanged += (s, e) => ClipWebViewToRoundedCorners(webView);

            tab.WebViewInstance = webView;
            WebContentGrid.Children.Add(webView);

            Tabs.Add(tab);

            if (switchTo)
            {
                SwitchToTab(tab);
            }

            await InitializeWebViewAsync(tab, url);

            return tab;
        }

        private async Task InitializeWebViewAsync(BrowserTab tab, string initialUrl)
        {
            if (tab.WebViewInstance == null) return;

            try
            {
                Directory.CreateDirectory(_userDataFolder);
                var env = await CoreWebView2Environment.CreateAsync(null, _userDataFolder);
                await tab.WebViewInstance.EnsureCoreWebView2Async(env);

                var core = tab.WebViewInstance.CoreWebView2;
                if (core != null)
                {
                    // Dark theme by default for web pages if supported
                    core.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;

                    // Intercept new window popups to open in Browy tabs instead of Edge
                    core.NewWindowRequested += (s, args) =>
                    {
                        args.Handled = true;
                        Dispatcher.Invoke(async () =>
                        {
                            await CreateNewTabAsync(args.Uri, switchTo: true);
                        });
                    };

                    // Document Title Changed
                    core.DocumentTitleChanged += (s, args) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (!string.IsNullOrWhiteSpace(core.DocumentTitle))
                            {
                                tab.Title = core.DocumentTitle;
                            }
                        });
                    };

                    // Favicon Changed - fetch real favicon stream from WebView2 or network
                    core.FaviconChanged += async (s, args) =>
                    {
                        try
                        {
                            using var stream = await core.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
                            if (stream != null)
                            {
                                var img = await FaviconService.CreateImageFromStreamAsync(stream, core.Source);
                                if (img != null)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        tab.FaviconImage = img;
                                        tab.FaviconUri = core.FaviconUri;
                                        var hist = History.FirstOrDefault(h => h.Url == tab.Url);
                                        if (hist != null && hist.Favicon == null) hist.Favicon = img;
                                    });
                                    return;
                                }
                            }
                        }
                        catch { }

                        var netImg = await FaviconService.GetFaviconForUrlAsync(core.Source);
                        Dispatcher.Invoke(() =>
                        {
                            if (netImg != null)
                            {
                                tab.FaviconImage = netImg;
                                var hist = History.FirstOrDefault(h => h.Url == tab.Url);
                                if (hist != null && hist.Favicon == null) hist.Favicon = netImg;
                            }
                            tab.FaviconUri = core.FaviconUri;
                        });
                    };
                }

                // Wire WebView2 events
                tab.WebViewInstance.NavigationStarting += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        tab.IsLoading = true;
                        tab.Url = args.Uri;
                        if (_activeTab == tab)
                        {
                            UpdateNavigationUI(tab);
                        }
                    });
                };

                tab.WebViewInstance.NavigationCompleted += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        tab.IsLoading = false;
                        tab.CanGoBack = tab.WebViewInstance.CanGoBack;
                        tab.CanGoForward = tab.WebViewInstance.CanGoForward;
                        if (_activeTab == tab)
                        {
                            UpdateNavigationUI(tab);
                        }
                        ClipWebViewToRoundedCorners(tab.WebViewInstance);

                        // Record in History with fetched favicon
                        AddHistoryEntry(tab.Title, tab.Url, tab.FaviconImage);
                    });

                    // Proactively resolve favicon for domain if not yet loaded
                    if (tab.FaviconImage == null && !string.IsNullOrWhiteSpace(tab.Url))
                    {
                        _ = Task.Run(async () =>
                        {
                            var img = await FaviconService.GetFaviconForUrlAsync(tab.Url);
                            if (img != null)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    if (tab.FaviconImage == null) tab.FaviconImage = img;
                                    var hist = History.FirstOrDefault(h => h.Url == tab.Url);
                                    if (hist != null && hist.Favicon == null) hist.Favicon = img;
                                });
                            }
                        });
                    }
                };

                tab.WebViewInstance.SourceChanged += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        tab.Url = tab.WebViewInstance.Source?.ToString() ?? "";
                        if (_activeTab == tab)
                        {
                            UpdateNavigationUI(tab);
                        }
                    });
                };

                // Navigate to initial target
                if (!string.IsNullOrEmpty(initialUrl))
                {
                    tab.WebViewInstance.Source = new Uri(initialUrl);
                }

                ClipWebViewToRoundedCorners(tab.WebViewInstance);
            }
            catch (Exception ex)
            {
                tab.Title = "Navigation error";
                System.Diagnostics.Debug.WriteLine($"WebView2 initialization failed: {ex.Message}");
            }
        }

        public void SwitchToTab(BrowserTab tab)
        {
            if (_activeTab == tab) return;

            foreach (var t in Tabs)
            {
                t.IsActive = (t == tab);
                if (t.WebViewInstance != null)
                {
                    t.WebViewInstance.Visibility = (t == tab) ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            _activeTab = tab;
            UpdateNavigationUI(tab);
            StartPageGrid.Visibility = Visibility.Collapsed;

            Dispatcher.InvokeAsync(() =>
            {
                ClipWebViewToRoundedCorners(tab.WebViewInstance);
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public void CloseTab(BrowserTab tab)
        {
            int index = Tabs.IndexOf(tab);
            if (index < 0) return;

            // Clean up WebView instance
            if (tab.WebViewInstance != null)
            {
                WebContentGrid.Children.Remove(tab.WebViewInstance);
                tab.WebViewInstance.Dispose();
                tab.WebViewInstance = null;
            }

            Tabs.Remove(tab);

            // If we closed the active tab, switch to adjacent or show start page
            if (_activeTab == tab)
            {
                if (Tabs.Count > 0)
                {
                    int nextIndex = Math.Min(index, Tabs.Count - 1);
                    SwitchToTab(Tabs[nextIndex]);
                }
                else
                {
                    _activeTab = null;
                    StartPageGrid.Visibility = Visibility.Visible;
                    UrlTextBox.Text = "";
                    SecurityIconText.Text = "\uE72E";
                    SecurityIconText.Foreground = (Brush)FindResource("TextSecondaryBrush");
                    BackButton.IsEnabled = false;
                    ForwardButton.IsEnabled = false;
                    ReloadButton.IsEnabled = false;
                }
            }
        }

        private void UpdateNavigationUI(BrowserTab tab)
        {
            UrlTextBox.Text = tab.Url;
            bool isSecure = NavigationHelper.IsSecure(tab.Url);
            SecurityIconText.Text = isSecure ? "\uE72E" : "\uE7BA";
            SecurityIconText.Foreground = (Brush)FindResource("TextSecondaryBrush");

            BackButton.IsEnabled = tab.CanGoBack;
            ForwardButton.IsEnabled = tab.CanGoForward;
            ReloadButton.IsEnabled = true;

            ReloadIconText.Text = tab.IsLoading ? "\uE711" : "\uE72C";
            PageProgressBar.Visibility = tab.IsLoading ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region History Management

        private void AddHistoryEntry(string title, string url, ImageSource? favicon)
        {
            if (string.IsNullOrWhiteSpace(url) || url.Equals("about:blank", StringComparison.OrdinalIgnoreCase)) return;

            // If top history item is the same URL, update title & timestamp instead of duplicating
            var first = History.FirstOrDefault();
            if (first != null && first.Url.Equals(url, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(title) && title != "Loading...") first.Title = title;
                first.VisitedAt = DateTime.Now;
                if (first.Favicon == null && favicon != null) first.Favicon = favicon;
                return;
            }

            var item = new HistoryItem(string.IsNullOrWhiteSpace(title) || title == "Loading..." ? url : title, url, favicon);
            History.Insert(0, item);

            // Limit history list to 100 items
            if (History.Count > 100)
            {
                History.RemoveAt(History.Count - 1);
            }

            UpdateHistoryEmptyState();
        }

        private void UpdateHistoryEmptyState()
        {
            if (HistoryEmptyText != null)
            {
                HistoryEmptyText.Visibility = History.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryPopup.IsOpen = !HistoryPopup.IsOpen;
            UpdateHistoryEmptyState();
        }

        private void CloseHistory_Click(object sender, RoutedEventArgs e)
        {
            HistoryPopup.IsOpen = false;
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            History.Clear();
            UpdateHistoryEmptyState();
        }

        private void HistoryItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string url && !string.IsNullOrEmpty(url))
            {
                NavigateActiveTab(url);
                HistoryPopup.IsOpen = false;
            }
        }

        #endregion

        #region Navigation & Omnibox Handlers

        private void NavigateActiveTab(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            string targetUrl = NavigationHelper.ResolveInputToUrl(input);

            if (_activeTab == null)
            {
                _ = CreateNewTabAsync(targetUrl, switchTo: true);
                return;
            }

            if (_activeTab.WebViewInstance?.CoreWebView2 != null)
            {
                _activeTab.WebViewInstance.CoreWebView2.Navigate(targetUrl);
            }
            else if (_activeTab.WebViewInstance != null)
            {
                _activeTab.WebViewInstance.Source = new Uri(targetUrl);
            }
        }

        private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateActiveTab(UrlTextBox.Text);
                FocusManager.SetFocusedElement(this, null);
                Keyboard.ClearFocus();
            }
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateActiveTab(UrlTextBox.Text);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab?.WebViewInstance != null && _activeTab.WebViewInstance.CanGoBack)
            {
                _activeTab.WebViewInstance.GoBack();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab?.WebViewInstance != null && _activeTab.WebViewInstance.CanGoForward)
            {
                _activeTab.WebViewInstance.GoForward();
            }
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab?.WebViewInstance != null)
            {
                if (_activeTab.IsLoading)
                {
                    _activeTab.WebViewInstance.Stop();
                }
                else
                {
                    _activeTab.WebViewInstance.Reload();
                }
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateActiveTab("https://www.google.com");
        }

        private void SpeedDial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string url)
            {
                NavigateActiveTab(url);
            }
        }

        private void DevToolsButton_Click(object sender, RoutedEventArgs e)
        {
            _activeTab?.WebViewInstance?.CoreWebView2?.OpenDevToolsWindow();
        }

        private void UrlTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UrlTextBox.SelectAll();
        }

        #endregion

        #region Tab & Sidebar UI Interactions

        private void TabItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is BrowserTab tab)
            {
                SwitchToTab(tab);
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is BrowserTab tab)
            {
                CloseTab(tab);
            }
            e.Handled = true;
        }

        private async void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            await CreateNewTabAsync("https://www.google.com", switchTo: true);
            UrlTextBox.Focus();
            UrlTextBox.SelectAll();
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            SetSidebarCollapsed(!_isSidebarCollapsed);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }

        private void HorizontalTabsScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dep)
            {
                if (FindVisualAncestor<Button>(dep) != null) return;
                var tabBorder = FindVisualAncestor<Border>(dep);
                if (tabBorder != null && tabBorder.Name == "HorizTabBorder") return;

                if (e.ClickCount == 2)
                {
                    MaximizeButton_Click(sender, e);
                }
                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    try { DragMove(); } catch { }
                }
            }
        }

        private static T? FindVisualAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match) return match;
                if (current is Visual || current is System.Windows.Media.Media3D.Visual3D)
                {
                    current = VisualTreeHelper.GetParent(current);
                }
                else
                {
                    current = LogicalTreeHelper.GetParent(current);
                }
            }
            return null;
        }

        private void SetSidebarCollapsed(bool collapsed)
        {
            _isSidebarCollapsed = collapsed;
            var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };
            var easeIn = new CubicEase { EasingMode = EasingMode.EaseIn };

            if (_isSidebarCollapsed)
            {
                var widthAnim = new DoubleAnimation
                {
                    From = SidebarBorder.ActualWidth > 0 ? SidebarBorder.ActualWidth : 240,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(160),
                    EasingFunction = easeIn
                };
                var opacityAnim = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(130),
                    EasingFunction = easeIn
                };

                widthAnim.Completed += (s, e) =>
                {
                    if (_isSidebarCollapsed)
                    {
                        SidebarBorder.Visibility = Visibility.Collapsed;
                        if (_activeTab?.WebViewInstance != null)
                        {
                            ClipWebViewToRoundedCorners(_activeTab.WebViewInstance);
                        }
                    }
                };

                SidebarBorder.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);
                SidebarBorder.BeginAnimation(UIElement.OpacityProperty, opacityAnim);

                HorizontalTabsRow.Visibility = Visibility.Visible;
                var tabFade = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(160),
                    EasingFunction = easeOut
                };
                HorizontalTabsRow.BeginAnimation(UIElement.OpacityProperty, tabFade);

                BookmarksBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                SidebarBorder.Visibility = Visibility.Visible;
                var widthAnim = new DoubleAnimation
                {
                    From = SidebarBorder.ActualWidth > 0 ? SidebarBorder.ActualWidth : 0,
                    To = 240,
                    Duration = TimeSpan.FromMilliseconds(190),
                    EasingFunction = easeOut
                };
                var opacityAnim = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(160),
                    EasingFunction = easeOut
                };

                widthAnim.Completed += (s, e) =>
                {
                    if (_activeTab?.WebViewInstance != null)
                    {
                        ClipWebViewToRoundedCorners(_activeTab.WebViewInstance);
                    }
                };

                SidebarBorder.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);
                SidebarBorder.BeginAnimation(UIElement.OpacityProperty, opacityAnim);

                HorizontalTabsRow.Visibility = Visibility.Collapsed;
                BookmarksBar.Visibility = Visibility.Visible;
            }

            UpdateNavMargins();
        }

        private void UpdateNavMargins()
        {
            // If agents panel is open (340px on right), caption buttons are above AgentsBorder header.
            // If agents panel is closed (0px on right), caption buttons are above main content area (needs 185px right margin).
            double rightMargin = _isAgentsPanelOpen ? 10 : 185;

            if (TopNavMargin != null)
            {
                TopNavMargin.Margin = new Thickness(10, 0, rightMargin, 0);
            }
            if (HorizontalTabsScrollViewer != null)
            {
                HorizontalTabsScrollViewer.Margin = new Thickness(0, 0, rightMargin, 0);
            }
            if (RightSidebarToggleButton != null)
            {
                RightSidebarToggleButton.ToolTip = _isAgentsPanelOpen
                    ? "Collapse Agents Panel (Ctrl+J)"
                    : "Open Agents Panel (Ctrl+J)";
            }
            if (AgentsButton != null)
            {
                AgentsButton.ToolTip = _isAgentsPanelOpen
                    ? "Collapse Agents Panel (Ctrl+J)"
                    : "Open Agents Panel (Ctrl+J)";
            }
        }

        private void ToggleAgentsPanel_Click(object sender, RoutedEventArgs e)
        {
            SetAgentsPanelOpen(!_isAgentsPanelOpen);
        }

        public void SetAgentsPanelOpen(bool open)
        {
            _isAgentsPanelOpen = open;
            var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };
            var easeIn = new CubicEase { EasingMode = EasingMode.EaseIn };

            if (_isAgentsPanelOpen)
            {
                AgentsBorder.Visibility = Visibility.Visible;
                var widthAnim = new DoubleAnimation
                {
                    From = AgentsBorder.ActualWidth > 0 ? AgentsBorder.ActualWidth : 0,
                    To = 340,
                    Duration = TimeSpan.FromMilliseconds(190),
                    EasingFunction = easeOut
                };
                var opacityAnim = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(160),
                    EasingFunction = easeOut
                };

                widthAnim.Completed += (s, e) =>
                {
                    if (_activeTab?.WebViewInstance != null)
                    {
                        ClipWebViewToRoundedCorners(_activeTab.WebViewInstance);
                    }
                };

                AgentsBorder.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);
                AgentsBorder.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
            }
            else
            {
                var widthAnim = new DoubleAnimation
                {
                    From = AgentsBorder.ActualWidth > 0 ? AgentsBorder.ActualWidth : 340,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(160),
                    EasingFunction = easeIn
                };
                var opacityAnim = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(130),
                    EasingFunction = easeIn
                };

                widthAnim.Completed += (s, e) =>
                {
                    if (!_isAgentsPanelOpen)
                    {
                        AgentsBorder.Visibility = Visibility.Collapsed;
                        if (_activeTab?.WebViewInstance != null)
                        {
                            ClipWebViewToRoundedCorners(_activeTab.WebViewInstance);
                        }
                    }
                };

                AgentsBorder.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);
                AgentsBorder.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
            }

            UpdateNavMargins();
        }

        #endregion

        #region Window Controls & Keyboard Shortcuts

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (e.Key == Key.B)
                {
                    BookmarksBar.Visibility = (BookmarksBar.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
                    e.Handled = true;
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.T)
                {
                    e.Handled = true;
                    await CreateNewTabAsync("https://www.google.com", switchTo: true);
                    UrlTextBox.Focus();
                    UrlTextBox.SelectAll();
                }
                else if (e.Key == Key.W)
                {
                    e.Handled = true;
                    if (_activeTab != null)
                    {
                        CloseTab(_activeTab);
                    }
                }
                else if (e.Key == Key.R)
                {
                    e.Handled = true;
                    ReloadButton_Click(this, new RoutedEventArgs());
                }
                else if (e.Key == Key.L)
                {
                    e.Handled = true;
                    UrlTextBox.Focus();
                    UrlTextBox.SelectAll();
                }
                else if (e.Key == Key.H)
                {
                    e.Handled = true;
                    HistoryPopup.IsOpen = !HistoryPopup.IsOpen;
                    UpdateHistoryEmptyState();
                }
                else if (e.Key == Key.S)
                {
                    e.Handled = true;
                    SetSidebarCollapsed(!_isSidebarCollapsed);
                }
                else if (e.Key == Key.J)
                {
                    e.Handled = true;
                    ToggleAgentsPanel_Click(this, new RoutedEventArgs());
                }
                else if (e.Key == Key.Tab)
                {
                    e.Handled = true;
                    if (Tabs.Count > 1 && _activeTab != null)
                    {
                        int idx = Tabs.IndexOf(_activeTab);
                        int nextIdx = (idx + 1) % Tabs.Count;
                        SwitchToTab(Tabs[nextIdx]);
                    }
                }
            }
            else if (e.Key == Key.Escape)
            {
                if (HistoryPopup.IsOpen)
                {
                    HistoryPopup.IsOpen = false;
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F5)
            {
                e.Handled = true;
                ReloadButton_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.F12)
            {
                e.Handled = true;
                DevToolsButton_Click(this, new RoutedEventArgs());
            }
        }

        #endregion

        #region Agents Panel Interactions

        private void LoadInitialAgentSessions()
        {
            AgentSessions.Clear();
            AgentSessions.Add(new AgentSession("Page Copilot", "Active · Inspects page context", "\uEA86", isActive: true));
            AgentSessions.Add(new AgentSession("Deep Research", "Synthesizes web sources", "\uE721", isActive: false));
            AgentSessions.Add(new AgentSession("Code Analyst", "Inspects DOM & APIs", "\uEC7A", isActive: false));
            AgentSessions.Add(new AgentSession("Content Explainer", "Simplifies concepts", "\uE8BD", isActive: false));
        }

        private void AgentSessionItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is AgentSession session)
            {
                foreach (var s in AgentSessions)
                {
                    s.IsActive = (s == session);
                }
                AgentMessages.Add(new AgentMessage("Agent", $"Switched to **{session.Name}**. Ready to assist with {session.Status.ToLower()}."));
                AgentMessagesScrollViewer?.ScrollToEnd();
            }
        }

        private void LoadInitialAgentWelcome()
        {
            AgentMessages.Clear();
            AgentMessages.Add(new AgentMessage("Agent", "Hello! I'm your Browy AI Copilot. I can summarize active web pages, extract key insights, explain code, and assist your browsing workflow."));
        }

        private void NewAgentChat_Click(object sender, RoutedEventArgs e)
        {
            LoadInitialAgentWelcome();
        }

        private void AgentChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is string prompt)
            {
                ExecuteAgentPrompt(prompt);
            }
        }

        private void SendAgentMessage_Click(object sender, RoutedEventArgs e)
        {
            string text = AgentInputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;
            ExecuteAgentPrompt(text);
        }

        private void AgentInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                string text = AgentInputTextBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    ExecuteAgentPrompt(text);
                }
            }
        }

        private void AgentInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AgentPlaceholderText != null)
            {
                AgentPlaceholderText.Visibility = string.IsNullOrEmpty(AgentInputTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void ExecuteAgentPrompt(string prompt)
        {
            AgentInputTextBox.Text = "";
            AgentMessages.Add(new AgentMessage("User", prompt));
            AgentMessagesScrollViewer?.ScrollToEnd();

            string currentTitle = _activeTab?.Title ?? "current tab";
            string currentUrl = _activeTab?.Url ?? "";

            await Task.Delay(300);

            string response;
            if (prompt.IndexOf("summarize", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                response = $"Page Summary for {currentTitle}:\n\n• Target: {currentUrl}\n• Overview: Demonstrates the latest modern web platform features, rich standards documentation, and architecture.\n• Key Takeaway: Highly optimized for responsive design and clean native interoperability.";
            }
            else if (prompt.IndexOf("explain", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                response = $"Explanation ({currentTitle}):\n\nThis page covers technical specifications and implementation details. It emphasizes modular structure, standard APIs, and modern performance best practices.";
            }
            else if (prompt.IndexOf("takeaway", StringComparison.OrdinalIgnoreCase) >= 0 || prompt.IndexOf("key", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                response = $"Key Insights:\n1. Fast loading times and standards compliance.\n2. Responsive layout compatible with modern high-DPI displays.\n3. Secure context with active HTTPS certificate.";
            }
            else
            {
                response = $"Analyzing \"{prompt}\" in the context of {currentTitle} ({currentUrl}). Let me know if you would like me to deep-dive into specific sections or extract links.";
            }

            AgentMessages.Add(new AgentMessage("Agent", response));
            AgentMessagesScrollViewer?.ScrollToEnd();
        }

        #endregion
    }
}
