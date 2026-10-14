using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Browy.Models;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class ExtensionServiceTests
    {
        [Fact]
        public void ExtensionItem_PropertiesInitializeCorrectly()
        {
            var ext = new ExtensionItem
            {
                Id = "ext_123",
                Name = "AdBlocker",
                Version = "2.1.0",
                Description = "Blocks web ads",
                IsEnabled = true,
                FolderPath = @"C:\Extensions\AdBlocker"
            };

            Assert.Equal("ext_123", ext.Id);
            Assert.Equal("AdBlocker", ext.Name);
            Assert.Equal("2.1.0", ext.Version);
            Assert.True(ext.IsEnabled);
        }

        [Fact]
        public void InternalHubService_GetExtensionsHtml_RendersCards()
        {
            var extensions = new[]
            {
                new ExtensionItem
                {
                    Id = "ext_darkreader",
                    Name = "Dark Reader",
                    Version = "4.9.58",
                    Description = "Dark mode for every website",
                    IsEnabled = true,
                    FolderPath = @"C:\Extensions\DarkReader"
                }
            };

            string html = InternalHubService.GetExtensionsHtml(extensions);

            Assert.Contains("Extensions", html);
            Assert.Contains("Dark Reader", html);
            Assert.Contains("v4.9.58", html);
            Assert.Contains("Dark mode for every website", html);
            Assert.Contains("ext_darkreader", html);
        }

        [Fact]
        public void InternalHubService_GetExtensionsHtml_RendersEmptyState()
        {
            string html = InternalHubService.GetExtensionsHtml(Enumerable.Empty<ExtensionItem>());

            Assert.Contains("No extensions installed", html);
            Assert.Contains("Load an unpacked Chrome or Edge extension", html);
        }
    }
}
