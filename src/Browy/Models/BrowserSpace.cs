using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Browy.Models
{
    public class BrowserSpace : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _name = "Personal";
        private string _iconGlyph = "\uE80F"; // Default Home/Personal
        private string _accentColorHex = "#6366F1"; // Modern Indigo
        private bool _isActive = false;
        private bool _isIsolated = false;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string IconGlyph
        {
            get => _iconGlyph;
            set { _iconGlyph = value; OnPropertyChanged(); }
        }

        public bool IsIsolated
        {
            get => _isIsolated;
            set
            {
                if (_isIsolated != value)
                {
                    _isIsolated = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowsIsolationBadge));
                    OnPropertyChanged(nameof(IsolationToolTip));
                }
            }
        }

        public bool ShowsIsolationBadge => _isIsolated;
        public string IsolationToolTip => _isIsolated ? "Isolated Container Profile (Separate logins & cookies)" : "Standard Profile";

        public string AccentColorHex
        {
            get => _accentColorHex;
            set
            {
                _accentColorHex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AccentBrush));
                OnPropertyChanged(nameof(AccentGlowBrush));
            }
        }

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

        public ObservableCollection<BrowserTab> Tabs { get; set; } = new();

        public int ActiveTabIndex { get; set; } = 0;

        public Brush AccentBrush
        {
            get
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(AccentColorHex);
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    return brush;
                }
                catch
                {
                    return Brushes.Indigo;
                }
            }
        }

        public Brush AccentGlowBrush
        {
            get
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(AccentColorHex);
                    var glowColor = Color.FromArgb(0x35, color.R, color.G, color.B);
                    var brush = new SolidColorBrush(glowColor);
                    brush.Freeze();
                    return brush;
                }
                catch
                {
                    return new SolidColorBrush(Color.FromArgb(0x35, 0x63, 0x66, 0xF1));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
