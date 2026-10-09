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

        public WebView2? WebViewInstance { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
