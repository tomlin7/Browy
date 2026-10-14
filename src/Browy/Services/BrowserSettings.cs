using System;
using System.IO;
using System.Text.Json;

namespace Browy.Services
{
    public class BrowserSettingsData
    {
        public string SearchEngineName { get; set; } = "Google";
        public string SearchEngineUrl { get; set; } = "https://www.google.com/search?q=";
        public string StartPageUrl { get; set; } = "browy://newtab";
        public string StartupBehavior { get; set; } = "RestoreSession";
        public bool IsShieldEnabled { get; set; } = true;
        public bool ShowBookmarksBar { get; set; } = true;
        public bool IsAcrylicEnabled { get; set; } = true;
        public bool BlockThirdPartyCookies { get; set; } = true;
        public bool DoNotTrack { get; set; } = true;
        public System.Collections.Generic.Dictionary<string, double> SiteZoomLevels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        // AI Copilot BYOK Configuration
        public string AiProvider { get; set; } = "BrowyDefault";
        public string OpenAiApiKey { get; set; } = string.Empty;
        public string AnthropicApiKey { get; set; } = string.Empty;
        public string OllamaEndpoint { get; set; } = "http://localhost:11434";
        public string SelectedAiModel { get; set; } = "gpt-4o-mini";
        public System.Collections.Generic.List<string> InstalledExtensionPaths { get; set; } = new();
        public System.Collections.Generic.List<string> AllowedNotificationOrigins { get; set; } = new();
        public System.Collections.Generic.List<string> BlockedNotificationOrigins { get; set; } = new();

        // Appearance & Backdrop (#4)
        public string BackdropMaterial { get; set; } = "Acrylic"; // "Acrylic", "Mica", "MicaAlt", "SolidDark"

        // Performance & Tab Suspension (#18)
        public bool EnableIdleTabSuspension { get; set; } = true;
        public int IdleTabSuspensionMinutes { get; set; } = 15;

        // Gestures & Navigation (#14)
        public bool EnableMouseGestures { get; set; } = true;
        public bool EnableTrackpadSwipe { get; set; } = true;

        // Interactive Keyboard Shortcuts (#9)
        public System.Collections.Generic.Dictionary<string, string> CustomKeyBindings { get; set; } = new();
    }

    public static class BrowserSettings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Browy",
            "settings.json");

        private static BrowserSettingsData _current = new();
        private static readonly object _lock = new();

        static BrowserSettings()
        {
            Load();
        }

        public static BrowserSettingsData Current
        {
            get
            {
                lock (_lock)
                {
                    return _current;
                }
            }
        }

        public static void Load()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(SettingsFilePath))
                    {
                        string json = File.ReadAllText(SettingsFilePath);
                        var loaded = JsonSerializer.Deserialize<BrowserSettingsData>(json);
                        if (loaded != null)
                        {
                            _current = loaded;
                        }
                    }
                }
                catch
                {
                    _current = new BrowserSettingsData();
                }
            }
        }

        public static void Save()
        {
            lock (_lock)
            {
                try
                {
                    string dir = Path.GetDirectoryName(SettingsFilePath)!;
                    Directory.CreateDirectory(dir);
                    string json = JsonSerializer.Serialize(_current, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(SettingsFilePath, json);
                }
                catch { }
            }
        }

        public static void SetSearchEngine(string name, string url)
        {
            lock (_lock)
            {
                _current.SearchEngineName = name;
                _current.SearchEngineUrl = url;
            }
            Save();
        }

        public static double GetSiteZoomLevel(string url)
        {
            string domain = NavigationHelper.ExtractDomain(url);
            if (string.IsNullOrWhiteSpace(domain)) return 1.0;
            lock (_lock)
            {
                if (_current.SiteZoomLevels != null && _current.SiteZoomLevels.TryGetValue(domain, out double zoom))
                {
                    return zoom;
                }
            }
            return 1.0;
        }

        public static void SetSiteZoomLevel(string url, double zoomFactor)
        {
            string domain = NavigationHelper.ExtractDomain(url);
            if (string.IsNullOrWhiteSpace(domain)) return;
            lock (_lock)
            {
                _current.SiteZoomLevels ??= new(StringComparer.OrdinalIgnoreCase);
                if (Math.Abs(zoomFactor - 1.0) < 0.01)
                {
                    _current.SiteZoomLevels.Remove(domain);
                }
                else
                {
                    _current.SiteZoomLevels[domain] = Math.Round(zoomFactor, 2);
                }
            }
            Save();
        }
    }
}
