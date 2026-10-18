using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;

namespace Browy.Models
{
    public class BrowserTab : INotifyPropertyChanged
    {
        private string _title = "New Tab";
        private string _url = "https://www.google.com";
        private bool _isLoading = false;
        private bool _canGoBack = false;
        private bool _canGoForward = false;
        private bool _isActive = false;
        private string? _faviconUri;
        private ImageSource? _faviconImage;
        private string _fallbackIcon = "\uE774";
        private bool _isPlayingAudio = false;
        private bool _isMuted = false;
        private bool _isPinned = false;

        public Guid Id { get; } = Guid.NewGuid();

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanGoBack
        {
            get => _canGoBack;
            set
            {
                if (_canGoBack != value)
                {
                    _canGoBack = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanGoForward
        {
            get => _canGoForward;
            set
            {
                if (_canGoForward != value)
                {
                    _canGoForward = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? FaviconUri
        {
            get => _faviconUri;
            set
            {
                if (_faviconUri != value)
                {
                    _faviconUri = value;
                    OnPropertyChanged();
                }
            }
        }

        public ImageSource? FaviconImage
        {
            get => _faviconImage;
            set
            {
                if (_faviconImage != value)
                {
                    _faviconImage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasFaviconImage));
                }
            }
        }

        public bool HasFaviconImage => _faviconImage != null;

        public string FallbackIcon
        {
            get => _fallbackIcon;
            set
            {
                if (_fallbackIcon != value)
                {
                    _fallbackIcon = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPlayingAudio
        {
            get => _isPlayingAudio;
            set
            {
                if (_isPlayingAudio != value)
                {
                    _isPlayingAudio = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowsAudioButton));
                    OnPropertyChanged(nameof(AudioIconGlyph));
                    OnPropertyChanged(nameof(AudioToolTip));
                }
            }
        }

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (_isMuted != value)
                {
                    _isMuted = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowsAudioButton));
                    OnPropertyChanged(nameof(AudioIconGlyph));
                    OnPropertyChanged(nameof(AudioToolTip));
                }
            }
        }

        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned != value)
                {
                    _isPinned = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowsAudioButton => _isPlayingAudio || _isMuted;
        public string AudioIconGlyph => _isMuted ? "\uE74F" : "\uE767";
        public string AudioToolTip => _isMuted ? "Unmute tab" : "Mute tab";

        private bool _isReaderModeActive = false;
        public bool IsReaderModeActive
        {
            get => _isReaderModeActive;
            set
            {
                if (_isReaderModeActive != value)
                {
                    _isReaderModeActive = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ReaderModeIconGlyph));
                    OnPropertyChanged(nameof(ReaderModeToolTip));
                }
            }
        }

        public string? OriginalUrlBeforeReader { get; set; }

        private bool _canEnterReaderMode = false;
        public bool CanEnterReaderMode
        {
            get => _canEnterReaderMode;
            set
            {
                if (_canEnterReaderMode != value)
                {
                    _canEnterReaderMode = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ReaderModeIconGlyph => _isReaderModeActive ? "\uE737" : "\uE736";
        public string ReaderModeToolTip => _isReaderModeActive ? "Exit Reader Mode (Ctrl+Shift+R, F9)" : "Enter Reader Mode (Ctrl+Shift+R, F9)";

        private int _blockedCount = 0;
        public int BlockedCount
        {
            get => _blockedCount;
            set
            {
                if (_blockedCount != value)
                {
                    _blockedCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShieldToolTip));
                }
            }
        }

        public string ShieldToolTip => _blockedCount > 0
            ? $"Browy Shield: {_blockedCount} ads & trackers blocked"
            : "Browy Shield: Active (Protected)";

        private bool _isSleeping = false;
        public bool IsSleeping
        {
            get => _isSleeping;
            set
            {
                if (_isSleeping != value)
                {
                    _isSleeping = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TabOpacity));
                    OnPropertyChanged(nameof(SleepStatusToolTip));
                    OnPropertyChanged(nameof(ShowsSleepIcon));
                }
            }
        }

        public double TabOpacity => _isSleeping ? 0.6 : 1.0;
        public bool ShowsSleepIcon => _isSleeping;
        public string SleepStatusToolTip => _isSleeping ? "Sleeping tab (saving memory) - click to wake" : "";

        public DateTime LastActiveTime { get; set; } = DateTime.Now;
        public bool IsSnoozed
        {
            get => IsSleeping;
            set => IsSleeping = value;
        }

        private string? _groupName;
        public string? GroupName
        {
            get => _groupName;
            set
            {
                if (_groupName != value)
                {
                    _groupName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasGroup));
                    OnPropertyChanged(nameof(GroupBrush));
                }
            }
        }

        private string? _groupColorHex;
        public string? GroupColorHex
        {
            get => _groupColorHex;
            set
            {
                if (_groupColorHex != value)
                {
                    _groupColorHex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GroupBrush));
                }
            }
        }

        public bool HasGroup => !string.IsNullOrWhiteSpace(_groupName);

        public Brush GroupBrush
        {
            get
            {
                if (!string.IsNullOrEmpty(_groupColorHex))
                {
                    try
                    {
                        var col = (Color)ColorConverter.ConvertFromString(_groupColorHex);
                        return new SolidColorBrush(Color.FromArgb(0x40, col.R, col.G, col.B));
                    }
                    catch { }
                }
                return new SolidColorBrush(Color.FromArgb(0x30, 0x63, 0x66, 0xF1));
            }
        }

        public Brush GroupBorderBrush
        {
            get
            {
                if (!string.IsNullOrEmpty(_groupColorHex))
                {
                    try
                    {
                        var col = (Color)ColorConverter.ConvertFromString(_groupColorHex);
                        return new SolidColorBrush(Color.FromArgb(0x80, col.R, col.G, col.B));
                    }
                    catch { }
                }
                return new SolidColorBrush(Color.FromArgb(0x60, 0x63, 0x66, 0xF1));
            }
        }

        public Brush GroupForegroundBrush
        {
            get
            {
                if (!string.IsNullOrEmpty(_groupColorHex))
                {
                    try
                    {
                        var col = (Color)ColorConverter.ConvertFromString(_groupColorHex);
                        return new SolidColorBrush(col);
                    }
                    catch { }
                }
                return new SolidColorBrush(Color.FromRgb(0xA5, 0xB4, 0xFC));
            }
        }

        public WebView2? WebViewInstance { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
