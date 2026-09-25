using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Browy.Services
{
    public static class FaviconService
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(6)
        };

        private static readonly ConcurrentDictionary<string, ImageSource?> _cache = new(StringComparer.OrdinalIgnoreCase);

        public static string GetFaviconUrlForDomain(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;

            try
            {
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "https://" + url;
                }

                var uri = new Uri(url);
                string host = uri.Host;
                return $"https://www.google.com/s2/favicons?domain={host}&sz=64";
            }
            catch
            {
                return $"https://www.google.com/s2/favicons?domain={url}&sz=64";
            }
        }

        public static async Task<ImageSource?> GetFaviconForUrlAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            string key = GetDomainKey(url);
            if (_cache.TryGetValue(key, out var cachedImage) && cachedImage != null)
            {
                return cachedImage;
            }

            try
            {
                string serviceUrl = GetFaviconUrlForDomain(url);
                var bytes = await _httpClient.GetByteArrayAsync(serviceUrl);
                if (bytes == null || bytes.Length == 0) return null;

                var image = CreateBitmapFromBytes(bytes);
                if (image != null)
                {
                    _cache[key] = image;
                }
                return image;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<ImageSource?> CreateImageFromStreamAsync(Stream stream, string? cacheKeyUrl = null)
        {
            if (stream == null) return null;

            try
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ms.Position = 0;
                if (ms.Length == 0) return null;

                var image = CreateBitmapFromBytes(ms.ToArray());
                if (image != null && !string.IsNullOrWhiteSpace(cacheKeyUrl))
                {
                    string key = GetDomainKey(cacheKeyUrl);
                    _cache[key] = image;
                }
                return image;
            }
            catch
            {
                return null;
            }
        }

        private static BitmapImage? CreateBitmapFromBytes(byte[] bytes)
        {
            try
            {
                var bitmap = new BitmapImage();
                using (var ms = new MemoryStream(bytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                }
                bitmap.Freeze(); // Freeze so it can be safely used across UI threads
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private static string GetDomainKey(string url)
        {
            try
            {
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "https://" + url;
                }
                return new Uri(url).Host.ToLowerInvariant();
            }
            catch
            {
                return url.ToLowerInvariant();
            }
        }
    }
}
