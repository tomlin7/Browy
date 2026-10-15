using System;

namespace Browy.Models
{
    public class FeatureFlagDefinition
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public bool DefaultValue { get; set; }

        public FeatureFlagDefinition() { }

        public FeatureFlagDefinition(string key, string name, string description, string category, bool defaultValue)
        {
            Key = key;
            Name = name;
            Description = description;
            Category = category;
            DefaultValue = defaultValue;
        }
    }
}
