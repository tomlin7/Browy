using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using Browy.Services;

namespace Browy.Models
{
    public class HistoryItem : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _url = string.Empty;
        private DateTime _visitedAt = DateTime.Now;
        private ImageSource? _favicon;

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

        public DateTime VisitedAt
        {
            get => _visitedAt;
            set
            {
                if (_visitedAt != value)
                {
                    _visitedAt = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FormattedTime => VisitedAt.ToString("HH:mm");

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

        public HistoryItem() { }

        public HistoryItem(string title, string url, ImageSource? favicon = null)
        {
            Title = string.IsNullOrWhiteSpace(title) ? url : title;
            Url = url;
            VisitedAt = DateTime.Now;
            Favicon = favicon;

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
