using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Browy.Models;
using Browy.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Browy
{
    public enum SplitViewMode
    {
        Single,
        DualVertical,
        DualHorizontal,
        Quad
    }

    public partial class MainWindow : Window
    {
        public ObservableCollection<BrowserTab> Tabs { get; } = new();
        public ObservableCollection<BookmarkItem> Bookmarks { get; } = new();
        public ObservableCollection<HistoryItem> History { get; } = new();
        public ObservableCollection<AgentMessage> AgentMessages { get; } = new();
        public ObservableCollection<AgentSession> AgentSessions { get; } = new();
        public ObservableCollection<DownloadItem> Downloads { get; } = new();
        public ObservableCollection<OmniboxSuggestion> OmniboxSuggestions { get; } = new();
        public ObservableCollection<ClosedTabItem> RecentlyClosedTabs { get; } = new();
        public ObservableCollection<BrowserSpace> Spaces { get; } = new();
        public BrowserSpace? ActiveSpace { get; private set; }
        public bool IsInPrivate { get; set; } = false;
        private CoreWebView2Environment? _inPrivateEnvironment;
        private bool _isSplitViewActive = false;
        public SplitViewMode SplitMode { get; private set; } = SplitViewMode.Single;
        private BrowserTab? _splitSecondaryTab = null;
        private BrowserTab? _splitTab3 = null;
        private BrowserTab? _splitTab4 = null;
        private CancellationTokenSource? _suggestionCts;

        private BrowserTab? _activeTab;
        public BrowserTab? ActiveTab => _activeTab;
        private bool _isSidebarCollapsed = false;
        private bool _isAgentsPanelOpen = true;
        private bool _isFindMatchCase = false;
        private bool _isFullscreen = false;
        private WindowState _savedWindowState = WindowState.Normal;
        private double _savedSidebarWidth = 240;
        private bool _savedIsSidebarCollapsed = false;
        private double _sidebarWidth = 240;
        private double _agentsWidth = 320;
        private bool _isDraggingLeftSplitter = false;
        private bool _isDraggingRightSplitter = false;
        private double _dragStartX = 0;
        private double _dragStartWidth = 0;
        private readonly string _userDataFolder;
        private readonly Stack<string> _recentlyClosedUrls = new();
        private CommandPaletteWindow? _commandPalette;
        private static CoreWebView2Environment? _sharedEnvironment;
        private static readonly SemaphoreSlim _envLock = new(1, 1);
        private Point _tabDragStartPoint;
        private BrowserTab? _draggedTab;
        private bool _isDraggingTab = false;

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
            DownloadsItemsControl.ItemsSource = Downloads;
            RecentlyClosedItemsControl.ItemsSource = RecentlyClosedTabs;
            OmniboxSuggestionsListBox.ItemsSource = OmniboxSuggestions;
            AgentPickerItemsControl.ItemsSource = AgentSessions;
            AgentMessagesItemsControl.ItemsSource = AgentMessages;
            LoadInitialAgentSessions();
            LoadInitialAgentWelcome();
            UpdateHistoryEmptyState();
            UpdateDownloadsEmptyState();
            UpdateNavMargins();
            FindPopup.CustomPopupPlacementCallback = FindPopupPlacementCallback;
            StateChanged += Window_StateChanged;

            InitializeGestureHandling();
            InitializeIdleTabTimer();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(WndProc);
        }

        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int MA_ACTIVATE = 1;
        private DateTime _lastSwipeTime = DateTime.MinValue;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE)
            {
                // When an inactive window or child WebView receives a mouse click,
                // activate and DO NOT eat the click (MA_ACTIVATE = 1).
                handled = true;
                return (IntPtr)MA_ACTIVATE;
            }

            if (msg == WM_MOUSEHWHEEL && BrowserSettings.Current.EnableTrackpadSwipe)
            {
                short tilt = unchecked((short)((long)wParam >> 16));
                if ((DateTime.Now - _lastSwipeTime).TotalMilliseconds > 400)
                {
                    if (tilt > 120)
                    {
                        _lastSwipeTime = DateTime.Now;
                        ForwardButton_Click(this, new RoutedEventArgs());
                        handled = true;
                    }
                    else if (tilt < -120)
                    {
                        _lastSwipeTime = DateTime.Now;
                        BackButton_Click(this, new RoutedEventArgs());
                        handled = true;
                    }
                }
            }

            return IntPtr.Zero;
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

            if (WindowState == WindowState.Minimized)
            {
                if (BrowserSettings.Current.EnableIdleTabSuspension)
                {
                    foreach (var tab in Tabs)
                    {
                        if (!tab.IsPlayingAudio && !tab.IsSleeping && tab.WebViewInstance?.CoreWebView2 != null)
                        {
                            try
                            {
                                _ = tab.WebViewInstance.CoreWebView2.TrySuspendAsync();
                                tab.IsSleeping = true;
                            }
                            catch { }
                        }
                    }
                }
            }
            else if (_activeTab?.IsSleeping == true && _activeTab.WebViewInstance?.CoreWebView2 != null)
            {
                WakeTab(_activeTab);
            }

            if (_activeTab?.WebViewInstance != null)
            {
                ClipWebViewToRoundedCorners(_activeTab.WebViewInstance);
            }
        }

        private System.Windows.Threading.DispatcherTimer? _idleTabTimer;

        private void InitializeIdleTabTimer()
        {
            _idleTabTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _idleTabTimer.Tick += async (s, e) =>
            {
                if (!BrowserSettings.Current.EnableIdleTabSuspension) return;
                int thresholdMins = Math.Max(1, BrowserSettings.Current.IdleTabSuspensionMinutes);
                DateTime cutoff = DateTime.Now.AddMinutes(-thresholdMins);

                foreach (var tab in Tabs)
                {
                    if (tab == _activeTab || tab == _splitSecondaryTab || tab == _splitTab3 || tab == _splitTab4) continue;
                    if (tab.IsPlayingAudio || tab.IsSleeping) continue;

                    if (tab.LastActiveTime <= cutoff && tab.WebViewInstance?.CoreWebView2 != null)
                    {
                        try
                        {
                            await tab.WebViewInstance.CoreWebView2.TrySuspendAsync();
                            tab.IsSleeping = true;
                        }
                        catch { }
                    }
                }
            };
            _idleTabTimer.Start();
        }

        private Point _gestureStartPoint;
        private bool _isMouseGestureTracking;
        private string? _currentGestureAction;

        private void InitializeGestureHandling()
        {
            this.PreviewMouseRightButtonDown += (s, e) =>
            {
                if (!BrowserSettings.Current.EnableMouseGestures) return;
                _gestureStartPoint = e.GetPosition(this);
                _isMouseGestureTracking = true;
                _currentGestureAction = null;
            };

            this.PreviewMouseMove += (s, e) =>
            {
                if (!_isMouseGestureTracking || !BrowserSettings.Current.EnableMouseGestures) return;
                var currentPt = e.GetPosition(this);
                double dx = currentPt.X - _gestureStartPoint.X;
                double dy = currentPt.Y - _gestureStartPoint.Y;

                if (Math.Abs(dx) > 40 || Math.Abs(dy) > 40)
                {
                    if (Math.Abs(dx) > Math.Abs(dy))
                    {
                        if (dx < -40)
                        {
                            _currentGestureAction = "Back";
                            GestureIconText.Text = "\uE76B";
                            GestureActionText.Text = "Back";
                            GestureOverlay.Visibility = Visibility.Visible;
                        }
                        else if (dx > 40)
                        {
                            _currentGestureAction = "Forward";
                            GestureIconText.Text = "\uE76C";
                            GestureActionText.Text = "Forward";
                            GestureOverlay.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        if (dy > 40)
                        {
                            _currentGestureAction = "NewTab";
                            GestureIconText.Text = "\uE710";
                            GestureActionText.Text = "New Tab";
                            GestureOverlay.Visibility = Visibility.Visible;
                        }
                        else if (dy < -40)
                        {
                            _currentGestureAction = "Reload";
                            GestureIconText.Text = "\uE72C";
                            GestureActionText.Text = "Reload";
                            GestureOverlay.Visibility = Visibility.Visible;
                        }
                    }
                }
            };

            this.PreviewMouseRightButtonUp += (s, e) =>
            {
                if (_isMouseGestureTracking)
                {
                    _isMouseGestureTracking = false;
                    GestureOverlay.Visibility = Visibility.Collapsed;

                    if (!string.IsNullOrEmpty(_currentGestureAction) && BrowserSettings.Current.EnableMouseGestures)
                    {
                        e.Handled = true;
                        switch (_currentGestureAction)
                        {
                            case "Back":
                                BackButton_Click(this, new RoutedEventArgs());
                                break;
                            case "Forward":
                                ForwardButton_Click(this, new RoutedEventArgs());
                                break;
                            case "NewTab":
                                _ = CreateNewTabAsync("browy://newtab", true);
                                break;
                            case "Reload":
                                ReloadButton_Click(this, new RoutedEventArgs());
                                break;
                        }
                        _currentGestureAction = null;
                    }
                }
            };
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

        public void ApplyThemeAccent(string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor)) return;
            try
            {
                if (!hexColor.StartsWith("#")) hexColor = "#" + hexColor;
                var color = (Color)ColorConverter.ConvertFromString(hexColor);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                Application.Current.Resources["AccentColor"] = color;
                Application.Current.Resources["AccentBrush"] = brush;
            }
            catch { }
        }

        #endregion

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply native Windows 11 Desktop Acrylic / Mica backdrop
            WindowsBackdropService.ApplyConfiguredBackdrop(this, BrowserSettings.Current.BackdropMaterial);

            // Apply configured theme accent tint
            ApplyThemeAccent(BrowserSettings.Current.AccentColorHex);

            // Populate initial bookmarks matching the sleek Arc/Zen style in screenshot 2
            LoadInitialBookmarks();
            BookmarksBar.Visibility = BrowserSettings.Current.ShowBookmarksBar ? Visibility.Visible : Visibility.Collapsed;

            if (IsInPrivate)
            {
                Title = "Browy [InPrivate]";
                InPrivateBadge.Visibility = Visibility.Visible;
                var inPrivateSpace = new BrowserSpace
                {
                    Name = "InPrivate",
                    IconGlyph = "\uE727",
                    AccentColorHex = "#94A3B8",
                    IsActive = true
                };
                Spaces.Add(inPrivateSpace);
                ActiveSpace = inPrivateSpace;
                await CreateNewTabAsync("browy://newtab", switchTo: true);
                return;
            }

            // Restore previous session or open start page
            bool restored = false;
            if (BrowserSettings.Current.StartupBehavior == "RestoreSession")
            {
                var session = SessionService.LoadSession();
                if (session != null && session.Spaces != null && session.Spaces.Count > 0)
                {
                    int activeSpaceIdx = Math.Clamp(session.ActiveSpaceIndex, 0, session.Spaces.Count - 1);
                    for (int s = 0; s < session.Spaces.Count; s++)
                    {
                        var sData = session.Spaces[s];
                        var space = new BrowserSpace
                        {
                            Id = sData.Id,
                            Name = sData.Name,
                            IconGlyph = sData.IconGlyph,
                            AccentColorHex = sData.AccentColorHex,
                            IsActive = (s == activeSpaceIdx),
                            IsIsolated = sData.IsIsolated,
                            ActiveTabIndex = sData.ActiveTabIndex
                        };

                        if (s == activeSpaceIdx)
                        {
                            ActiveSpace = space;
                            for (int t = 0; t < sData.Tabs.Count; t++)
                            {
                                var tData = sData.Tabs[t];
                                bool isTarget = (t == sData.ActiveTabIndex) || tData.IsActive;
                                var tab = await CreateNewTabAsync(tData.Url, switchTo: isTarget);
                                tab.IsPinned = tData.IsPinned;
                                if (!string.IsNullOrWhiteSpace(tData.Title)) tab.Title = tData.Title;
                            }
                        }
                        else
                        {
                            foreach (var tData in sData.Tabs)
                            {
                                space.Tabs.Add(new BrowserTab
                                {
                                    Url = tData.Url,
                                    Title = string.IsNullOrWhiteSpace(tData.Title) ? "New Tab" : tData.Title,
                                    IsPinned = tData.IsPinned
                                });
                            }
                        }

                        Spaces.Add(space);
                    }
                    restored = true;
                }
            }

            if (!restored)
            {
                var pSpace = new BrowserSpace { Name = "Personal", IconGlyph = "\uE80F", AccentColorHex = "#6366F1", IsActive = true };
                var wSpace = new BrowserSpace { Name = "Work", IconGlyph = "\uE7BE", AccentColorHex = "#EC4899" };
                var dSpace = new BrowserSpace { Name = "Dev", IconGlyph = "\uEC7A", AccentColorHex = "#10B981" };
                Spaces.Add(pSpace);
                Spaces.Add(wSpace);
                Spaces.Add(dSpace);
                ActiveSpace = pSpace;
                await CreateNewTabAsync(BrowserSettings.Current.StartPageUrl, switchTo: true);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (IsInPrivate)
            {
                try
                {
                    if (!string.IsNullOrEmpty(_userDataFolder) && Directory.Exists(_userDataFolder))
                    {
                        Directory.Delete(_userDataFolder, true);
                    }
                }
                catch { }
            }
            else if (BrowserSettings.Current.StartupBehavior == "RestoreSession")
            {
                SyncActiveSpaceTabs();
                SessionService.SaveSpacesSession(Spaces, ActiveSpace, _activeTab);
            }
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
            Bookmarks.Clear();
            var loaded = BookmarkService.LoadBookmarks();
            foreach (var b in loaded)
            {
                Bookmarks.Add(b);
            }
        }

        private void BookmarkItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem)
            {
                if (elem.Tag is BookmarkItem item)
                {
                    if (item.IsFolder)
                    {
                        var cm = new ContextMenu { Background = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x24)), BorderBrush = new SolidColorBrush(Color.FromArgb(0x25, 0xFF, 0xFF, 0xFF)) };
                        if (item.Children.Count == 0)
                        {
                            var emptyMi = new MenuItem { Header = "(Empty Folder)", IsEnabled = false, Foreground = Brushes.Gray };
                            cm.Items.Add(emptyMi);
                        }
                        else
                        {
                            foreach (var child in item.Children)
                            {
                                var mi = new MenuItem { Header = child.Title, Foreground = Brushes.White };
                                mi.Click += (s, ev) =>
                                {
                                    if (!string.IsNullOrEmpty(child.Url))
                                    {
                                        NavigateActiveTab(child.Url);
                                    }
                                };
                                cm.Items.Add(mi);
                            }
                        }
                        cm.PlacementTarget = elem;
                        cm.IsOpen = true;
                        return;
                    }

                    if (!string.IsNullOrEmpty(item.Url))
                    {
                        NavigateActiveTab(item.Url);
                    }
                }
                else if (elem.Tag is string url && !string.IsNullOrEmpty(url))
                {
                    NavigateActiveTab(url);
                }
            }
        }

        private void AddBookmarkBarFolder_Click(object sender, RoutedEventArgs e)
        {
            var folder = new BookmarkItem("New Folder", isFolder: true);
            Bookmarks.Add(folder);
            BookmarkService.SaveBookmarks(Bookmarks);
        }

        private void OpenAllBookmarks_Click(object sender, RoutedEventArgs e)
        {
            foreach (var b in Bookmarks)
            {
                if (!b.IsFolder && !string.IsNullOrEmpty(b.Url))
                {
                    _ = CreateNewTabAsync(b.Url, switchTo: false);
                }
                else if (b.IsFolder)
                {
                    foreach (var child in b.Children)
                    {
                        if (!string.IsNullOrEmpty(child.Url))
                        {
                            _ = CreateNewTabAsync(child.Url, switchTo: false);
                        }
                    }
                }
            }
        }

        private void BookmarkCurrentPage_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab != null && !string.IsNullOrWhiteSpace(_activeTab.Url))
            {
                string currentUrl = _activeTab.Url;
                var existing = Bookmarks.FirstOrDefault(b => BookmarkService.IsUrlBookmarked(new[] { b }, currentUrl));
                if (existing != null)
                {
                    // Already bookmarked -> remove
                    Bookmarks.Remove(existing);
                }
                else
                {
                    // Add new bookmark
                    string title = string.IsNullOrWhiteSpace(_activeTab.Title) || _activeTab.Title == "New Tab"
                        ? NavigationHelper.ExtractDomain(currentUrl)
                        : _activeTab.Title;
                    if (title.Length > 24) title = title.Substring(0, 22) + "..";

                    var bookmark = new BookmarkItem(title, currentUrl, _activeTab.FaviconImage);
                    Bookmarks.Add(bookmark);
                }

                BookmarkService.SaveBookmarks(Bookmarks);
                UpdateBookmarkStarUI();
            }
        }

        public void UpdateBookmarkStarUI()
        {
            if (BookmarkStarIcon == null) return;
            bool isBookmarked = _activeTab != null && BookmarkService.IsUrlBookmarked(Bookmarks, _activeTab.Url);
            BookmarkStarIcon.Text = isBookmarked ? "\uE735" : "\uE734"; // filled star vs outline
        }

        private void OpenBookmarkInNewTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is BookmarkItem item && !string.IsNullOrEmpty(item.Url))
            {
                _ = CreateNewTabAsync(item.Url, switchTo: true);
            }
        }

        private void CopyBookmarkUrl_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is BookmarkItem item && !string.IsNullOrEmpty(item.Url))
            {
                Clipboard.SetText(item.Url);
            }
        }

        private void DeleteBookmark_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is BookmarkItem item)
            {
                Bookmarks.Remove(item);
                BookmarkService.SaveBookmarks(Bookmarks);
                UpdateBookmarkStarUI();
            }
        }

        private void BookmarksScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (BookmarksScrollViewer != null)
            {
                BookmarksScrollViewer.ScrollToHorizontalOffset(BookmarksScrollViewer.HorizontalOffset - (e.Delta * 0.7));
                e.Handled = true;
            }
        }

        private void HorizontalTabsScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (HorizontalTabsScrollViewer != null)
            {
                HorizontalTabsScrollViewer.ScrollToHorizontalOffset(HorizontalTabsScrollViewer.HorizontalOffset - (e.Delta * 0.7));
                e.Handled = true;
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
            Grid.SetColumn(webView, 0);
            WebContentGrid.Children.Add(webView);

            Tabs.Add(tab);

            if (switchTo)
            {
                SwitchToTab(tab);
            }

            await InitializeWebViewAsync(tab, url);

            return tab;
        }

        private readonly Dictionary<string, CoreWebView2Environment> _spaceEnvironments = new();

        private async Task<CoreWebView2Environment> EnsureEnvironmentAsync(BrowserSpace? space = null)
        {
            if (IsInPrivate)
            {
                if (_inPrivateEnvironment != null) return _inPrivateEnvironment;
                string tempFolder = Path.Combine(Path.GetTempPath(), $"Browy_InPrivate_{Environment.ProcessId}_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempFolder);
                var privOptions = new CoreWebView2EnvironmentOptions(
                    additionalBrowserArguments: "--disable-features=CalculateNativeWinOcclusion,SpareRendererForSitePerProcess --renderer-process-limit=6"
                );
                _inPrivateEnvironment = await CoreWebView2Environment.CreateAsync(null, tempFolder, privOptions);
                return _inPrivateEnvironment;
            }

            if (space != null && space.IsIsolated)
            {
                if (_spaceEnvironments.TryGetValue(space.Id, out var existingEnv))
                {
                    return existingEnv;
                }
                string spaceFolder = Path.Combine(_userDataFolder, $"Space_{space.Id}");
                Directory.CreateDirectory(spaceFolder);
                var spaceOptions = new CoreWebView2EnvironmentOptions(
                    additionalBrowserArguments: "--disable-features=CalculateNativeWinOcclusion,SpareRendererForSitePerProcess --renderer-process-limit=6 --enable-features=msResourceUsageReport"
                );
                var isolatedEnv = await CoreWebView2Environment.CreateAsync(null, spaceFolder, spaceOptions);
                _spaceEnvironments[space.Id] = isolatedEnv;
                return isolatedEnv;
            }

            if (_sharedEnvironment != null) return _sharedEnvironment;

            await _envLock.WaitAsync();
            try
            {
                if (_sharedEnvironment != null) return _sharedEnvironment;

                var options = new CoreWebView2EnvironmentOptions(
                    additionalBrowserArguments: "--disable-features=CalculateNativeWinOcclusion,SpareRendererForSitePerProcess --renderer-process-limit=6 --enable-features=msResourceUsageReport"
                );

                try
                {
                    Directory.CreateDirectory(_userDataFolder);
                    _sharedEnvironment = await CoreWebView2Environment.CreateAsync(null, _userDataFolder, options);
                }
                catch (Exception ex)
                {
                    try
                    {
                        File.AppendAllText(@"E:\browy\crash.log", $"[{DateTime.Now:O}] Warning: Primary WebView2Data folder busy or locked ({ex.Message}), using session profile\n");
                    }
                    catch { }

                    string fallbackFolder = Path.Combine(Path.GetTempPath(), $"Browy_WebView2_{Environment.ProcessId}");
                    Directory.CreateDirectory(fallbackFolder);
                    _sharedEnvironment = await CoreWebView2Environment.CreateAsync(null, fallbackFolder, options);
                }

                return _sharedEnvironment;
            }
            finally
            {
                _envLock.Release();
            }
        }

        private async void HandleWebMessage(BrowserTab tab, string message)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(message);
                var root = doc.RootElement;
                if (!root.TryGetProperty("action", out var actionProp)) return;
                string action = actionProp.GetString() ?? "";

                switch (action)
                {
                    case "clear_history":
                    case "clear_all_history":
                        ClearAllHistory();
                        break;

                    case "delete_history_entry":
                        if (root.TryGetProperty("url", out var delUrlProp))
                        {
                            string delUrl = delUrlProp.GetString() ?? "";
                            var existing = History.FirstOrDefault(h => h.Url == delUrl);
                            if (existing != null) History.Remove(existing);
                            UpdateHistoryEmptyState();
                        }
                        break;

                    case "delete_bookmark":
                        if (root.TryGetProperty("url", out var delBmUrlProp))
                        {
                            string delUrl = delBmUrlProp.GetString() ?? "";
                            var existing = Bookmarks.FirstOrDefault(b => b.Url == delUrl);
                            if (existing != null) Bookmarks.Remove(existing);
                            BookmarkService.SaveBookmarks(Bookmarks);
                        }
                        break;

                    case "delete_history_batch":
                        if (root.TryGetProperty("urls", out var urlsArray) && urlsArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            var toRemove = new HashSet<string>(urlsArray.EnumerateArray().Select(u => u.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)));
                            var items = History.Where(h => toRemove.Contains(h.Url)).ToList();
                            foreach (var item in items) History.Remove(item);
                            UpdateHistoryEmptyState();
                            if (_activeTab?.Url.StartsWith("browy://history", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetHistoryHtml(History));
                            }
                        }
                        break;

                    case "delete_domain_history":
                        if (root.TryGetProperty("domain", out var domProp))
                        {
                            string dom = domProp.GetString() ?? "";
                            if (!string.IsNullOrEmpty(dom))
                            {
                                var items = History.Where(h =>
                                {
                                    try { return new Uri(h.Url).Host.Equals(dom, StringComparison.OrdinalIgnoreCase); }
                                    catch { return false; }
                                }).ToList();
                                foreach (var item in items) History.Remove(item);
                                UpdateHistoryEmptyState();
                                if (_activeTab?.Url.StartsWith("browy://history", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetHistoryHtml(History));
                                }
                            }
                        }
                        break;

                    case "clear_history_timeframe":
                        if (root.TryGetProperty("timeframe", out var tfProp))
                        {
                            string tf = tfProp.GetString() ?? "all";
                            DateTime cutoff = tf switch
                            {
                                "hour" => DateTime.Now.AddHours(-1),
                                "day" => DateTime.Today,
                                "week" => DateTime.Now.AddDays(-7),
                                _ => DateTime.MinValue
                            };
                            var toDel = History.Where(h => h.VisitedAt >= cutoff).ToList();
                            foreach (var itm in toDel) History.Remove(itm);
                            UpdateHistoryEmptyState();
                            if (_activeTab?.Url.StartsWith("browy://history", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetHistoryHtml(History));
                            }
                        }
                        break;

                    case "pause_download":
                        if (root.TryGetProperty("id", out var pIdProp))
                        {
                            string id = pIdProp.GetString() ?? "";
                            var d = Downloads.FirstOrDefault(x => x.Id == id);
                            d?.Pause();
                            if (_activeTab?.Url.StartsWith("browy://downloads", StringComparison.OrdinalIgnoreCase) == true)
                                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetDownloadsHtml(Downloads));
                        }
                        break;

                    case "resume_download":
                        if (root.TryGetProperty("id", out var rIdProp))
                        {
                            string id = rIdProp.GetString() ?? "";
                            var d = Downloads.FirstOrDefault(x => x.Id == id);
                            d?.Resume();
                            if (_activeTab?.Url.StartsWith("browy://downloads", StringComparison.OrdinalIgnoreCase) == true)
                                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetDownloadsHtml(Downloads));
                        }
                        break;

                    case "cancel_download":
                        if (root.TryGetProperty("id", out var cIdProp))
                        {
                            string id = cIdProp.GetString() ?? "";
                            var d = Downloads.FirstOrDefault(x => x.Id == id);
                            d?.Cancel();
                            UpdateDownloadsButtonBadge();
                            if (_activeTab?.Url.StartsWith("browy://downloads", StringComparison.OrdinalIgnoreCase) == true)
                                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetDownloadsHtml(Downloads));
                        }
                        break;

                    case "clear_completed_downloads":
                        ClearDownloads_Click(this, new RoutedEventArgs());
                        if (_activeTab?.Url.StartsWith("browy://downloads", StringComparison.OrdinalIgnoreCase) == true)
                            _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetDownloadsHtml(Downloads));
                        break;

                    case "open_download_file":
                        if (root.TryGetProperty("path", out var pVal))
                        {
                            string p = pVal.GetString() ?? "";
                            if (!string.IsNullOrEmpty(p) && File.Exists(p))
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(p) { UseShellExecute = true });
                            }
                        }
                        break;

                    case "show_download_in_folder":
                        if (root.TryGetProperty("path", out var foldVal))
                        {
                            string p = foldVal.GetString() ?? "";
                            if (!string.IsNullOrEmpty(p) && File.Exists(p))
                            {
                                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{p}\"");
                            }
                        }
                        break;

                    case "clear_cookies":
                        if (tab.WebViewInstance?.CoreWebView2 != null)
                        {
                            await tab.WebViewInstance.CoreWebView2.Profile.ClearBrowsingDataAsync();
                        }
                        break;

                    case "set_search_engine":
                        if (root.TryGetProperty("name", out var nameProp) && root.TryGetProperty("url", out var urlProp))
                        {
                            BrowserSettings.SetSearchEngine(nameProp.GetString() ?? "Google", urlProp.GetString() ?? "https://www.google.com/search?q=");
                        }
                        break;

                    case "toggle_sidebar":
                        ToggleSidebar();
                        break;

                    case "open_downloads_folder":
                        OpenDownloadsFolder_Click(this, new RoutedEventArgs());
                        break;

                    case "exit_reader":
                        ExitReaderMode(tab);
                        break;

                    case "navigate":
                        if (root.TryGetProperty("url", out var urlVal))
                        {
                            string target = urlVal.GetString() ?? "";
                            if (!string.IsNullOrWhiteSpace(target))
                            {
                                NavigateActiveTab(target);
                            }
                        }
                        break;

                    case "set_startup_behavior":
                        if (root.TryGetProperty("behavior", out var behProp))
                        {
                            BrowserSettings.Current.StartupBehavior = behProp.GetString() ?? "RestoreSession";
                            BrowserSettings.Save();
                        }
                        break;

                    case "set_shield_enabled":
                        if (root.TryGetProperty("enabled", out var shProp))
                        {
                            BrowserSettings.Current.IsShieldEnabled = shProp.GetBoolean();
                            BrowserSettings.Save();
                            if (_activeTab != null) UpdateShieldUI(_activeTab);
                        }
                        break;

                    case "load_unpacked_extension":
                        await LoadUnpackedExtensionAsync();
                        break;

                    case "toggle_extension":
                        if (root.TryGetProperty("id", out var extIdProp) && root.TryGetProperty("enabled", out var extEnProp))
                        {
                            await ExtensionService.ToggleExtensionAsync(tab.WebViewInstance?.CoreWebView2?.Profile, extIdProp.GetString() ?? "", extEnProp.GetBoolean());
                        }
                        break;

                    case "remove_extension":
                        if (root.TryGetProperty("id", out var remIdProp))
                        {
                            await ExtensionService.RemoveExtensionAsync(tab.WebViewInstance?.CoreWebView2?.Profile, remIdProp.GetString() ?? "");
                        }
                        break;

                    case "set_ai_config":
                        if (root.TryGetProperty("provider", out var pProp)) BrowserSettings.Current.AiProvider = pProp.GetString() ?? "BrowyDefault";
                        if (root.TryGetProperty("openAiKey", out var oaKey)) BrowserSettings.Current.OpenAiApiKey = oaKey.GetString() ?? "";
                        if (root.TryGetProperty("anthropicKey", out var clKey)) BrowserSettings.Current.AnthropicApiKey = clKey.GetString() ?? "";
                        if (root.TryGetProperty("ollamaEndpoint", out var olEnd)) BrowserSettings.Current.OllamaEndpoint = olEnd.GetString() ?? "http://localhost:11434";
                        if (root.TryGetProperty("selectedModel", out var sModel)) BrowserSettings.Current.SelectedAiModel = sModel.GetString() ?? "gpt-4o-mini";
                        BrowserSettings.Save();
                        break;

                    case "set_backdrop_material":
                        if (root.TryGetProperty("material", out var matProp))
                        {
                            string mat = matProp.GetString() ?? "Acrylic";
                            BrowserSettings.Current.BackdropMaterial = mat;
                            BrowserSettings.Save();
                            WindowsBackdropService.ApplyConfiguredBackdrop(this, mat);
                        }
                        break;

                    case "set_tab_suspension":
                        if (root.TryGetProperty("enabled", out var susEnProp))
                            BrowserSettings.Current.EnableIdleTabSuspension = susEnProp.GetBoolean();
                        if (root.TryGetProperty("minutes", out var susMinProp))
                            BrowserSettings.Current.IdleTabSuspensionMinutes = susMinProp.GetInt32();
                        BrowserSettings.Save();
                        break;

                    case "set_gestures":
                        if (root.TryGetProperty("mouse", out var mGProp))
                            BrowserSettings.Current.EnableMouseGestures = mGProp.GetBoolean();
                        if (root.TryGetProperty("trackpad", out var tpProp))
                            BrowserSettings.Current.EnableTrackpadSwipe = tpProp.GetBoolean();
                        BrowserSettings.Save();
                        break;

                    case "set_custom_shortcut":
                        if (root.TryGetProperty("actionId", out var aProp) && root.TryGetProperty("shortcut", out var scProp))
                        {
                            string aId = aProp.GetString() ?? "";
                            string sc = scProp.GetString() ?? "";
                            if (!string.IsNullOrEmpty(aId))
                            {
                                BrowserSettings.Current.CustomKeyBindings[aId] = sc;
                                BrowserSettings.Save();
                            }
                        }
                        break;

                    case "reset_custom_shortcuts":
                        BrowserSettings.Current.CustomKeyBindings.Clear();
                        BrowserSettings.Save();
                        break;

                    case "set_theme_mode":
                        if (root.TryGetProperty("mode", out var tmProp))
                        {
                            string mode = tmProp.GetString() ?? "Dark";
                            BrowserSettings.SetThemeMode(mode);
                        }
                        break;

                    case "set_accent_color":
                        if (root.TryGetProperty("color", out var colProp))
                        {
                            string hex = colProp.GetString() ?? "#3B82F6";
                            BrowserSettings.SetAccentColor(hex);
                            ApplyThemeAccent(hex);
                        }
                        break;

                    case "snooze_inactive_tabs":
                        SnoozeAllInactiveTabs();
                        break;

                    case "wake_all_tabs":
                        WakeAllTabs();
                        break;

                    case "export_bookmarks_html":
                        ExportBookmarksHtml();
                        break;

                    case "import_bookmarks_html":
                        ImportBookmarksHtml();
                        break;

                    case "export_history_json":
                        ExportHistoryJson();
                        break;

                    case "copy_diagnostics":
                    case "copy_to_clipboard":
                        if (root.TryGetProperty("text", out var diagTextProp))
                        {
                            string diagText = diagTextProp.GetString() ?? "";
                            if (!string.IsNullOrEmpty(diagText))
                            {
                                try { Clipboard.SetText(diagText); } catch { }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                try { File.AppendAllText(@"E:\browy\crash.log", $"[{DateTime.Now:O}] HandleWebMessage error: {ex}\n"); } catch { }
            }
        }

        private async Task InitializeWebViewAsync(BrowserTab tab, string initialUrl)
        {
            if (tab.WebViewInstance == null) return;

            try
            {
                var env = await EnsureEnvironmentAsync(ActiveSpace);
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
                            if (!string.IsNullOrWhiteSpace(core.DocumentTitle) &&
                                !tab.Url.StartsWith("browy://", StringComparison.OrdinalIgnoreCase))
                            {
                                tab.Title = core.DocumentTitle;
                            }
                        });
                    };

                    // Favicon Changed - fetch real favicon stream from WebView2 or network
                    core.FaviconChanged += async (s, args) =>
                    {
                        if (tab.Url.StartsWith("browy://", StringComparison.OrdinalIgnoreCase)) return;

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

                    // Downloads handling
                    core.DownloadStarting += CoreWebView2_DownloadStarting;

                    // Find in page events
                    if (core.Find != null)
                    {
                        core.Find.MatchCountChanged += (s, args) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (_activeTab?.WebViewInstance?.CoreWebView2 == core && FindPopup.IsOpen)
                                {
                                    UpdateFindMatchUI(core.Find.ActiveMatchIndex, core.Find.MatchCount);
                                }
                            });
                        };

                        core.Find.ActiveMatchIndexChanged += (s, args) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (_activeTab?.WebViewInstance?.CoreWebView2 == core && FindPopup.IsOpen)
                                {
                                    UpdateFindMatchUI(core.Find.ActiveMatchIndex, core.Find.MatchCount);
                                }
                            });
                        };
                    }

                    // Audio playing and mute state
                    core.IsDocumentPlayingAudioChanged += (s, args) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            tab.IsPlayingAudio = core.IsDocumentPlayingAudio;
                        });
                    };

                    core.IsMutedChanged += (s, args) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            tab.IsMuted = core.IsMuted;
                        });
                    };

                    // Web message dispatch for internal settings and UI integrations
                    core.WebMessageReceived += (s, args) =>
                    {
                        try
                        {
                            string msg = args.TryGetWebMessageAsString();
                            HandleWebMessage(tab, msg);
                        }
                        catch { }
                    };

                    // Web Notifications & Permissions
                    core.PermissionRequested += (s, args) =>
                    {
                        if (args.PermissionKind == CoreWebView2PermissionKind.Notifications)
                        {
                            try
                            {
                                var uri = new Uri(args.Uri);
                                string origin = $"{uri.Scheme}://{uri.Host}";
                                if (BrowserSettings.Current.BlockedNotificationOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                                {
                                    args.State = CoreWebView2PermissionState.Deny;
                                }
                                else
                                {
                                    args.State = CoreWebView2PermissionState.Allow;
                                    if (!BrowserSettings.Current.AllowedNotificationOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                                    {
                                        BrowserSettings.Current.AllowedNotificationOrigins.Add(origin);
                                        BrowserSettings.Save();
                                    }
                                }
                            }
                            catch
                            {
                                args.State = CoreWebView2PermissionState.Allow;
                            }
                        }
                    };

                    core.NotificationReceived += (s, args) =>
                    {
                        args.Handled = true;
                        string title = args.Notification.Title;
                        string body = args.Notification.Body;
                        string origin = args.SenderOrigin;
                        NotificationService.ShowToast(title, body, origin);
                    };
                    // Browy Shield Network Request Interception (Ad & Tracker Blocking)
                    core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                    core.WebResourceRequested += (sender, args) =>
                    {
                        if (args.ResourceContext != CoreWebView2WebResourceContext.Document &&
                            BrowserSettings.Current.IsShieldEnabled &&
                            ShieldService.IsBlocked(args.Request.Uri, out _))
                        {
                            try
                            {
                                var response = core.Environment.CreateWebResourceResponse(
                                    new MemoryStream(), 204, "No Content", "Content-Type: text/plain");
                                args.Response = response;
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    tab.BlockedCount++;
                                    if (_activeTab == tab)
                                    {
                                        UpdateShieldUI(tab);
                                    }
                                }));
                            }
                            catch { }
                        }
                    };
                }

                // Wire WebView2 events
                tab.WebViewInstance.NavigationStarting += (s, args) =>
                {
                    if (args.Uri.StartsWith("browy://settings", StringComparison.OrdinalIgnoreCase) ||
                        args.Uri.StartsWith("about:settings", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Cancel = true;
                        Dispatcher.Invoke(() =>
                        {
                            tab.Url = "browy://settings";
                            tab.Title = "Settings - Browy";
                            tab.FallbackIcon = "\uE713";
                            tab.IsLoading = false;
                            tab.BlockedCount = 0;
                            if (_activeTab == tab)
                            {
                                UpdateNavigationUI(tab);
                            }
                            tab.WebViewInstance.NavigateToString(SettingsPageService.GetSettingsHtml());
                        });
                        return;
                    }

                    if (args.Uri.StartsWith("browy://history", StringComparison.OrdinalIgnoreCase) ||
                        args.Uri.StartsWith("about:history", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Cancel = true;
                        Dispatcher.Invoke(() =>
                        {
                            tab.Url = "browy://history";
                            tab.Title = "History - Browy";
                            tab.FallbackIcon = "\uE81C";
                            tab.IsLoading = false;
                            tab.BlockedCount = 0;
                            if (_activeTab == tab) UpdateNavigationUI(tab);
                            tab.WebViewInstance.NavigateToString(InternalHubService.GetHistoryHtml(History));
                        });
                        return;
                    }

                    if (args.Uri.StartsWith("browy://bookmarks", StringComparison.OrdinalIgnoreCase) ||
                        args.Uri.StartsWith("about:bookmarks", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Cancel = true;
                        Dispatcher.Invoke(() =>
                        {
                            tab.Url = "browy://bookmarks";
                            tab.Title = "Bookmarks - Browy";
                            tab.FallbackIcon = "\uE734";
                            tab.IsLoading = false;
                            tab.BlockedCount = 0;
                            if (_activeTab == tab) UpdateNavigationUI(tab);
                            tab.WebViewInstance.NavigateToString(InternalHubService.GetBookmarksHtml(Bookmarks));
                        });
                        return;
                    }

                    if (args.Uri.StartsWith("browy://downloads", StringComparison.OrdinalIgnoreCase) ||
                        args.Uri.StartsWith("about:downloads", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Cancel = true;
                        Dispatcher.Invoke(() =>
                        {
                            tab.Url = "browy://downloads";
                            tab.Title = "Downloads - Browy";
                            tab.FallbackIcon = "\uE896";
                            tab.IsLoading = false;
                            tab.BlockedCount = 0;
                            if (_activeTab == tab) UpdateNavigationUI(tab);
                            tab.WebViewInstance.NavigateToString(InternalHubService.GetDownloadsHtml(Downloads));
                        });
                        return;
                    }

                    if (args.Uri.StartsWith("browy://extensions", StringComparison.OrdinalIgnoreCase) ||
                        args.Uri.StartsWith("about:extensions", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Cancel = true;
                        Dispatcher.Invoke(() =>
                        {
                            tab.Url = "browy://extensions";
                            tab.Title = "Extensions - Browy";
                            tab.FallbackIcon = "\uE74C";
                            tab.IsLoading = false;
                            tab.BlockedCount = 0;
                            if (_activeTab == tab) UpdateNavigationUI(tab);
                            tab.WebViewInstance.NavigateToString(InternalHubService.GetExtensionsHtml(ExtensionService.Extensions));
                        });
                        return;
                    }

                    if (args.Uri.StartsWith("browy://version", StringComparison.OrdinalIgnoreCase) ||
                        args.Uri.StartsWith("browy://about", StringComparison.OrdinalIgnoreCase) ||
                        args.Uri.StartsWith("about:version", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Cancel = true;
                        Dispatcher.Invoke(() =>
                        {
                            tab.Url = "browy://version";
                            tab.Title = "About - Browy";
                            tab.FallbackIcon = "\uE946";
                            tab.IsLoading = false;
                            tab.BlockedCount = 0;
                            if (_activeTab == tab) UpdateNavigationUI(tab);
                            tab.WebViewInstance.NavigateToString(InternalHubService.GetAboutHtml(Tabs.Count, Spaces.Count));
                        });
                        return;
                    }

                    if (args.Uri.StartsWith("browy://shortcuts", StringComparison.OrdinalIgnoreCase) ||
                        args.Uri.StartsWith("about:shortcuts", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Cancel = true;
                        Dispatcher.Invoke(() =>
                        {
                            tab.Url = "browy://shortcuts";
                            tab.Title = "Shortcuts - Browy";
                            tab.FallbackIcon = "\uE765";
                            tab.IsLoading = false;
                            tab.BlockedCount = 0;
                            if (_activeTab == tab) UpdateNavigationUI(tab);
                            tab.WebViewInstance.NavigateToString(InternalHubService.GetShortcutsHtml(BrowserSettings.Current.CustomKeyBindings));
                        });
                        return;
                    }

                    if (args.Uri.StartsWith("browy://newtab", StringComparison.OrdinalIgnoreCase) ||
                        args.Uri.StartsWith("browy://start", StringComparison.OrdinalIgnoreCase) ||
                        args.Uri.StartsWith("about:newtab", StringComparison.OrdinalIgnoreCase) ||
                        args.Uri.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Cancel = true;
                        Dispatcher.Invoke(() =>
                        {
                            tab.Url = "browy://newtab";
                            tab.Title = "New Tab";
                            tab.FallbackIcon = "\uE72B";
                            tab.IsLoading = false;
                            tab.BlockedCount = 0;
                            if (_activeTab == tab)
                            {
                                UpdateNavigationUI(tab);
                            }
                            tab.WebViewInstance.NavigateToString(NewTabPageService.GetNewTabHtml());
                        });
                        return;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        tab.BlockedCount = 0;
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

                        if (!args.IsSuccess)
                        {
                            if (args.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled &&
                                !tab.Url.StartsWith("browy://", StringComparison.OrdinalIgnoreCase))
                            {
                                tab.Title = $"Site unreachable ({args.WebErrorStatus})";
                                try
                                {
                                    tab.WebViewInstance.NavigateToString(SettingsPageService.GetErrorPageHtml(tab.Url, args.WebErrorStatus.ToString()));
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(tab.Title) || tab.Title == "Loading...")
                            {
                                tab.Title = NavigationHelper.ExtractDomain(tab.Url);
                            }

                            double savedZoom = BrowserSettings.GetSiteZoomLevel(tab.Url);
                            if (Math.Abs(tab.WebViewInstance.ZoomFactor - savedZoom) > 0.01)
                            {
                                tab.WebViewInstance.ZoomFactor = savedZoom;
                            }
                        }

                        if (_activeTab == tab)
                        {
                            UpdateNavigationUI(tab);
                            if (FindPopup.IsOpen && !string.IsNullOrWhiteSpace(FindTextBox.Text))
                            {
                                FindActiveTab(FindTextBox.Text, _isFindMatchCase);
                            }
                        }
                        ClipWebViewToRoundedCorners(tab.WebViewInstance);

                        if (args.IsSuccess && !tab.IsReaderModeActive && !tab.Url.StartsWith("browy://", StringComparison.OrdinalIgnoreCase))
                        {
                            _ = DetectReaderModeAsync(tab);
                        }

                        // Record in History with fetched favicon if successful navigation
                        if (args.IsSuccess && !tab.Url.StartsWith("browy://", StringComparison.OrdinalIgnoreCase) && !tab.Url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
                        {
                            AddHistoryEntry(tab.Title, tab.Url, tab.FaviconImage);
                        }
                    });

                    // Proactively resolve favicon for domain if not yet loaded
                    if (tab.FaviconImage == null && !string.IsNullOrWhiteSpace(tab.Url) && !tab.Url.StartsWith("browy://", StringComparison.OrdinalIgnoreCase))
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
                        string currentSrc = tab.WebViewInstance.Source?.ToString() ?? "";
                        if (!tab.Url.StartsWith("browy://", StringComparison.OrdinalIgnoreCase))
                        {
                            tab.Url = currentSrc;
                        }
                        if (_activeTab == tab)
                        {
                            UpdateNavigationUI(tab);
                        }
                    });
                };

                tab.WebViewInstance.ZoomFactorChanged += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (_activeTab == tab)
                        {
                            UpdateZoomUI();
                        }
                    });
                };

                // Navigate to initial target
                if (!string.IsNullOrEmpty(initialUrl))
                {
                    if (initialUrl.StartsWith("browy://settings", StringComparison.OrdinalIgnoreCase) ||
                        initialUrl.StartsWith("about:settings", StringComparison.OrdinalIgnoreCase))
                    {
                        tab.Url = "browy://settings";
                        tab.Title = "Settings - Browy";
                        tab.FallbackIcon = "\uE713";
                        tab.WebViewInstance.NavigateToString(SettingsPageService.GetSettingsHtml());
                    }
                    else if (initialUrl.StartsWith("browy://history", StringComparison.OrdinalIgnoreCase) ||
                             initialUrl.StartsWith("about:history", StringComparison.OrdinalIgnoreCase))
                    {
                        tab.Url = "browy://history";
                        tab.Title = "History - Browy";
                        tab.FallbackIcon = "\uE81C";
                        tab.WebViewInstance.NavigateToString(InternalHubService.GetHistoryHtml(History));
                    }
                    else if (initialUrl.StartsWith("browy://bookmarks", StringComparison.OrdinalIgnoreCase) ||
                             initialUrl.StartsWith("about:bookmarks", StringComparison.OrdinalIgnoreCase))
                    {
                        tab.Url = "browy://bookmarks";
                        tab.Title = "Bookmarks - Browy";
                        tab.FallbackIcon = "\uE734";
                        tab.WebViewInstance.NavigateToString(InternalHubService.GetBookmarksHtml(Bookmarks));
                    }
                    else if (initialUrl.StartsWith("browy://downloads", StringComparison.OrdinalIgnoreCase) ||
                             initialUrl.StartsWith("about:downloads", StringComparison.OrdinalIgnoreCase))
                    {
                        tab.Url = "browy://downloads";
                        tab.Title = "Downloads - Browy";
                        tab.FallbackIcon = "\uE896";
                        tab.WebViewInstance.NavigateToString(InternalHubService.GetDownloadsHtml(Downloads));
                    }
                    else if (initialUrl.StartsWith("browy://extensions", StringComparison.OrdinalIgnoreCase) ||
                             initialUrl.StartsWith("about:extensions", StringComparison.OrdinalIgnoreCase))
                    {
                        tab.Url = "browy://extensions";
                        tab.Title = "Extensions - Browy";
                        tab.FallbackIcon = "\uE74C";
                        tab.WebViewInstance.NavigateToString(InternalHubService.GetExtensionsHtml(ExtensionService.Extensions));
                    }
                    else if (initialUrl.StartsWith("browy://version", StringComparison.OrdinalIgnoreCase) ||
                             initialUrl.StartsWith("browy://about", StringComparison.OrdinalIgnoreCase) ||
                             initialUrl.StartsWith("about:version", StringComparison.OrdinalIgnoreCase))
                    {
                        tab.Url = "browy://version";
                        tab.Title = "About - Browy";
                        tab.FallbackIcon = "\uE946";
                        tab.WebViewInstance.NavigateToString(InternalHubService.GetAboutHtml(Tabs.Count, Spaces.Count));
                    }
                    else if (initialUrl.StartsWith("browy://shortcuts", StringComparison.OrdinalIgnoreCase) ||
                             initialUrl.StartsWith("about:shortcuts", StringComparison.OrdinalIgnoreCase))
                    {
                        tab.Url = "browy://shortcuts";
                        tab.Title = "Shortcuts - Browy";
                        tab.FallbackIcon = "\uE765";
                        tab.WebViewInstance.NavigateToString(InternalHubService.GetShortcutsHtml(BrowserSettings.Current.CustomKeyBindings));
                    }
                    else if (initialUrl.StartsWith("browy://newtab", StringComparison.OrdinalIgnoreCase) ||
                             initialUrl.StartsWith("browy://start", StringComparison.OrdinalIgnoreCase) ||
                             initialUrl.StartsWith("about:newtab", StringComparison.OrdinalIgnoreCase) ||
                             initialUrl.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
                    {
                        tab.Url = "browy://newtab";
                        tab.Title = "New Tab";
                        tab.FallbackIcon = "\uE72B";
                        tab.WebViewInstance.NavigateToString(NewTabPageService.GetNewTabHtml());
                    }
                    else if (tab.WebViewInstance.CoreWebView2 != null)
                    {
                        tab.WebViewInstance.CoreWebView2.Navigate(initialUrl);
                    }
                    else
                    {
                        tab.WebViewInstance.Source = new Uri(initialUrl);
                    }
                }

                ClipWebViewToRoundedCorners(tab.WebViewInstance);
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(@"E:\browy\crash.log", $"[{DateTime.Now:O}] InitializeWebViewAsync failed ({initialUrl}): {ex}\n");
                }
                catch { }

                tab.Title = "Navigation error";
                if (tab.WebViewInstance?.CoreWebView2 != null)
                {
                    try
                    {
                        tab.WebViewInstance.NavigateToString(SettingsPageService.GetErrorPageHtml(initialUrl, "InitializationFailed", ex.Message));
                    }
                    catch { }
                }
            }
        }

        public void SwitchToTab(BrowserTab tab)
        {
            if (_activeTab == tab) return;

            foreach (var t in Tabs)
            {
                t.IsActive = (t == tab);
            }

            _activeTab = tab;
            ApplySplitLayout();
            if (tab.IsSleeping)
            {
                WakeTab(tab);
            }
            if (tab.WebViewInstance != null)
            {
                double savedZoom = BrowserSettings.GetSiteZoomLevel(tab.Url);
                if (Math.Abs(tab.WebViewInstance.ZoomFactor - savedZoom) > 0.01)
                {
                    tab.WebViewInstance.ZoomFactor = savedZoom;
                }
            }
            UpdateNavigationUI(tab);
            StartPageGrid.Visibility = Visibility.Collapsed;

            if (FindPopup.IsOpen)
            {
                if (!string.IsNullOrWhiteSpace(FindTextBox.Text))
                {
                    FindActiveTab(FindTextBox.Text, _isFindMatchCase);
                }
                else
                {
                    UpdateFindMatchUI(0, 0);
                }
            }

            Dispatcher.InvokeAsync(() =>
            {
                ClipWebViewToRoundedCorners(tab.WebViewInstance);
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public void CloseTab(BrowserTab tab)
        {
            int index = Tabs.IndexOf(tab);
            if (index < 0) return;

            if (!string.IsNullOrWhiteSpace(tab.Url) && !tab.Url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            {
                _recentlyClosedUrls.Push(tab.Url);
                RecentlyClosedTabs.Insert(0, new ClosedTabItem(tab.Title, tab.Url, tab.FaviconImage));
                if (RecentlyClosedTabs.Count > 20)
                {
                    RecentlyClosedTabs.RemoveAt(RecentlyClosedTabs.Count - 1);
                }
                UpdateHistoryEmptyState();
            }

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
                    SecurityIconText.Foreground = (Brush)FindResource("TextPrimaryBrush");
                    BackButton.IsEnabled = false;
                    ForwardButton.IsEnabled = false;
                    ReloadButton.IsEnabled = false;
                    UpdateZoomUI();
                }
            }

            UpdateBookmarkStarUI();
        }

        private void UpdateNavigationUI(BrowserTab tab)
        {
            UrlTextBox.Text = tab.Url;
            bool isSecure = NavigationHelper.IsSecure(tab.Url);
            SecurityIconText.Text = isSecure ? "\uE72E" : "\uE7BA";
            SecurityIconText.Foreground = isSecure
                ? new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81))
                : new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));

            BackButton.IsEnabled = tab.CanGoBack;
            ForwardButton.IsEnabled = tab.CanGoForward;
            ReloadButton.IsEnabled = true;

            ReloadIconText.Text = tab.IsLoading ? "\uE711" : "\uE72C";
            PageProgressBar.Visibility = tab.IsLoading ? Visibility.Visible : Visibility.Collapsed;

            UpdateBookmarkStarUI();
            UpdateZoomUI();
            UpdateReaderModeUI(tab);
            UpdateShieldUI(tab);
        }

        private void UpdateShieldUI(BrowserTab tab)
        {
            if (ShieldButton == null || ShieldIconText == null || ShieldBadgeText == null) return;

            bool isInternal = tab.Url.StartsWith("browy://", StringComparison.OrdinalIgnoreCase);
            ShieldButton.Visibility = isInternal ? Visibility.Collapsed : Visibility.Visible;

            if (!BrowserSettings.Current.IsShieldEnabled)
            {
                ShieldIconText.Foreground = new SolidColorBrush(Color.FromArgb(0x70, 0xFF, 0xFF, 0xFF));
                ShieldBadgeText.Visibility = Visibility.Collapsed;
                ShieldButton.ToolTip = "Browy Shield: Paused (Click to enable)";
            }
            else if (tab.BlockedCount > 0)
            {
                ShieldIconText.Foreground = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                ShieldBadgeText.Text = tab.BlockedCount > 99 ? "99+" : tab.BlockedCount.ToString();
                ShieldBadgeText.Visibility = Visibility.Visible;
                ShieldButton.ToolTip = $"Browy Shield: {tab.BlockedCount} ads & trackers blocked (Click to toggle)";
            }
            else
            {
                ShieldIconText.Foreground = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                ShieldBadgeText.Visibility = Visibility.Collapsed;
                ShieldButton.ToolTip = "Browy Shield: Active (Protected)";
            }
        }

        public void ToggleShield()
        {
            BrowserSettings.Current.IsShieldEnabled = !BrowserSettings.Current.IsShieldEnabled;
            BrowserSettings.Save();
            if (_activeTab != null)
            {
                UpdateShieldUI(_activeTab);
                ReloadCurrentPage();
            }
        }

        private void ShieldButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleShield();
        }

        private void ReaderModeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleReaderMode();
        }

        public void ToggleReaderMode()
        {
            _ = ToggleReaderModeAsync();
        }

        public async Task ToggleReaderModeAsync()
        {
            if (_activeTab == null) return;

            if (_activeTab.IsReaderModeActive)
            {
                ExitReaderMode(_activeTab);
            }
            else
            {
                await EnterReaderModeAsync(_activeTab);
            }
        }

        public void ExitReaderMode(BrowserTab tab)
        {
            tab.IsReaderModeActive = false;
            UpdateReaderModeUI(tab);

            if (!string.IsNullOrWhiteSpace(tab.OriginalUrlBeforeReader))
            {
                string target = tab.OriginalUrlBeforeReader;
                tab.OriginalUrlBeforeReader = null;
                tab.Url = target;
                if (tab.WebViewInstance?.CoreWebView2 != null)
                {
                    tab.WebViewInstance.CoreWebView2.Navigate(target);
                }
            }
            else if (tab.WebViewInstance != null && tab.WebViewInstance.CanGoBack)
            {
                tab.WebViewInstance.GoBack();
            }
        }

        public async Task EnterReaderModeAsync(BrowserTab tab)
        {
            if (tab.WebViewInstance?.CoreWebView2 == null) return;
            if (tab.Url.StartsWith("browy://", StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                tab.OriginalUrlBeforeReader = tab.Url;
                string json = await tab.WebViewInstance.CoreWebView2.ExecuteScriptAsync(ReaderModeService.ExtractionScript);
                var article = ReaderModeService.ParseExtractionResult(json);
                if (article != null && !string.IsNullOrWhiteSpace(article.Content))
                {
                    string readerHtml = ReaderModeService.GenerateReaderHtml(article);
                    tab.IsReaderModeActive = true;
                    UpdateReaderModeUI(tab);
                    tab.WebViewInstance.NavigateToString(readerHtml);
                }
            }
            catch (Exception ex)
            {
                try { File.AppendAllText(@"E:\browy\crash.log", $"[{DateTime.Now:O}] EnterReaderModeAsync error: {ex}\n"); } catch { }
            }
        }

        private void UpdateReaderModeUI(BrowserTab tab)
        {
            if (ReaderModeIconText == null || ReaderModeButton == null) return;

            if (tab.IsReaderModeActive)
            {
                ReaderModeIconText.Text = "\uE737";
                ReaderModeIconText.Foreground = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                ReaderModeButton.ToolTip = "Exit Reader Mode (Ctrl+Shift+R, F9)";
                ReaderModeButton.Visibility = Visibility.Visible;
            }
            else
            {
                ReaderModeIconText.Text = "\uE736";
                ReaderModeIconText.Foreground = tab.CanEnterReaderMode
                    ? new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xF9))
                    : new SolidColorBrush(Color.FromArgb(0x70, 0xFF, 0xFF, 0xFF));
                ReaderModeButton.ToolTip = "Toggle Reader Mode (Ctrl+Shift+R, F9)";
                ReaderModeButton.Visibility = tab.Url.StartsWith("browy://", StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        private async Task DetectReaderModeAsync(BrowserTab tab)
        {
            if (tab.WebViewInstance?.CoreWebView2 == null) return;
            try
            {
                string res = await tab.WebViewInstance.CoreWebView2.ExecuteScriptAsync(ReaderModeService.ArticleDetectionScript);
                bool canRead = bool.TryParse(res, out bool b) && b;
                tab.CanEnterReaderMode = canRead;
                if (_activeTab == tab)
                {
                    Dispatcher.Invoke(() => UpdateReaderModeUI(tab));
                }
            }
            catch { }
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
            if (RecentlyClosedSection != null)
            {
                RecentlyClosedSection.Visibility = RecentlyClosedTabs.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (HistorySectionHeader != null)
            {
                HistorySectionHeader.Visibility = (RecentlyClosedTabs.Count > 0 && History.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (HistoryEmptyText != null)
            {
                HistoryEmptyText.Visibility = (History.Count == 0 && RecentlyClosedTabs.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void RecentlyClosedItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is ClosedTabItem item)
            {
                RecentlyClosedTabs.Remove(item);
                HistoryPopup.IsOpen = false;
                _ = CreateNewTabAsync(item.Url, switchTo: true);
                UpdateHistoryEmptyState();
            }
        }

        private async void RestoreAllClosedTabs_Click(object sender, RoutedEventArgs e)
        {
            var items = RecentlyClosedTabs.ToList();
            RecentlyClosedTabs.Clear();
            _recentlyClosedUrls.Clear();
            HistoryPopup.IsOpen = false;
            UpdateHistoryEmptyState();

            foreach (var item in items)
            {
                await CreateNewTabAsync(item.Url, switchTo: false);
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

        public void NavigateActiveTab(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            string targetUrl = NavigationHelper.ResolveInputToUrl(input);

            if (_activeTab == null)
            {
                _ = CreateNewTabAsync(targetUrl, switchTo: true);
                return;
            }

            if (targetUrl.StartsWith("browy://settings", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.StartsWith("about:settings", StringComparison.OrdinalIgnoreCase))
            {
                _activeTab.Url = "browy://settings";
                _activeTab.Title = "Settings - Browy";
                _activeTab.FallbackIcon = "\uE713";
                UpdateNavigationUI(_activeTab);
                _activeTab.WebViewInstance?.NavigateToString(SettingsPageService.GetSettingsHtml());
                return;
            }

            if (targetUrl.StartsWith("browy://history", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.StartsWith("about:history", StringComparison.OrdinalIgnoreCase))
            {
                _activeTab.Url = "browy://history";
                _activeTab.Title = "History - Browy";
                _activeTab.FallbackIcon = "\uE81C";
                UpdateNavigationUI(_activeTab);
                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetHistoryHtml(History));
                return;
            }

            if (targetUrl.StartsWith("browy://bookmarks", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.StartsWith("about:bookmarks", StringComparison.OrdinalIgnoreCase))
            {
                _activeTab.Url = "browy://bookmarks";
                _activeTab.Title = "Bookmarks - Browy";
                _activeTab.FallbackIcon = "\uE734";
                UpdateNavigationUI(_activeTab);
                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetBookmarksHtml(Bookmarks));
                return;
            }

            if (targetUrl.StartsWith("browy://downloads", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.StartsWith("about:downloads", StringComparison.OrdinalIgnoreCase))
            {
                _activeTab.Url = "browy://downloads";
                _activeTab.Title = "Downloads - Browy";
                _activeTab.FallbackIcon = "\uE896";
                UpdateNavigationUI(_activeTab);
                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetDownloadsHtml(Downloads));
                return;
            }

            if (targetUrl.StartsWith("browy://extensions", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.StartsWith("about:extensions", StringComparison.OrdinalIgnoreCase))
            {
                _activeTab.Url = "browy://extensions";
                _activeTab.Title = "Extensions - Browy";
                _activeTab.FallbackIcon = "\uE74C";
                UpdateNavigationUI(_activeTab);
                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetExtensionsHtml(ExtensionService.Extensions));
                return;
            }

            if (targetUrl.StartsWith("browy://version", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.StartsWith("browy://about", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.StartsWith("about:version", StringComparison.OrdinalIgnoreCase))
            {
                _activeTab.Url = "browy://version";
                _activeTab.Title = "About - Browy";
                _activeTab.FallbackIcon = "\uE946";
                UpdateNavigationUI(_activeTab);
                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetAboutHtml(Tabs.Count, Spaces.Count));
                return;
            }

            if (targetUrl.StartsWith("browy://shortcuts", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.StartsWith("about:shortcuts", StringComparison.OrdinalIgnoreCase))
            {
                _activeTab.Url = "browy://shortcuts";
                _activeTab.Title = "Shortcuts - Browy";
                _activeTab.FallbackIcon = "\uE765";
                UpdateNavigationUI(_activeTab);
                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetShortcutsHtml(BrowserSettings.Current.CustomKeyBindings));
                return;
            }

            if (targetUrl.StartsWith("browy://newtab", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.StartsWith("browy://start", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.StartsWith("about:newtab", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
            {
                _activeTab.Url = "browy://newtab";
                _activeTab.Title = "New Tab";
                _activeTab.FallbackIcon = "\uE72B";
                UpdateNavigationUI(_activeTab);
                _activeTab.WebViewInstance?.NavigateToString(NewTabPageService.GetNewTabHtml());
                return;
            }

            if (_activeTab.WebViewInstance?.CoreWebView2 != null)
            {
                _activeTab.WebViewInstance.CoreWebView2.Navigate(targetUrl);
            }
            else if (_activeTab.WebViewInstance != null)
            {
                _ = Task.Run(async () =>
                {
                    await InitializeWebViewAsync(_activeTab, targetUrl);
                });
            }
        }

        private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OmniboxSuggestionsPopup.IsOpen = false;
                NavigateActiveTab(UrlTextBox.Text);
                FocusManager.SetFocusedElement(this, null);
                Keyboard.ClearFocus();
            }
        }

        private async void UrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (UrlPlaceholderText != null)
            {
                UrlPlaceholderText.Visibility = string.IsNullOrEmpty(UrlTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (!UrlTextBox.IsFocused)
            {
                return;
            }

            string query = UrlTextBox.Text;
            if (string.IsNullOrWhiteSpace(query))
            {
                OmniboxSuggestionsPopup.IsOpen = false;
                OmniboxSuggestions.Clear();
                return;
            }

            _suggestionCts?.Cancel();
            _suggestionCts = new CancellationTokenSource();
            var token = _suggestionCts.Token;

            try
            {
                await Task.Delay(120, token);
                var suggestions = await SuggestionService.GetSuggestionsAsync(query, History, Bookmarks, token);
                if (token.IsCancellationRequested) return;

                OmniboxSuggestions.Clear();
                foreach (var s in suggestions)
                {
                    OmniboxSuggestions.Add(s);
                }

                OmniboxSuggestionsPopup.IsOpen = OmniboxSuggestions.Count > 0;
            }
            catch (OperationCanceledException) { }
            catch { }
        }

        private void UrlTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && OmniboxSuggestionsPopup.IsOpen && OmniboxSuggestions.Count > 0)
            {
                e.Handled = true;
                OmniboxSuggestionsListBox.Focus();
                OmniboxSuggestionsListBox.SelectedIndex = 0;
            }
            else if (e.Key == Key.Escape && OmniboxSuggestionsPopup.IsOpen)
            {
                e.Handled = true;
                OmniboxSuggestionsPopup.IsOpen = false;
            }
        }

        private void OmniboxSuggestionsListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                ExecuteSelectedSuggestion();
            }
            else if (e.Key == Key.Up && OmniboxSuggestionsListBox.SelectedIndex == 0)
            {
                e.Handled = true;
                OmniboxSuggestionsListBox.SelectedIndex = -1;
                UrlTextBox.Focus();
                UrlTextBox.CaretIndex = UrlTextBox.Text.Length;
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                OmniboxSuggestionsPopup.IsOpen = false;
                UrlTextBox.Focus();
            }
        }

        private void OmniboxSuggestionsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ExecuteSelectedSuggestion();
        }

        private void OmniboxSuggestionsListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dep)
            {
                var item = FindVisualAncestor<ListBoxItem>(dep);
                if (item?.DataContext is OmniboxSuggestion suggestion)
                {
                    OmniboxSuggestionsListBox.SelectedItem = suggestion;
                    ExecuteSelectedSuggestion();
                }
            }
        }

        private void ExecuteSelectedSuggestion()
        {
            if (OmniboxSuggestionsListBox.SelectedItem is OmniboxSuggestion suggestion)
            {
                OmniboxSuggestionsPopup.IsOpen = false;
                if (!string.IsNullOrWhiteSpace(suggestion.Url))
                {
                    NavigateActiveTab(suggestion.Url);
                }
                else
                {
                    NavigateActiveTab(suggestion.Title);
                }
                FocusManager.SetFocusedElement(this, null);
                Keyboard.ClearFocus();
            }
        }

        private void SecurityButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab == null || string.IsNullOrWhiteSpace(_activeTab.Url))
            {
                SecurityPopup.IsOpen = false;
                return;
            }

            try
            {
                var uri = new Uri(_activeTab.Url);
                SecurityStatusDomain.Text = uri.Host;
                bool isHttps = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
                if (isHttps)
                {
                    SecurityDialogIcon.Text = "\uE72E";
                    SecurityDialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
                    SecurityStatusTitle.Text = "Connection is secure";
                    SecurityCertText.Text = "Valid SSL Certificate (HTTPS encryption active)";
                }
                else
                {
                    SecurityDialogIcon.Text = "\uE7BA";
                    SecurityDialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                    SecurityStatusTitle.Text = "Connection is not secure";
                    SecurityCertText.Text = "You should not enter sensitive info on this site (unencrypted HTTP).";
                }
            }
            catch
            {
                SecurityStatusDomain.Text = _activeTab.Url;
                SecurityStatusTitle.Text = "Site Information";
                SecurityCertText.Text = "Local or special page";
            }

            SecurityPopup.IsOpen = !SecurityPopup.IsOpen;
        }

        private async void ClearSiteData_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab?.WebViewInstance?.CoreWebView2?.Profile != null)
            {
                try
                {
                    await _activeTab.WebViewInstance.CoreWebView2.Profile.ClearBrowsingDataAsync(
                        CoreWebView2BrowsingDataKinds.Cookies |
                        CoreWebView2BrowsingDataKinds.CacheStorage |
                        CoreWebView2BrowsingDataKinds.IndexedDb |
                        CoreWebView2BrowsingDataKinds.LocalStorage);
                    SecurityPopup.IsOpen = false;
                    _activeTab.WebViewInstance.Reload();
                }
                catch { }
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
                if (_activeTab.Url.StartsWith("browy://settings", StringComparison.OrdinalIgnoreCase))
                {
                    _activeTab.WebViewInstance.NavigateToString(SettingsPageService.GetSettingsHtml());
                    return;
                }

                if (_activeTab.Url.StartsWith("browy://history", StringComparison.OrdinalIgnoreCase) ||
                    _activeTab.Url.StartsWith("about:history", StringComparison.OrdinalIgnoreCase))
                {
                    _activeTab.WebViewInstance.NavigateToString(InternalHubService.GetHistoryHtml(History));
                    return;
                }

                if (_activeTab.Url.StartsWith("browy://bookmarks", StringComparison.OrdinalIgnoreCase) ||
                    _activeTab.Url.StartsWith("about:bookmarks", StringComparison.OrdinalIgnoreCase))
                {
                    _activeTab.WebViewInstance.NavigateToString(InternalHubService.GetBookmarksHtml(Bookmarks));
                    return;
                }

                if (_activeTab.Url.StartsWith("browy://downloads", StringComparison.OrdinalIgnoreCase) ||
                    _activeTab.Url.StartsWith("about:downloads", StringComparison.OrdinalIgnoreCase))
                {
                    _activeTab.WebViewInstance.NavigateToString(InternalHubService.GetDownloadsHtml(Downloads));
                    return;
                }

                if (_activeTab.Url.StartsWith("browy://extensions", StringComparison.OrdinalIgnoreCase) ||
                    _activeTab.Url.StartsWith("about:extensions", StringComparison.OrdinalIgnoreCase))
                {
                    _activeTab.WebViewInstance.NavigateToString(InternalHubService.GetExtensionsHtml(ExtensionService.Extensions));
                    return;
                }

                if (_activeTab.Url.StartsWith("browy://version", StringComparison.OrdinalIgnoreCase) ||
                    _activeTab.Url.StartsWith("browy://about", StringComparison.OrdinalIgnoreCase) ||
                    _activeTab.Url.StartsWith("about:version", StringComparison.OrdinalIgnoreCase))
                {
                    _activeTab.WebViewInstance.NavigateToString(InternalHubService.GetAboutHtml(Tabs.Count, Spaces.Count));
                    return;
                }

                if (_activeTab.Url.StartsWith("browy://shortcuts", StringComparison.OrdinalIgnoreCase) ||
                    _activeTab.Url.StartsWith("about:shortcuts", StringComparison.OrdinalIgnoreCase))
                {
                    _activeTab.WebViewInstance.NavigateToString(InternalHubService.GetShortcutsHtml(BrowserSettings.Current.CustomKeyBindings));
                    return;
                }

                if (_activeTab.Url.StartsWith("browy://newtab", StringComparison.OrdinalIgnoreCase) ||
                    _activeTab.Url.StartsWith("browy://start", StringComparison.OrdinalIgnoreCase) ||
                    _activeTab.Url.StartsWith("about:newtab", StringComparison.OrdinalIgnoreCase) ||
                    _activeTab.Url.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
                {
                    _activeTab.WebViewInstance.NavigateToString(NewTabPageService.GetNewTabHtml());
                    return;
                }

                if (_activeTab.WebViewInstance.CoreWebView2 != null)
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
                else if (!string.IsNullOrEmpty(_activeTab.Url))
                {
                    NavigateActiveTab(_activeTab.Url);
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

        #region Command Palette & Browser Actions

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSettingsPage();
        }

        public void OpenSettingsPage()
        {
            var existingSettingsTab = Tabs.FirstOrDefault(t => t.Url.StartsWith("browy://settings", StringComparison.OrdinalIgnoreCase));
            if (existingSettingsTab != null)
            {
                SwitchToTab(existingSettingsTab);
            }
            else
            {
                _ = CreateNewTabAsync("browy://settings", switchTo: true);
            }
        }

        private void CommandPaletteButton_Click(object sender, RoutedEventArgs e)
        {
            OpenCommandPalette();
        }

        public void FocusActiveContent()
        {
            try
            {
                if (WindowState != WindowState.Minimized)
                {
                    Activate();
                    Focus();
                    if (_activeTab?.WebViewInstance != null)
                    {
                        _activeTab.WebViewInstance.Focus();
                    }
                }
            }
            catch { }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (WindowState != WindowState.Minimized)
                    {
                        Activate();
                        Focus();
                        if (_activeTab?.WebViewInstance != null)
                        {
                            _activeTab.WebViewInstance.Focus();
                        }
                    }
                }
                catch { }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        public void OpenCommandPalette()
        {
            if (_commandPalette != null && _commandPalette.IsLoaded)
            {
                _commandPalette.Activate();
                return;
            }

            _commandPalette = new CommandPaletteWindow(this);
            _commandPalette.Closed += (s, e) =>
            {
                _commandPalette = null;
                FocusActiveContent();
            };
            _commandPalette.Show();
        }

        public void AddNewTab(string? url = null)
        {
            _ = CreateNewTabAsync(string.IsNullOrEmpty(url) ? BrowserSettings.Current.StartPageUrl : url, switchTo: true);
        }

        public void CloseCurrentTab()
        {
            if (_activeTab != null)
            {
                CloseTab(_activeTab);
            }
        }

        public void ReopenClosedTab()
        {
            if (RecentlyClosedTabs.Count > 0)
            {
                var item = RecentlyClosedTabs[0];
                RecentlyClosedTabs.RemoveAt(0);
                if (_recentlyClosedUrls.Count > 0) _recentlyClosedUrls.Pop();
                _ = CreateNewTabAsync(item.Url, switchTo: true);
                UpdateHistoryEmptyState();
            }
            else if (_recentlyClosedUrls.Count > 0)
            {
                string url = _recentlyClosedUrls.Pop();
                _ = CreateNewTabAsync(url, switchTo: true);
            }
        }

        public void DuplicateCurrentTab()
        {
            if (_activeTab != null && !string.IsNullOrEmpty(_activeTab.Url))
            {
                _ = CreateNewTabAsync(_activeTab.Url, switchTo: true);
            }
        }

        public void SelectNextTab()
        {
            if (Tabs.Count > 1 && _activeTab != null)
            {
                int idx = Tabs.IndexOf(_activeTab);
                SwitchToTab(Tabs[(idx + 1) % Tabs.Count]);
            }
        }

        public void SelectPreviousTab()
        {
            if (Tabs.Count > 1 && _activeTab != null)
            {
                int idx = Tabs.IndexOf(_activeTab);
                SwitchToTab(Tabs[(idx - 1 + Tabs.Count) % Tabs.Count]);
            }
        }

        public void GoBack()
        {
            if (_activeTab?.WebViewInstance?.CoreWebView2 != null && _activeTab.WebViewInstance.CoreWebView2.CanGoBack)
                _activeTab.WebViewInstance.CoreWebView2.GoBack();
        }

        public void GoForward()
        {
            if (_activeTab?.WebViewInstance?.CoreWebView2 != null && _activeTab.WebViewInstance.CoreWebView2.CanGoForward)
                _activeTab.WebViewInstance.CoreWebView2.GoForward();
        }

        public void ReloadCurrentPage()
        {
            ReloadButton_Click(this, new RoutedEventArgs());
        }

        public void HardReloadCurrentPage()
        {
            if (_activeTab?.Url.StartsWith("browy://settings", StringComparison.OrdinalIgnoreCase) == true)
            {
                _activeTab.WebViewInstance?.NavigateToString(SettingsPageService.GetSettingsHtml());
                return;
            }

            if (_activeTab?.Url.StartsWith("browy://history", StringComparison.OrdinalIgnoreCase) == true ||
                _activeTab?.Url.StartsWith("about:history", StringComparison.OrdinalIgnoreCase) == true)
            {
                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetHistoryHtml(History));
                return;
            }

            if (_activeTab?.Url.StartsWith("browy://bookmarks", StringComparison.OrdinalIgnoreCase) == true ||
                _activeTab?.Url.StartsWith("about:bookmarks", StringComparison.OrdinalIgnoreCase) == true)
            {
                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetBookmarksHtml(Bookmarks));
                return;
            }

            if (_activeTab?.Url.StartsWith("browy://downloads", StringComparison.OrdinalIgnoreCase) == true ||
                _activeTab?.Url.StartsWith("about:downloads", StringComparison.OrdinalIgnoreCase) == true)
            {
                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetDownloadsHtml(Downloads));
                return;
            }

            if (_activeTab?.Url.StartsWith("browy://extensions", StringComparison.OrdinalIgnoreCase) == true ||
                _activeTab?.Url.StartsWith("about:extensions", StringComparison.OrdinalIgnoreCase) == true)
            {
                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetExtensionsHtml(ExtensionService.Extensions));
                return;
            }

            if (_activeTab?.Url.StartsWith("browy://version", StringComparison.OrdinalIgnoreCase) == true ||
                _activeTab?.Url.StartsWith("browy://about", StringComparison.OrdinalIgnoreCase) == true ||
                _activeTab?.Url.StartsWith("about:version", StringComparison.OrdinalIgnoreCase) == true)
            {
                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetAboutHtml(Tabs.Count, Spaces.Count));
                return;
            }

            if (_activeTab?.Url.StartsWith("browy://shortcuts", StringComparison.OrdinalIgnoreCase) == true ||
                _activeTab?.Url.StartsWith("about:shortcuts", StringComparison.OrdinalIgnoreCase) == true)
            {
                _activeTab.WebViewInstance?.NavigateToString(InternalHubService.GetShortcutsHtml(BrowserSettings.Current.CustomKeyBindings));
                return;
            }

            if (_activeTab?.Url.StartsWith("browy://newtab", StringComparison.OrdinalIgnoreCase) == true ||
                _activeTab?.Url.StartsWith("browy://start", StringComparison.OrdinalIgnoreCase) == true ||
                _activeTab?.Url.StartsWith("about:newtab", StringComparison.OrdinalIgnoreCase) == true ||
                _activeTab?.Url.Equals("about:blank", StringComparison.OrdinalIgnoreCase) == true)
            {
                _activeTab.WebViewInstance?.NavigateToString(NewTabPageService.GetNewTabHtml());
                return;
            }

            if (_activeTab?.WebViewInstance?.CoreWebView2 != null)
            {
                _activeTab.WebViewInstance.CoreWebView2.ExecuteScriptAsync("location.reload(true);");
            }
            else if (_activeTab != null && !string.IsNullOrEmpty(_activeTab.Url))
            {
                NavigateActiveTab(_activeTab.Url);
            }
        }

        public void OpenStartPage()
        {
            NavigateActiveTab("browy://newtab");
        }

        public void FocusAddressBar()
        {
            UrlTextBox.Focus();
            UrlTextBox.SelectAll();
        }

        public void CopyCurrentUrl()
        {
            if (_activeTab != null && !string.IsNullOrEmpty(_activeTab.Url))
            {
                Clipboard.SetText(_activeTab.Url);
            }
        }

        public void ToggleSidebar()
        {
            SetSidebarCollapsed(!_isSidebarCollapsed);
        }

        public void ToggleAgentsPanel()
        {
            ToggleAgentsPanel_Click(this, new RoutedEventArgs());
        }

        public void ToggleBookmarksBar()
        {
            bool isVisible = (BookmarksBar.Visibility != Visibility.Visible);
            BookmarksBar.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            BrowserSettings.Current.ShowBookmarksBar = isVisible;
            BrowserSettings.Save();
        }

        public void ZoomIn()
        {
            if (_activeTab?.WebViewInstance != null)
            {
                double z = Math.Min(3.0, _activeTab.WebViewInstance.ZoomFactor + 0.1);
                _activeTab.WebViewInstance.ZoomFactor = Math.Round(z, 2);
                BrowserSettings.SetSiteZoomLevel(_activeTab.Url, _activeTab.WebViewInstance.ZoomFactor);
                UpdateZoomUI();
            }
        }

        public void ZoomOut()
        {
            if (_activeTab?.WebViewInstance != null)
            {
                double z = Math.Max(0.25, _activeTab.WebViewInstance.ZoomFactor - 0.1);
                _activeTab.WebViewInstance.ZoomFactor = Math.Round(z, 2);
                BrowserSettings.SetSiteZoomLevel(_activeTab.Url, _activeTab.WebViewInstance.ZoomFactor);
                UpdateZoomUI();
            }
        }

        public void ResetZoom()
        {
            if (_activeTab?.WebViewInstance != null)
            {
                _activeTab.WebViewInstance.ZoomFactor = 1.0;
                BrowserSettings.SetSiteZoomLevel(_activeTab.Url, 1.0);
                UpdateZoomUI();
            }
        }

        private void UpdateZoomUI()
        {
            if (_activeTab?.WebViewInstance == null)
            {
                ZoomIndicatorButton.Visibility = Visibility.Collapsed;
                ZoomPopupPercentageText.Text = "100%";
                return;
            }

            double factor = _activeTab.WebViewInstance.ZoomFactor;
            int percent = (int)Math.Round(factor * 100);
            ZoomIndicatorText.Text = $"{percent}%";
            ZoomPopupPercentageText.Text = $"{percent}%";

            ZoomIndicatorButton.Visibility = (Math.Abs(factor - 1.0) > 0.01) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ZoomIndicatorButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateZoomUI();
            ZoomPopup.IsOpen = !ZoomPopup.IsOpen;
        }

        private void ZoomInPopup_Click(object sender, RoutedEventArgs e)
        {
            ZoomIn();
        }

        private void ZoomOutPopup_Click(object sender, RoutedEventArgs e)
        {
            ZoomOut();
        }

        private void ResetZoomPopup_Click(object sender, RoutedEventArgs e)
        {
            ResetZoom();
            ZoomPopup.IsOpen = false;
        }

        public void OpenDevTools()
        {
            _activeTab?.WebViewInstance?.CoreWebView2?.OpenDevToolsWindow();
        }

        public void ViewPageSource()
        {
            if (_activeTab != null && !string.IsNullOrEmpty(_activeTab.Url))
            {
                _ = CreateNewTabAsync("view-source:" + _activeTab.Url, switchTo: true);
            }
        }

        public void PrintCurrentPage()
        {
            _activeTab?.WebViewInstance?.CoreWebView2?.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
        }

        public void BookmarkCurrentPage()
        {
            BookmarkCurrentPage_Click(this, new RoutedEventArgs());
        }

        public void ExportBackup()
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Browy Backup Bundle (*.json)|*.json",
                    FileName = $"browy_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };
                if (sfd.ShowDialog() == true)
                {
                    string json = BackupService.CreateBackupJson(Bookmarks, BrowserSettings.Current, Spaces, ActiveSpace, _activeTab);
                    File.WriteAllText(sfd.FileName, json);
                    System.Windows.MessageBox.Show("Backup successfully exported!", "Browy Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to export backup: {ex.Message}", "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ImportBackup()
        {
            try
            {
                var ofd = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Browy Backup Bundle (*.json)|*.json"
                };
                if (ofd.ShowDialog() == true)
                {
                    string json = File.ReadAllText(ofd.FileName);
                    var bundle = BackupService.RestoreBackupFromJson(json);
                    if (bundle != null)
                    {
                        if (bundle.Bookmarks?.Count > 0)
                        {
                            Bookmarks.Clear();
                            foreach (var b in bundle.Bookmarks)
                            {
                                Bookmarks.Add(new BookmarkItem(b.Title, b.Url));
                            }
                            BookmarkService.SaveBookmarks(Bookmarks);
                        }
                        System.Windows.MessageBox.Show("Backup successfully restored!", "Browy Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to import backup: {ex.Message}", "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExportBookmarksHtml()
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "HTML Bookmarks (*.html)|*.html",
                    FileName = "bookmarks.html"
                };
                if (sfd.ShowDialog() == true)
                {
                    string html = BookmarkService.ExportToHtml(Bookmarks);
                    File.WriteAllText(sfd.FileName, html);
                    System.Windows.MessageBox.Show("Bookmarks exported to HTML!", "Browy Bookmarks", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to export bookmarks: {ex.Message}", "Bookmarks Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ImportBookmarksHtml()
        {
            try
            {
                var ofd = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "HTML Bookmarks (*.html;*.htm)|*.html;*.htm"
                };
                if (ofd.ShowDialog() == true)
                {
                    string html = File.ReadAllText(ofd.FileName);
                    var imported = BookmarkService.ImportFromHtml(html);
                    if (imported.Count > 0)
                    {
                        foreach (var item in imported)
                        {
                            if (!Bookmarks.Any(b => b.Url.Equals(item.Url, StringComparison.OrdinalIgnoreCase)))
                            {
                                Bookmarks.Add(item);
                            }
                        }
                        BookmarkService.SaveBookmarks(Bookmarks);
                        System.Windows.MessageBox.Show($"Successfully imported {imported.Count} bookmarks!", "Browy Bookmarks", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to import bookmarks: {ex.Message}", "Bookmarks Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExportHistoryJson()
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON History (*.json)|*.json",
                    FileName = $"browy_history_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };
                if (sfd.ShowDialog() == true)
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(History, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(sfd.FileName, json);
                    System.Windows.MessageBox.Show("Browsing history exported to JSON!", "Browy History", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to export history: {ex.Message}", "History Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ToggleHistory()
        {
            HistoryPopup.IsOpen = !HistoryPopup.IsOpen;
            UpdateHistoryEmptyState();
        }

        public void ClearAllHistory()
        {
            ClearHistory_Click(this, new RoutedEventArgs());
        }

        public void StartNewAgentChat()
        {
            NewAgentChat_Click(this, new RoutedEventArgs());
        }

        public void TriggerAgentPrompt(string prompt)
        {
            if (!_isAgentsPanelOpen)
            {
                ToggleAgentsPanel_Click(this, new RoutedEventArgs());
            }
            AgentInputTextBox.Text = prompt;
            SendAgentMessage_Click(this, new RoutedEventArgs());
        }

        #region Downloads Manager

        private void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            e.Handled = true;

            Dispatcher.Invoke(() =>
            {
                var item = new DownloadItem(e.DownloadOperation);
                item.PropertyChanged += (s, ev) =>
                {
                    if (ev.PropertyName == nameof(DownloadItem.IsCompleted) && item.IsCompleted)
                    {
                        NotificationService.ShowToast("Download Complete", $"{item.FileName} has finished downloading.");
                    }
                };
                Downloads.Insert(0, item);
                UpdateDownloadsButtonBadge();
                UpdateDownloadsEmptyState();
                DownloadsPopup.IsOpen = true;
            });
        }

        private void UpdateDownloadsButtonBadge()
        {
            if (DownloadsBadge == null || DownloadsBadgeText == null) return;
            int inProgressCount = Downloads.Count(d => d.IsInProgress);
            if (inProgressCount > 0)
            {
                DownloadsBadgeText.Text = inProgressCount.ToString();
                DownloadsBadge.Visibility = Visibility.Visible;
            }
            else
            {
                DownloadsBadge.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateDownloadsEmptyState()
        {
            if (DownloadsEmptyText != null)
            {
                DownloadsEmptyText.Visibility = Downloads.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void DownloadsButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleDownloads();
        }

        public void ToggleDownloads()
        {
            DownloadsPopup.IsOpen = !DownloadsPopup.IsOpen;
            UpdateDownloadsEmptyState();
            UpdateDownloadsButtonBadge();
        }

        private void CloseDownloads_Click(object sender, RoutedEventArgs e)
        {
            DownloadsPopup.IsOpen = false;
        }

        private void OpenDownloadsFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (!Directory.Exists(downloadsPath))
                {
                    Directory.CreateDirectory(downloadsPath);
                }
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"\"{downloadsPath}\"") { UseShellExecute = true });
            }
            catch { }
        }

        private void ClearDownloads_Click(object sender, RoutedEventArgs e)
        {
            var nonActive = Downloads.Where(d => !d.IsInProgress).ToList();
            foreach (var item in nonActive)
            {
                Downloads.Remove(item);
            }
            UpdateDownloadsEmptyState();
            UpdateDownloadsButtonBadge();
        }

        private void OpenFileDownload_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is DownloadItem item)
            {
                item.OpenFile();
            }
        }

        private void OpenFolderDownload_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is DownloadItem item)
            {
                item.OpenFolder();
            }
        }

        private void CancelDownload_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is DownloadItem item)
            {
                item.Cancel();
                UpdateDownloadsButtonBadge();
            }
        }

        #endregion

        #region Find In Page

        private CustomPopupPlacement[] FindPopupPlacementCallback(Size popupSize, Size targetSize, Point offset)
        {
            double x = Math.Max(0, targetSize.Width - popupSize.Width - 18);
            double y = 12;
            return new[] { new CustomPopupPlacement(new Point(x, y), PopupPrimaryAxis.Horizontal) };
        }

        public void OpenFindInPage()
        {
            FindPopup.IsOpen = true;
            FindTextBox.Focus();
            FindTextBox.SelectAll();
            if (!string.IsNullOrWhiteSpace(FindTextBox.Text))
            {
                FindActiveTab(FindTextBox.Text, _isFindMatchCase);
            }
        }

        public void CloseFindInPage()
        {
            FindPopup.IsOpen = false;
            try
            {
                _activeTab?.WebViewInstance?.CoreWebView2?.Find?.Stop();
            }
            catch { }
            _activeTab?.WebViewInstance?.Focus();
        }

        private async void FindActiveTab(string term, bool matchCase)
        {
            if (_activeTab?.WebViewInstance?.CoreWebView2 == null)
            {
                UpdateFindMatchUI(0, 0);
                return;
            }

            var core = _activeTab.WebViewInstance.CoreWebView2;

            if (string.IsNullOrEmpty(term))
            {
                try
                {
                    core.Find?.Stop();
                }
                catch { }
                UpdateFindMatchUI(0, 0);
                return;
            }

            try
            {
                var options = core.Environment.CreateFindOptions();
                options.FindTerm = term;
                options.IsCaseSensitive = matchCase;
                options.ShouldHighlightAllMatches = true;
                options.SuppressDefaultFindDialog = true;

                await core.Find.StartAsync(options);
            }
            catch { }
        }

        private void UpdateFindMatchUI(int activeIndex, int totalMatches)
        {
            if (FindMatchCountText == null) return;

            if (string.IsNullOrEmpty(FindTextBox.Text))
            {
                FindMatchCountText.Text = "";
                return;
            }

            if (totalMatches == 0)
            {
                FindMatchCountText.Text = "0/0";
                FindMatchCountText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // #EF4444 red
            }
            else
            {
                int displayIndex = activeIndex > 0 ? activeIndex : 1;
                FindMatchCountText.Text = $"{displayIndex}/{totalMatches}";
                FindMatchCountText.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // #94A3B8 neutral
            }
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FindActiveTab(FindTextBox.Text, _isFindMatchCase);
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    FindPrevious_Click(this, new RoutedEventArgs());
                }
                else
                {
                    FindNext_Click(this, new RoutedEventArgs());
                }
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CloseFindInPage();
            }
        }

        private void FindNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _activeTab?.WebViewInstance?.CoreWebView2?.Find?.FindNext();
            }
            catch { }
        }

        private void FindPrevious_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _activeTab?.WebViewInstance?.CoreWebView2?.Find?.FindPrevious();
            }
            catch { }
        }

        private void FindMatchCase_Click(object sender, RoutedEventArgs e)
        {
            _isFindMatchCase = !_isFindMatchCase;
            FindMatchCaseText.Foreground = _isFindMatchCase
                ? new SolidColorBrush(Color.FromRgb(59, 130, 246))
                : new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));
            FindActiveTab(FindTextBox.Text, _isFindMatchCase);
        }

        private void FindClose_Click(object sender, RoutedEventArgs e)
        {
            CloseFindInPage();
        }

        #endregion

        #endregion

        private void UrlTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UrlTextBox.SelectAll();
        }

        #endregion

        #region Tab & Sidebar UI Interactions

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void TabItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dep && FindVisualParent<Button>(dep) != null)
            {
                return;
            }

            _tabDragStartPoint = e.GetPosition(this);
            _draggedTab = (sender as FrameworkElement)?.DataContext as BrowserTab;
            if (_draggedTab != null)
            {
                SwitchToTab(_draggedTab);
            }
        }

        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedTab == null || _isDraggingTab) return;

            Point currentPoint = e.GetPosition(this);
            Vector diff = _tabDragStartPoint - currentPoint;
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _isDraggingTab = true;
                try
                {
                    if (sender is DependencyObject dragSource)
                    {
                        DragDrop.DoDragDrop(dragSource, _draggedTab, DragDropEffects.Move);
                    }
                }
                finally
                {
                    _isDraggingTab = false;
                    _draggedTab = null;
                }
            }
        }

        private void TabItem_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(BrowserTab)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(BrowserTab)) is BrowserTab sourceTab &&
                sender is FrameworkElement elem && elem.DataContext is BrowserTab targetTab)
            {
                if (sourceTab != targetTab)
                {
                    int oldIndex = Tabs.IndexOf(sourceTab);
                    int newIndex = Tabs.IndexOf(targetTab);
                    if (oldIndex >= 0 && newIndex >= 0)
                    {
                        Tabs.Move(oldIndex, newIndex);
                    }
                }
                e.Handled = true;
            }
        }

        private void TabsContainer_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(BrowserTab)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TabsContainer_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(BrowserTab)) is BrowserTab sourceTab)
            {
                int oldIndex = Tabs.IndexOf(sourceTab);
                if (oldIndex >= 0 && oldIndex != Tabs.Count - 1)
                {
                    Tabs.Move(oldIndex, Tabs.Count - 1);
                }
                e.Handled = true;
            }
        }

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

        private void TabMuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is BrowserTab tab)
            {
                ToggleMuteTab(tab);
            }
            e.Handled = true;
        }

        private void TabItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && sender is FrameworkElement elem && elem.DataContext is BrowserTab tab)
            {
                CloseTab(tab);
                e.Handled = true;
            }
        }

        private void TabItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is BrowserTab tab)
            {
                ShowTabContextMenu(elem, tab);
                e.Handled = true;
            }
        }

        private void ShowTabContextMenu(FrameworkElement target, BrowserTab tab)
        {
            var cm = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromArgb(0xF4, 0x14, 0x14, 0x1A)),
                BorderBrush = (Brush)FindResource("GlassBorderBrush"),
                BorderThickness = new Thickness(1)
            };

            // Pin / Unpin
            var pinItem = new MenuItem
            {
                Header = tab.IsPinned ? "Unpin Tab" : "Pin Tab",
                Icon = new TextBlock { Style = (Style)FindResource("FluentIcon"), Text = "\uE718", FontSize = 11, Foreground = Brushes.White }
            };
            pinItem.Click += (s, args) => TogglePinTab(tab);
            cm.Items.Add(pinItem);

            // Mute / Unmute
            var muteItem = new MenuItem
            {
                Header = tab.IsMuted ? "Unmute Tab" : "Mute Tab",
                Icon = new TextBlock { Style = (Style)FindResource("FluentIcon"), Text = tab.IsMuted ? "\uE767" : "\uE74F", FontSize = 11, Foreground = Brushes.White }
            };
            muteItem.Click += (s, args) => ToggleMuteTab(tab);
            cm.Items.Add(muteItem);

            // Mute Other Tabs
            var muteOthers = new MenuItem
            {
                Header = "Mute Other Tabs",
                Icon = new TextBlock { Style = (Style)FindResource("FluentIcon"), Text = "\uE74F", FontSize = 11, Foreground = Brushes.White }
            };
            muteOthers.Click += (s, args) => MuteOtherTabs(tab);
            cm.Items.Add(muteOthers);

            // Snooze / Sleep Tab
            if (tab != _activeTab && !tab.IsPinned && !tab.IsPlayingAudio)
            {
                var snoozeItem = new MenuItem
                {
                    Header = tab.IsSleeping ? "Wake Tab" : "Snooze Tab (Save Memory)",
                    Icon = new TextBlock { Style = (Style)FindResource("FluentIcon"), Text = "\uE708", FontSize = 11, Foreground = Brushes.White }
                };
                snoozeItem.Click += (s, args) =>
                {
                    if (tab.IsSleeping) WakeTab(tab);
                    else SnoozeTab(tab);
                };
                cm.Items.Add(snoozeItem);
            }

            cm.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF)) });

            // Duplicate
            var dupItem = new MenuItem
            {
                Header = "Duplicate Tab",
                Icon = new TextBlock { Style = (Style)FindResource("FluentIcon"), Text = "\uE7C3", FontSize = 11, Foreground = Brushes.White }
            };
            dupItem.Click += (s, args) => _ = CreateNewTabAsync(tab.Url, switchTo: true);
            cm.Items.Add(dupItem);

            // Reload
            var reloadItem = new MenuItem
            {
                Header = "Reload Tab",
                Icon = new TextBlock { Style = (Style)FindResource("FluentIcon"), Text = "\uE72C", FontSize = 11, Foreground = Brushes.White }
            };
            reloadItem.Click += (s, args) => tab.WebViewInstance?.Reload();
            cm.Items.Add(reloadItem);

            // Inspect Element / DevTools
            var inspectItem = new MenuItem
            {
                Header = "Inspect Element (F12)",
                Icon = new TextBlock { Style = (Style)FindResource("FluentIcon"), Text = "\uEBE8", FontSize = 11, Foreground = Brushes.White }
            };
            inspectItem.Click += (s, args) => tab.WebViewInstance?.CoreWebView2?.OpenDevToolsWindow();
            cm.Items.Add(inspectItem);

            cm.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF)) });

            // Close Tab
            var closeItem = new MenuItem
            {
                Header = "Close Tab",
                Icon = new TextBlock { Style = (Style)FindResource("FluentIcon"), Text = "\uE711", FontSize = 10, Foreground = Brushes.White }
            };
            closeItem.Click += (s, args) => CloseTab(tab);
            cm.Items.Add(closeItem);

            // Close Others
            var closeOthers = new MenuItem { Header = "Close Other Tabs" };
            closeOthers.Click += (s, args) => CloseOtherTabs(tab);
            cm.Items.Add(closeOthers);

            // Close to the Left
            var closeLeft = new MenuItem { Header = "Close Tabs to the Left" };
            closeLeft.Click += (s, args) => CloseTabsToLeft(tab);
            cm.Items.Add(closeLeft);

            // Close to the Right
            var closeRight = new MenuItem { Header = "Close Tabs to the Right" };
            closeRight.Click += (s, args) => CloseTabsToRight(tab);
            cm.Items.Add(closeRight);

            cm.PlacementTarget = target;
            cm.IsOpen = true;
        }

        public void TogglePinTab(BrowserTab tab)
        {
            tab.IsPinned = !tab.IsPinned;
            if (tab.IsPinned)
            {
                int pinnedCount = Tabs.Count(t => t.IsPinned && t != tab);
                int oldIndex = Tabs.IndexOf(tab);
                if (oldIndex != pinnedCount)
                {
                    Tabs.Move(oldIndex, pinnedCount);
                }
            }
        }

        public void ToggleMuteTab(BrowserTab tab)
        {
            if (tab.WebViewInstance?.CoreWebView2 != null)
            {
                tab.WebViewInstance.CoreWebView2.IsMuted = !tab.WebViewInstance.CoreWebView2.IsMuted;
            }
        }

        public void TogglePinCurrentTab()
        {
            if (_activeTab != null) TogglePinTab(_activeTab);
        }

        public void ToggleMuteCurrentTab()
        {
            if (_activeTab != null) ToggleMuteTab(_activeTab);
        }

        public void MuteOtherTabsCurrent()
        {
            if (_activeTab != null) MuteOtherTabs(_activeTab);
        }

        public void SnoozeCurrentTab()
        {
            if (_activeTab != null) SnoozeTab(_activeTab);
        }

        public void MuteOtherTabs(BrowserTab keepTab)
        {
            foreach (var t in Tabs.Where(t => t != keepTab))
            {
                if (t.WebViewInstance?.CoreWebView2 != null)
                {
                    t.WebViewInstance.CoreWebView2.IsMuted = true;
                    t.IsMuted = true;
                }
            }
        }

        public async void SnoozeTab(BrowserTab tab)
        {
            if (tab == null || tab == _activeTab || tab.IsPinned || tab.IsPlayingAudio) return;
            try
            {
                if (tab.WebViewInstance?.CoreWebView2 != null)
                {
                    await tab.WebViewInstance.CoreWebView2.TrySuspendAsync();
                    tab.IsSleeping = true;
                }
            }
            catch { }
        }

        public void WakeTab(BrowserTab tab)
        {
            if (tab == null) return;
            try
            {
                if (tab.IsSleeping && tab.WebViewInstance?.CoreWebView2 != null)
                {
                    tab.WebViewInstance.CoreWebView2.Resume();
                }
            }
            catch { }
            finally
            {
                tab.IsSleeping = false;
            }
        }

        public void PrintActiveTab()
        {
            if (_activeTab?.WebViewInstance?.CoreWebView2 != null)
            {
                try
                {
                    _activeTab.WebViewInstance.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
                }
                catch { }
            }
        }

        public async Task SaveActiveTabAsPdfAsync()
        {
            if (_activeTab?.WebViewInstance?.CoreWebView2 == null) return;
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Document (*.pdf)|*.pdf",
                    FileName = $"{string.Join("_", _activeTab.Title.Split(Path.GetInvalidFileNameChars()))}.pdf"
                };
                if (sfd.ShowDialog() == true)
                {
                    await _activeTab.WebViewInstance.CoreWebView2.PrintToPdfAsync(sfd.FileName);
                }
            }
            catch { }
        }

        public async void TogglePictureInPicture()
        {
            if (_activeTab?.WebViewInstance?.CoreWebView2 == null) return;
            try
            {
                string script = @"(() => {
                    const video = document.querySelector('video');
                    if (!video) return false;
                    if (document.pictureInPictureElement) {
                        document.exitPictureInPicture();
                        return true;
                    } else if (document.pictureInPictureEnabled) {
                        video.requestPictureInPicture();
                        return true;
                    }
                    return false;
                })()";
                string result = await _activeTab.WebViewInstance.CoreWebView2.ExecuteScriptAsync(script);
                if (result == "false")
                {
                    System.Windows.MessageBox.Show("No active video found on this page to pop out.", "Picture-in-Picture", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch { }
        }

        public async Task<string?> CaptureActiveTabScreenshotBase64Async()
        {
            if (_activeTab?.WebViewInstance?.CoreWebView2 == null) return null;
            try
            {
                using var ms = new MemoryStream();
                await _activeTab.WebViewInstance.CoreWebView2.CapturePreviewAsync(
                    CoreWebView2CapturePreviewImageFormat.Png, ms);
                return Convert.ToBase64String(ms.ToArray());
            }
            catch
            {
                return null;
            }
        }

        public void CloseOtherTabs(BrowserTab keepTab)
        {
            var toRemove = Tabs.Where(t => t != keepTab && !t.IsPinned).ToList();
            foreach (var t in toRemove)
            {
                CloseTab(t);
            }
        }

        public void CloseOtherTabsCurrent()
        {
            if (_activeTab != null) CloseOtherTabs(_activeTab);
        }

        public void CloseTabsToRight(BrowserTab currentTab)
        {
            int index = Tabs.IndexOf(currentTab);
            if (index >= 0 && index < Tabs.Count - 1)
            {
                var toRemove = Tabs.Skip(index + 1).Where(t => !t.IsPinned).ToList();
                foreach (var t in toRemove)
                {
                    CloseTab(t);
                }
            }
        }

        public void CloseTabsToRightCurrent()
        {
            if (_activeTab != null) CloseTabsToRight(_activeTab);
        }

        public void CloseTabsToLeft(BrowserTab currentTab)
        {
            if (currentTab == null) return;
            int index = Tabs.IndexOf(currentTab);
            if (index > 0)
            {
                var toRemove = Tabs.Take(index).Where(t => !t.IsPinned).ToList();
                foreach (var t in toRemove)
                {
                    CloseTab(t);
                }
            }
        }

        public void CloseTabsToLeftCurrent()
        {
            if (_activeTab != null) CloseTabsToLeft(_activeTab);
        }

        public void SnoozeAllInactiveTabs()
        {
            foreach (var tab in Tabs)
            {
                if (tab != _activeTab && !tab.IsPinned && !tab.IsPlayingAudio && !tab.IsSleeping)
                {
                    SnoozeTab(tab);
                }
            }
        }

        public void WakeAllTabs()
        {
            foreach (var tab in Tabs)
            {
                if (tab.IsSleeping)
                {
                    WakeTab(tab);
                }
            }
        }

        public void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                EnterFullscreen();
            }
            else
            {
                ExitFullscreen();
            }
        }

        private void EnterFullscreen()
        {
            _isFullscreen = true;
            _savedWindowState = WindowState;
            _savedSidebarWidth = SidebarBorder.Width;
            _savedIsSidebarCollapsed = _isSidebarCollapsed;

            // Hide chrome and sidebars
            SidebarBorder.Visibility = Visibility.Collapsed;
            HorizontalTabsRow.Visibility = Visibility.Collapsed;
            TopNavRowDef.Height = new GridLength(0);
            BookmarksRowDef.Height = new GridLength(0);
            if (AgentsBorder != null) AgentsBorder.Visibility = Visibility.Collapsed;

            // Remove card margin in fullscreen for edge-to-edge viewing
            WebContentCardBorder.Margin = new Thickness(0);
            WebContentCardBorder.CornerRadius = new CornerRadius(0);
            WebContentCardBorder.BorderThickness = new Thickness(0);

            // Fullscreen window bounds
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;

            // Show floating exit notification pill
            FullscreenNotificationPill.Visibility = Visibility.Visible;
            FullscreenNotificationPill.Opacity = 1.0;

            // Auto fade out pill after 2.8s
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(0.4))
            {
                BeginTime = TimeSpan.FromSeconds(2.8)
            };
            fadeOut.Completed += (s, e) =>
            {
                if (_isFullscreen) FullscreenNotificationPill.Visibility = Visibility.Collapsed;
            };
            FullscreenNotificationPill.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        public void ExitFullscreen()
        {
            if (!_isFullscreen) return;
            _isFullscreen = false;

            FullscreenNotificationPill.Visibility = Visibility.Collapsed;
            FullscreenNotificationPill.BeginAnimation(UIElement.OpacityProperty, null);

            // Restore window bounds and style
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            WindowState = _savedWindowState;

            // Restore card styling
            WebContentCardBorder.Margin = new Thickness(10, 2, 14, 14);
            WebContentCardBorder.CornerRadius = new CornerRadius(14);
            WebContentCardBorder.BorderThickness = new Thickness(1);

            // Restore chrome
            TopNavRowDef.Height = new GridLength(42);
            BookmarksRowDef.Height = new GridLength(0, GridUnitType.Auto);

            SetSidebarCollapsed(_savedIsSidebarCollapsed);
            if (AgentsBorder != null)
            {
                AgentsBorder.Visibility = _isAgentsPanelOpen ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ExitFullscreen_Click(object sender, RoutedEventArgs e)
        {
            ExitFullscreen();
        }

        private async void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            await CreateNewTabAsync(BrowserSettings.Current.StartPageUrl, switchTo: true);
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
                    From = SidebarBorder.ActualWidth > 0 ? SidebarBorder.ActualWidth : _sidebarWidth,
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
                    To = _sidebarWidth,
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
            // If agents panel is open, caption buttons are above AgentsBorder header.
            // If agents panel is closed (0px on right), caption buttons are above main content area (needs 172px right margin).
            double rightMargin = _isAgentsPanelOpen ? 10 : 172;

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
                    To = _agentsWidth,
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
                    From = AgentsBorder.ActualWidth > 0 ? AgentsBorder.ActualWidth : _agentsWidth,
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

        #region Sidebar Resizing

        private void LeftSidebarSplitter_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                LeftSidebarSplitter_MouseDoubleClick(sender, e);
                return;
            }

            _isDraggingLeftSplitter = true;
            _dragStartX = e.GetPosition(this).X;
            _dragStartWidth = SidebarBorder.ActualWidth;
            SidebarBorder.BeginAnimation(FrameworkElement.WidthProperty, null);
            LeftSidebarSplitter.CaptureMouse();
            e.Handled = true;
        }

        private void LeftSidebarSplitter_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingLeftSplitter)
            {
                double currentX = e.GetPosition(this).X;
                double delta = currentX - _dragStartX;
                double maxAllowed = Math.Min(500, ActualWidth - (_isAgentsPanelOpen ? AgentsBorder.ActualWidth : 0) - 350);
                double newWidth = Math.Clamp(_dragStartWidth + delta, 160, Math.Max(160, maxAllowed));
                SidebarBorder.Width = newWidth;
                _sidebarWidth = newWidth;

                if (_activeTab?.WebViewInstance != null)
                {
                    ClipWebViewToRoundedCorners(_activeTab.WebViewInstance);
                }
                e.Handled = true;
            }
        }

        private void LeftSidebarSplitter_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingLeftSplitter)
            {
                _isDraggingLeftSplitter = false;
                LeftSidebarSplitter.ReleaseMouseCapture();
                _sidebarWidth = SidebarBorder.Width;
                if (_activeTab?.WebViewInstance != null)
                {
                    ClipWebViewToRoundedCorners(_activeTab.WebViewInstance);
                }
                e.Handled = true;
            }
        }

        private void LeftSidebarSplitter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _sidebarWidth = 240;
            SidebarBorder.BeginAnimation(FrameworkElement.WidthProperty, null);
            SidebarBorder.Width = 240;
            if (_activeTab?.WebViewInstance != null)
            {
                ClipWebViewToRoundedCorners(_activeTab.WebViewInstance);
            }
            e.Handled = true;
        }

        private void RightSidebarSplitter_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                RightSidebarSplitter_MouseDoubleClick(sender, e);
                return;
            }

            _isDraggingRightSplitter = true;
            _dragStartX = e.GetPosition(this).X;
            _dragStartWidth = AgentsBorder.ActualWidth;
            AgentsBorder.BeginAnimation(FrameworkElement.WidthProperty, null);
            RightSidebarSplitter.CaptureMouse();
            e.Handled = true;
        }

        private void RightSidebarSplitter_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingRightSplitter)
            {
                double currentX = e.GetPosition(this).X;
                double delta = _dragStartX - currentX; // Moving left increases right sidebar width
                double maxAllowed = Math.Min(600, ActualWidth - (!_isSidebarCollapsed ? SidebarBorder.ActualWidth : 0) - 350);
                double newWidth = Math.Clamp(_dragStartWidth + delta, 220, Math.Max(220, maxAllowed));
                AgentsBorder.Width = newWidth;
                _agentsWidth = newWidth;

                if (_activeTab?.WebViewInstance != null)
                {
                    ClipWebViewToRoundedCorners(_activeTab.WebViewInstance);
                }
                e.Handled = true;
            }
        }

        private void RightSidebarSplitter_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingRightSplitter)
            {
                _isDraggingRightSplitter = false;
                RightSidebarSplitter.ReleaseMouseCapture();
                _agentsWidth = AgentsBorder.Width;
                if (_activeTab?.WebViewInstance != null)
                {
                    ClipWebViewToRoundedCorners(_activeTab.WebViewInstance);
                }
                e.Handled = true;
            }
        }

        private void RightSidebarSplitter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _agentsWidth = 320;
            AgentsBorder.BeginAnimation(FrameworkElement.WidthProperty, null);
            AgentsBorder.Width = 320;
            if (_activeTab?.WebViewInstance != null)
            {
                ClipWebViewToRoundedCorners(_activeTab.WebViewInstance);
            }
            e.Handled = true;
        }

        private void Splitter_LostMouseCapture(object sender, MouseEventArgs e)
        {
            _isDraggingLeftSplitter = false;
            _isDraggingRightSplitter = false;
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
            if (e.Key == Key.Escape)
            {
                if (FindPopup.IsOpen) { FindPopup.IsOpen = false; e.Handled = true; return; }
                if (DownloadsPopup.IsOpen) { DownloadsPopup.IsOpen = false; e.Handled = true; return; }
                if (OmniboxSuggestionsPopup.IsOpen) { OmniboxSuggestionsPopup.IsOpen = false; e.Handled = true; return; }
            }

            // Hardware Media Keys Integration (#24)
            if (e.Key == Key.MediaPlayPause)
            {
                await ToggleMediaPlaybackAsync();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.MediaNextTrack)
            {
                await SkipMediaAsync(10);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.MediaPreviousTrack)
            {
                await SkipMediaAsync(-10);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.MediaStop)
            {
                await StopMediaAsync();
                e.Handled = true;
                return;
            }

            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (e.Key == Key.B)
                {
                    ToggleBookmarksBar();
                    e.Handled = true;
                }
                else if (e.Key == Key.P)
                {
                    OpenCommandPalette();
                    e.Handled = true;
                }
                else if (e.Key == Key.T)
                {
                    ReopenClosedTab();
                    e.Handled = true;
                }
                else if (e.Key == Key.R)
                {
                    ToggleReaderMode();
                    e.Handled = true;
                }
                else if (e.Key == Key.N)
                {
                    OpenInPrivateWindow();
                    e.Handled = true;
                }
                else if (e.Key == Key.Tab)
                {
                    SelectPreviousTab();
                    e.Handled = true;
                }
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
            {
                if (e.Key >= Key.D1 && e.Key <= Key.D9)
                {
                    int spaceIdx = e.Key - Key.D1;
                    if (spaceIdx < Spaces.Count)
                    {
                        _ = SwitchToSpaceAsync(Spaces[spaceIdx]);
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.N)
                {
                    PromptCreateNewSpace();
                    e.Handled = true;
                }
                else if (e.Key == Key.Right)
                {
                    SwitchToNextSpace();
                    e.Handled = true;
                }
                else if (e.Key == Key.Left)
                {
                    SwitchToPreviousSpace();
                    e.Handled = true;
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.OemBackslash || e.Key == Key.Oem5)
                {
                    ToggleSplitView();
                    e.Handled = true;
                }
                else if (e.Key == Key.K)
                {
                    OpenCommandPalette();
                    e.Handled = true;
                }
                else if (e.Key == Key.OemComma)
                {
                    OpenSettingsPage();
                    e.Handled = true;
                }
                else if (e.Key == Key.D)
                {
                    BookmarkCurrentPage();
                    e.Handled = true;
                }
                else if (e.Key == Key.P)
                {
                    PrintCurrentPage();
                    e.Handled = true;
                }
                else if (e.Key == Key.U)
                {
                    ViewPageSource();
                    e.Handled = true;
                }
                else if (e.Key == Key.OemPlus || e.Key == Key.Add)
                {
                    ZoomIn();
                    e.Handled = true;
                }
                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
                {
                    ZoomOut();
                    e.Handled = true;
                }
                else if (e.Key == Key.D0 || e.Key == Key.NumPad0)
                {
                    ResetZoom();
                    e.Handled = true;
                }
                else if (e.Key == Key.T)
                {
                    e.Handled = true;
                    await CreateNewTabAsync(BrowserSettings.Current.StartPageUrl, switchTo: true);
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
                else if (e.Key == Key.F)
                {
                    e.Handled = true;
                    OpenFindInPage();
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
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        ToggleDownloads();
                    }
                    else
                    {
                        ToggleAgentsPanel_Click(this, new RoutedEventArgs());
                    }
                }
                else if (e.Key == Key.K)
                {
                    e.Handled = true;
                    OpenCommandPalette();
                }
                else if (e.Key == Key.P)
                {
                    e.Handled = true;
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        OpenCommandPalette();
                    }
                    else
                    {
                        PrintActiveTab();
                    }
                }
                else if (e.Key == Key.I && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    e.Handled = true;
                    OpenDevTools();
                }
                else if (e.Key >= Key.D1 && e.Key <= Key.D9)
                {
                    e.Handled = true;
                    int targetIdx = e.Key == Key.D9 ? Tabs.Count - 1 : (e.Key - Key.D1);
                    if (targetIdx >= 0 && targetIdx < Tabs.Count)
                    {
                        SwitchToTab(Tabs[targetIdx]);
                    }
                }
                else if (e.Key == Key.Tab)
                {
                    e.Handled = true;
                    SelectNextTab();
                }
                else if (e.Key == Key.M)
                {
                    e.Handled = true;
                    ToggleMuteCurrentTab();
                }
            }
            else if (e.Key == Key.Escape)
            {
                if (FindPopup.IsOpen)
                {
                    CloseFindInPage();
                    e.Handled = true;
                }
                else if (DownloadsPopup.IsOpen)
                {
                    DownloadsPopup.IsOpen = false;
                    e.Handled = true;
                }
                else if (HistoryPopup.IsOpen)
                {
                    HistoryPopup.IsOpen = false;
                    e.Handled = true;
                }
                else if (_isFullscreen)
                {
                    ExitFullscreen();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F9)
            {
                e.Handled = true;
                ToggleReaderMode();
            }
            else if (e.Key == Key.F11)
            {
                e.Handled = true;
                ToggleFullscreen();
            }
            else if (e.Key == Key.F5)
            {
                e.Handled = true;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    HardReloadCurrentPage();
                }
                else
                {
                    ReloadButton_Click(this, new RoutedEventArgs());
                }
            }
            else if (e.Key == Key.F12)
            {
                e.Handled = true;
                OpenDevTools();
            }
        }

        #endregion

        #region Agents Panel Interactions

        private void LoadInitialAgentSessions()
        {
            AgentSessions.Clear();
            var defaultSession = new AgentSession("Page Copilot", "Active · Inspects page context", "\uF4A5", isActive: true);
            AgentSessions.Add(defaultSession);
            AgentSessions.Add(new AgentSession("Deep Research", "Synthesizes web sources", "\uE721", isActive: false));
            AgentSessions.Add(new AgentSession("Code Analyst", "Inspects DOM & APIs", "\uEC7A", isActive: false));
            AgentSessions.Add(new AgentSession("Content Explainer", "Simplifies concepts", "\uE8BD", isActive: false));

            if (SelectedAgentText != null) SelectedAgentText.Text = defaultSession.Name;
            if (SelectedAgentIcon != null) SelectedAgentIcon.Text = defaultSession.Icon;
        }

        private void AgentPickerButton_Click(object sender, RoutedEventArgs e)
        {
            AgentPickerPopup.IsOpen = !AgentPickerPopup.IsOpen;
        }

        private void AgentPickerItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is AgentSession session)
            {
                foreach (var s in AgentSessions)
                {
                    s.IsActive = (s == session);
                }
                if (SelectedAgentText != null) SelectedAgentText.Text = session.Name;
                if (SelectedAgentIcon != null) SelectedAgentIcon.Text = session.Icon;
                AgentPickerPopup.IsOpen = false;

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

            string currentTitle = _activeTab?.Title ?? "Current Tab";
            string currentUrl = _activeTab?.Url ?? "";

            string pageText = "";
            string headings = "";

            if (_activeTab?.WebViewInstance?.CoreWebView2 != null &&
                !currentUrl.StartsWith("browy://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string script = @"(function() {
                        var h = Array.from(document.querySelectorAll('h1, h2, h3'))
                                     .map(x => x.innerText.trim())
                                     .filter(x => x.length > 2)
                                     .slice(0, 10).join(' • ');
                        var body = document.body ? document.body.innerText.replace(/\s+/g, ' ').trim() : '';
                        return JSON.stringify({ headings: h, text: body.slice(0, 4000) });
                    })()";
                    string rawJson = await _activeTab.WebViewInstance.CoreWebView2.ExecuteScriptAsync(script);
                    using var doc = System.Text.Json.JsonDocument.Parse(rawJson);
                    if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        using var innerDoc = System.Text.Json.JsonDocument.Parse(doc.RootElement.GetString()!);
                        headings = innerDoc.RootElement.GetProperty("headings").GetString() ?? "";
                        pageText = innerDoc.RootElement.GetProperty("text").GetString() ?? "";
                    }
                }
                catch { }
            }

            string response = await AiCopilotService.QueryCopilotAsync(prompt, pageText, headings, currentTitle, currentUrl);
            AgentMessages.Add(new AgentMessage("Agent", response));
            AgentMessagesScrollViewer?.ScrollToEnd();
        }

        #endregion

        #region Arc/Zen Spaces Management (Milestone 1)

        public void SyncActiveSpaceTabs()
        {
            if (ActiveSpace == null) return;
            ActiveSpace.Tabs.Clear();
            foreach (var t in Tabs)
            {
                ActiveSpace.Tabs.Add(t);
            }
            ActiveSpace.ActiveTabIndex = _activeTab != null ? Math.Max(0, Tabs.IndexOf(_activeTab)) : 0;
        }

        public async Task SwitchToSpaceAsync(BrowserSpace space)
        {
            if (space == null || ActiveSpace == space) return;

            SyncActiveSpaceTabs();
            if (ActiveSpace != null)
            {
                ActiveSpace.IsActive = false;
            }

            foreach (var t in Tabs)
            {
                if (t.WebViewInstance != null)
                {
                    t.WebViewInstance.Visibility = Visibility.Collapsed;
                }
            }

            ActiveSpace = space;
            space.IsActive = true;

            Tabs.Clear();
            _activeTab = null;

            if (space.Tabs.Count == 0)
            {
                await CreateNewTabAsync(BrowserSettings.Current.StartPageUrl, switchTo: true);
            }
            else
            {
                BrowserTab? targetActiveTab = null;
                for (int i = 0; i < space.Tabs.Count; i++)
                {
                    var tab = space.Tabs[i];
                    Tabs.Add(tab);

                    if (tab.WebViewInstance == null)
                    {
                        var webView = new WebView2
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            DefaultBackgroundColor = System.Drawing.Color.Transparent,
                            Visibility = Visibility.Collapsed
                        };
                        webView.SizeChanged += (s, e) => ClipWebViewToRoundedCorners(webView);
                        tab.WebViewInstance = webView;
                        Grid.SetColumn(webView, 0);
                        WebContentGrid.Children.Add(webView);
                        _ = InitializeWebViewAsync(tab, tab.Url);
                    }
                    else if (!WebContentGrid.Children.Contains(tab.WebViewInstance))
                    {
                        Grid.SetColumn(tab.WebViewInstance, 0);
                        WebContentGrid.Children.Add(tab.WebViewInstance);
                    }

                    if (i == space.ActiveTabIndex)
                    {
                        targetActiveTab = tab;
                    }
                }

                if (targetActiveTab == null && Tabs.Count > 0)
                {
                    targetActiveTab = Tabs[0];
                }

                if (targetActiveTab != null)
                {
                    SwitchToTab(targetActiveTab);
                }
            }
        }

        public void SwitchToNextSpace()
        {
            if (Spaces.Count <= 1 || ActiveSpace == null) return;
            int idx = Spaces.IndexOf(ActiveSpace);
            int nextIdx = (idx + 1) % Spaces.Count;
            _ = SwitchToSpaceAsync(Spaces[nextIdx]);
        }

        public void SwitchToPreviousSpace()
        {
            if (Spaces.Count <= 1 || ActiveSpace == null) return;
            int idx = Spaces.IndexOf(ActiveSpace);
            int prevIdx = (idx - 1 + Spaces.Count) % Spaces.Count;
            _ = SwitchToSpaceAsync(Spaces[prevIdx]);
        }

        public void PromptCreateNewSpace()
        {
            int num = Spaces.Count + 1;
            string[] colors = { "#6366F1", "#EC4899", "#10B981", "#F59E0B", "#0EA5E9", "#8B5CF6" };
            string[] icons = { "\uE774", "\uE80F", "\uE7BE", "\uEC7A", "\uE8BD", "\uE734" };

            string color = colors[(num - 1) % colors.Length];
            string icon = icons[(num - 1) % icons.Length];
            string name = $"Space {num}";

            var newSpace = new BrowserSpace
            {
                Name = name,
                IconGlyph = icon,
                AccentColorHex = color
            };
            Spaces.Add(newSpace);
            _ = SwitchToSpaceAsync(newSpace);
        }

        private void SpacePill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is BrowserSpace space)
            {
                _ = SwitchToSpaceAsync(space);
            }
        }

        private void NewSpaceButton_Click(object sender, RoutedEventArgs e)
        {
            PromptCreateNewSpace();
        }

        private void RenameSpace_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is BrowserSpace space)
            {
                space.Name = space.Name + "+";
            }
        }

        private void SetSpaceColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is BrowserSpace space && item.CommandParameter is string hex)
            {
                space.AccentColorHex = hex;
            }
        }

        private void DeleteSpace_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is BrowserSpace space)
            {
                if (Spaces.Count <= 1) return;
                int idx = Spaces.IndexOf(space);
                Spaces.Remove(space);
                if (ActiveSpace == space)
                {
                    int nextIdx = Math.Min(idx, Spaces.Count - 1);
                    _ = SwitchToSpaceAsync(Spaces[nextIdx]);
                }
            }
        }

        private void ToggleSpaceIsolation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is BrowserSpace space)
            {
                space.IsIsolated = !space.IsIsolated;
                SessionService.SaveSpacesSession(Spaces, ActiveSpace, _activeTab);
                NotificationService.ShowToast(
                    "Space Container Profile",
                    $"{space.Name} is now {(space.IsIsolated ? "Isolated (Independent Logins & Storage)" : "Shared Profile")}.");
            }
        }

        #endregion

        #region Split View Dual-Pane & Quad (Milestone 2 & Issue #11)

        public void ToggleSplitView()
        {
            if (!_isSplitViewActive)
            {
                SetSplitMode(SplitViewMode.DualVertical);
            }
            else
            {
                DisableSplitView();
            }
        }

        public void SetSplitMode(SplitViewMode mode)
        {
            if (mode == SplitViewMode.Single)
            {
                DisableSplitView();
                return;
            }

            _isSplitViewActive = true;
            SplitMode = mode;

            if (_splitSecondaryTab == null || _splitSecondaryTab == _activeTab)
            {
                _splitSecondaryTab = Tabs.FirstOrDefault(t => t != _activeTab);
                if (_splitSecondaryTab == null)
                {
                    _ = CreateNewTabAsync(BrowserSettings.Current.StartPageUrl, switchTo: false).ContinueWith(task =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _splitSecondaryTab = task.Result;
                            ApplySplitLayout();
                        });
                    });
                    return;
                }
            }

            if (mode == SplitViewMode.Quad)
            {
                _ = SetupQuadTabsAsync();
                return;
            }

            SplitViewButton.Background = new SolidColorBrush(Color.FromArgb(0x35, 0x63, 0x66, 0xF1));
            ApplySplitLayout();
        }

        public async Task SetupQuadTabsAsync()
        {
            while (Tabs.Count < 4)
            {
                await CreateNewTabAsync(BrowserSettings.Current.StartPageUrl, switchTo: false);
            }

            var otherTabs = Tabs.Where(t => t != _activeTab).Take(3).ToList();
            _splitSecondaryTab = otherTabs.Count > 0 ? otherTabs[0] : null;
            _splitTab3 = otherTabs.Count > 1 ? otherTabs[1] : null;
            _splitTab4 = otherTabs.Count > 2 ? otherTabs[2] : null;

            _isSplitViewActive = true;
            SplitMode = SplitViewMode.Quad;
            SplitViewButton.Background = new SolidColorBrush(Color.FromArgb(0x35, 0x63, 0x66, 0xF1));
            ApplySplitLayout();
        }

        public void ToggleSplitOrientation()
        {
            if (!_isSplitViewActive)
            {
                SetSplitMode(SplitViewMode.DualVertical);
                return;
            }

            if (SplitMode == SplitViewMode.DualVertical)
            {
                SetSplitMode(SplitViewMode.DualHorizontal);
            }
            else if (SplitMode == SplitViewMode.DualHorizontal)
            {
                SetSplitMode(SplitViewMode.DualVertical);
            }
        }

        public void ApplySplitLayout()
        {
            if (!_isSplitViewActive || SplitMode == SplitViewMode.Single)
            {
                SecondarySplitColumn.Width = new GridLength(0);
                SecondarySplitRow.Height = new GridLength(0);
                CenterWebSplitter.Visibility = Visibility.Collapsed;
                HorizontalWebSplitter.Visibility = Visibility.Collapsed;

                foreach (var t in Tabs)
                {
                    if (t.WebViewInstance != null)
                    {
                        Grid.SetColumn(t.WebViewInstance, 0);
                        Grid.SetRow(t.WebViewInstance, 0);
                        Grid.SetColumnSpan(t.WebViewInstance, 3);
                        Grid.SetRowSpan(t.WebViewInstance, 3);
                        t.WebViewInstance.Visibility = (t == _activeTab) ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
                return;
            }

            if (SplitMode == SplitViewMode.DualVertical)
            {
                SecondarySplitColumn.Width = new GridLength(1, GridUnitType.Star);
                SecondarySplitRow.Height = new GridLength(0);
                CenterWebSplitter.Visibility = Visibility.Visible;
                HorizontalWebSplitter.Visibility = Visibility.Collapsed;

                foreach (var t in Tabs)
                {
                    if (t.WebViewInstance != null)
                    {
                        if (t == _activeTab)
                        {
                            Grid.SetColumn(t.WebViewInstance, 0);
                            Grid.SetRow(t.WebViewInstance, 0);
                            Grid.SetColumnSpan(t.WebViewInstance, 1);
                            Grid.SetRowSpan(t.WebViewInstance, 3);
                            t.WebViewInstance.Visibility = Visibility.Visible;
                        }
                        else if (t == _splitSecondaryTab)
                        {
                            Grid.SetColumn(t.WebViewInstance, 2);
                            Grid.SetRow(t.WebViewInstance, 0);
                            Grid.SetColumnSpan(t.WebViewInstance, 1);
                            Grid.SetRowSpan(t.WebViewInstance, 3);
                            t.WebViewInstance.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            t.WebViewInstance.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
            else if (SplitMode == SplitViewMode.DualHorizontal)
            {
                SecondarySplitColumn.Width = new GridLength(0);
                SecondarySplitRow.Height = new GridLength(1, GridUnitType.Star);
                CenterWebSplitter.Visibility = Visibility.Collapsed;
                HorizontalWebSplitter.Visibility = Visibility.Visible;

                foreach (var t in Tabs)
                {
                    if (t.WebViewInstance != null)
                    {
                        if (t == _activeTab)
                        {
                            Grid.SetColumn(t.WebViewInstance, 0);
                            Grid.SetRow(t.WebViewInstance, 0);
                            Grid.SetColumnSpan(t.WebViewInstance, 3);
                            Grid.SetRowSpan(t.WebViewInstance, 1);
                            t.WebViewInstance.Visibility = Visibility.Visible;
                        }
                        else if (t == _splitSecondaryTab)
                        {
                            Grid.SetColumn(t.WebViewInstance, 0);
                            Grid.SetRow(t.WebViewInstance, 2);
                            Grid.SetColumnSpan(t.WebViewInstance, 3);
                            Grid.SetRowSpan(t.WebViewInstance, 1);
                            t.WebViewInstance.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            t.WebViewInstance.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
            else if (SplitMode == SplitViewMode.Quad)
            {
                SecondarySplitColumn.Width = new GridLength(1, GridUnitType.Star);
                SecondarySplitRow.Height = new GridLength(1, GridUnitType.Star);
                CenterWebSplitter.Visibility = Visibility.Visible;
                HorizontalWebSplitter.Visibility = Visibility.Visible;

                foreach (var t in Tabs)
                {
                    if (t.WebViewInstance != null)
                    {
                        if (t == _activeTab)
                        {
                            Grid.SetColumn(t.WebViewInstance, 0);
                            Grid.SetRow(t.WebViewInstance, 0);
                            Grid.SetColumnSpan(t.WebViewInstance, 1);
                            Grid.SetRowSpan(t.WebViewInstance, 1);
                            t.WebViewInstance.Visibility = Visibility.Visible;
                        }
                        else if (t == _splitSecondaryTab)
                        {
                            Grid.SetColumn(t.WebViewInstance, 2);
                            Grid.SetRow(t.WebViewInstance, 0);
                            Grid.SetColumnSpan(t.WebViewInstance, 1);
                            Grid.SetRowSpan(t.WebViewInstance, 1);
                            t.WebViewInstance.Visibility = Visibility.Visible;
                        }
                        else if (t == _splitTab3)
                        {
                            Grid.SetColumn(t.WebViewInstance, 0);
                            Grid.SetRow(t.WebViewInstance, 2);
                            Grid.SetColumnSpan(t.WebViewInstance, 1);
                            Grid.SetRowSpan(t.WebViewInstance, 1);
                            t.WebViewInstance.Visibility = Visibility.Visible;
                        }
                        else if (t == _splitTab4)
                        {
                            Grid.SetColumn(t.WebViewInstance, 2);
                            Grid.SetRow(t.WebViewInstance, 2);
                            Grid.SetColumnSpan(t.WebViewInstance, 1);
                            Grid.SetRowSpan(t.WebViewInstance, 1);
                            t.WebViewInstance.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            t.WebViewInstance.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
        }

        private void DisableSplitView()
        {
            _isSplitViewActive = false;
            SplitMode = SplitViewMode.Single;
            SecondarySplitColumn.Width = new GridLength(0);
            SecondarySplitRow.Height = new GridLength(0);
            CenterWebSplitter.Visibility = Visibility.Collapsed;
            HorizontalWebSplitter.Visibility = Visibility.Collapsed;

            _splitSecondaryTab = null;
            _splitTab3 = null;
            _splitTab4 = null;

            SplitViewButton.Background = Brushes.Transparent;
            ApplySplitLayout();
        }

        private void SplitViewButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleSplitView();
        }

        private void SplitViewVertical_Click(object sender, RoutedEventArgs e)
        {
            SetSplitMode(SplitViewMode.DualVertical);
        }

        private void SplitViewHorizontal_Click(object sender, RoutedEventArgs e)
        {
            SetSplitMode(SplitViewMode.DualHorizontal);
        }

        private void SplitViewQuad_Click(object sender, RoutedEventArgs e)
        {
            _ = SetupQuadTabsAsync();
        }

        private void SplitViewClose_Click(object sender, RoutedEventArgs e)
        {
            DisableSplitView();
        }

        #endregion

        #region InPrivate Stealth Browsing (Milestone 3)

        public void OpenInPrivateWindow()
        {
            var inPrivateWin = new MainWindow { IsInPrivate = true };
            inPrivateWin.Show();
        }

        #endregion

        #region Dedicated Internal Hub Pages (Milestone 5)

        public void OpenHistoryPage()
        {
            NavigateActiveTab("browy://history");
        }

        public void OpenBookmarksPage()
        {
            NavigateActiveTab("browy://bookmarks");
        }

        public void OpenDownloadsPage()
        {
            NavigateActiveTab("browy://downloads");
        }

        public void OpenExtensionsPage()
        {
            NavigateActiveTab("browy://extensions");
        }

        public async Task LoadUnpackedExtensionAsync()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Unpacked Extension Folder (containing manifest.json)"
            };

            if (dialog.ShowDialog(this) == true)
            {
                try
                {
                    if (_activeTab?.WebViewInstance?.CoreWebView2?.Profile != null)
                    {
                        var ext = await ExtensionService.LoadExtensionFromFolderAsync(_activeTab.WebViewInstance.CoreWebView2.Profile, dialog.FolderName);
                        if (_activeTab.Url.StartsWith("browy://extensions", StringComparison.OrdinalIgnoreCase))
                        {
                            _activeTab.WebViewInstance.NavigateToString(InternalHubService.GetExtensionsHtml(ExtensionService.Extensions));
                        }
                        else
                        {
                            OpenExtensionsPage();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to load extension: {ex.Message}", "Extension Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Media Controls & SMTC Integration (#24)

        public async Task ToggleMediaPlaybackAsync()
        {
            var targetTab = Tabs.FirstOrDefault(t => t.IsPlayingAudio) ?? _activeTab;
            if (targetTab?.WebViewInstance?.CoreWebView2 != null)
            {
                await targetTab.WebViewInstance.CoreWebView2.ExecuteScriptAsync(@"
                    (function() {
                        var media = Array.from(document.querySelectorAll('video, audio'));
                        if (media.length === 0) return;
                        var anyPlaying = media.some(m => !m.paused);
                        media.forEach(m => anyPlaying ? m.pause() : m.play());
                    })();
                ");
            }
        }

        public async Task SkipMediaAsync(int seconds)
        {
            var targetTab = Tabs.FirstOrDefault(t => t.IsPlayingAudio) ?? _activeTab;
            if (targetTab?.WebViewInstance?.CoreWebView2 != null)
            {
                await targetTab.WebViewInstance.CoreWebView2.ExecuteScriptAsync($@"
                    (function() {{
                        var media = document.querySelectorAll('video, audio');
                        media.forEach(m => m.currentTime += {seconds});
                    }})();
                ");
            }
        }

        public async Task StopMediaAsync()
        {
            var targetTab = Tabs.FirstOrDefault(t => t.IsPlayingAudio) ?? _activeTab;
            if (targetTab?.WebViewInstance?.CoreWebView2 != null)
            {
                await targetTab.WebViewInstance.CoreWebView2.ExecuteScriptAsync(@"
                    (function() {
                        var media = document.querySelectorAll('video, audio');
                        media.forEach(m => { m.pause(); m.currentTime = 0; });
                    })();
                ");
            }
        }

        #endregion
    }
}
