using System;

namespace Browy.Models
{
    public class ExtensionItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string FolderPath { get; set; } = string.Empty;
    }
}
