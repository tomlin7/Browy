using System;
using System.IO;
using System.Text.Json;

namespace Browy.Services
{
    public class BrowserSettingsData
    {
        public string SearchEngineName { get; set; } = "Google";
        public string SearchEngineUrl { get; set; } = "https://www.google.com/search?q=";
        public string StartPageUrl { get; set; } = "https://www.google.com";
        public bool ShowBookmarksBar { get; set; } = true;
        public bool IsAcrylicEnabled { get; set; } = true;
        public bool BlockThirdPartyCookies { get; set; } = true;
        public bool DoNotTrack { get; set; } = true;
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
    }
}
