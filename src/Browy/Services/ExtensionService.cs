using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Browy.Models;
using Microsoft.Web.WebView2.Core;

namespace Browy.Services
{
    public static class ExtensionService
    {
        public static ObservableCollection<ExtensionItem> Extensions { get; } = new();

        public static async Task InitializeExtensionsAsync(CoreWebView2Profile profile)
        {
            var savedPaths = BrowserSettings.Current.InstalledExtensionPaths;
            foreach (var path in savedPaths.ToList())
            {
                if (Directory.Exists(path))
                {
                    await LoadExtensionFromFolderAsync(profile, path, saveToSettings: false);
                }
            }
        }

        public static async Task<ExtensionItem?> LoadExtensionFromFolderAsync(CoreWebView2Profile profile, string folderPath, bool saveToSettings = true)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return null;

            string manifestPath = Path.Combine(folderPath, "manifest.json");
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("manifest.json not found in extension directory.", manifestPath);

            string name = Path.GetFileName(folderPath);
            string version = "1.0";
            string description = string.Empty;

            try
            {
                string manifestJson = await File.ReadAllTextAsync(manifestPath);
                using var doc = JsonDocument.Parse(manifestJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("name", out var nameProp))
                    name = nameProp.GetString() ?? name;
                if (root.TryGetProperty("version", out var verProp))
                    version = verProp.GetString() ?? version;
                if (root.TryGetProperty("description", out var descProp))
                    description = descProp.GetString() ?? description;
            }
            catch { }

            string extId = string.Empty;
            try
            {
                var coreExt = await profile.AddBrowserExtensionAsync(folderPath);
                extId = coreExt.Id;
            }
            catch
            {
                extId = Guid.NewGuid().ToString("N").Substring(0, 16);
            }

            var item = Extensions.FirstOrDefault(e => e.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                item = new ExtensionItem
                {
                    Id = extId,
                    Name = name,
                    Version = version,
                    Description = description,
                    FolderPath = folderPath,
                    IsEnabled = true
                };
                Extensions.Add(item);
            }

            if (saveToSettings)
            {
                if (!BrowserSettings.Current.InstalledExtensionPaths.Contains(folderPath, StringComparer.OrdinalIgnoreCase))
                {
                    BrowserSettings.Current.InstalledExtensionPaths.Add(folderPath);
                    BrowserSettings.Save();
                }
            }

            return item;
        }

        public static async Task RemoveExtensionAsync(CoreWebView2Profile? profile, string extensionId)
        {
            var item = Extensions.FirstOrDefault(e => e.Id.Equals(extensionId, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                Extensions.Remove(item);
                BrowserSettings.Current.InstalledExtensionPaths.RemoveAll(p => p.Equals(item.FolderPath, StringComparison.OrdinalIgnoreCase));
                BrowserSettings.Save();

                if (profile != null)
                {
                    try
                    {
                        var exts = await profile.GetBrowserExtensionsAsync();
                        var target = exts.FirstOrDefault(x => x.Id.Equals(extensionId, StringComparison.OrdinalIgnoreCase));
                        if (target != null)
                        {
                            await target.RemoveAsync();
                        }
                    }
                    catch { }
                }
            }
        }

        public static async Task ToggleExtensionAsync(CoreWebView2Profile? profile, string extensionId, bool enabled)
        {
            var item = Extensions.FirstOrDefault(e => e.Id.Equals(extensionId, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                item.IsEnabled = enabled;
                if (profile != null)
                {
                    try
                    {
                        var exts = await profile.GetBrowserExtensionsAsync();
                        var target = exts.FirstOrDefault(x => x.Id.Equals(extensionId, StringComparison.OrdinalIgnoreCase));
                        if (target != null)
                        {
                            await target.EnableAsync(enabled);
                        }
                    }
                    catch { }
                }
            }
        }
    }
}
