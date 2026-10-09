using System;
using System.Windows.Media;

namespace Browy.Models
{
    public class ClosedTabItem
    {
        public string Title { get; set; } = "Closed Tab";
        public string Url { get; set; } = string.Empty;
        public ImageSource? Favicon { get; set; }
        public DateTime ClosedAt { get; set; } = DateTime.Now;

        public bool HasFavicon => Favicon != null;
        public string FormattedTime => ClosedAt.ToString("HH:mm");

        public ClosedTabItem(string title, string url, ImageSource? favicon = null)
        {
            Title = string.IsNullOrWhiteSpace(title) ? url : title;
            Url = url;
            Favicon = favicon;
            ClosedAt = DateTime.Now;
        }
    }
}
