using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using Browy.Services;

namespace Browy.Models
{
    public class BookmarkItem : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _url = string.Empty;
        private ImageSource? _favicon;
        private string _backgroundColor = "#10FFFFFF";
        private string _foregroundColor = "#E5E7EB";

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

        public ImageSource? Favicon
        {
            get => _favicon;
            set
            {
                if (_favicon != value)
                {
                    _favicon = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                if (_foregroundColor != value)
                {
                    _foregroundColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public BookmarkItem() { }

        public BookmarkItem(string title, string url, ImageSource? favicon = null, string bgColor = "#10FFFFFF", string fgColor = "#E5E7EB")
        {
            Title = title;
            Url = url;
            Favicon = favicon;
            BackgroundColor = bgColor;
            ForegroundColor = fgColor;

            if (Favicon == null && !string.IsNullOrWhiteSpace(url))
            {
                _ = LoadFaviconAsync();
            }
        }

        public async Task LoadFaviconAsync()
        {
            var img = await FaviconService.GetFaviconForUrlAsync(Url);
            if (img != null)
            {
                Favicon = img;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
