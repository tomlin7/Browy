using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class PageEnhancementServiceTests
    {
        [Fact]
        public void BuildInjectedStyle_EmptyWhenAllDisabled()
        {
            string script = PageEnhancementService.BuildInjectedStyle(false, false, false);
            Assert.Equal(string.Empty, script);
        }

        [Fact]
        public void BuildInjectedStyle_GeneratesForceDarkCss()
        {
            string script = PageEnhancementService.BuildInjectedStyle(forceDark: true, smoothScroll: false, cosmeticAdBlock: false);
            Assert.NotEmpty(script);
            Assert.Contains("invert(90%)", script);
            Assert.Contains("__browy_page_enhancements__", script);
        }

        [Fact]
        public void BuildInjectedStyle_GeneratesSmoothScrollCss()
        {
            string script = PageEnhancementService.BuildInjectedStyle(forceDark: false, smoothScroll: true, cosmeticAdBlock: false);
            Assert.NotEmpty(script);
            Assert.Contains("scroll-behavior: smooth", script);
        }

        [Fact]
        public void BuildInjectedStyle_GeneratesCosmeticAdBlockCss()
        {
            string script = PageEnhancementService.BuildInjectedStyle(forceDark: false, smoothScroll: false, cosmeticAdBlock: true);
            Assert.NotEmpty(script);
            Assert.Contains("google_ads", script);
            Assert.Contains("adsbygoogle", script);
            Assert.Contains("display: none !important", script);
        }

        [Fact]
        public void BuildInjectedStyle_CombinesMultipleStyles()
        {
            string script = PageEnhancementService.BuildInjectedStyle(forceDark: true, smoothScroll: true, cosmeticAdBlock: true);
            Assert.Contains("invert(90%)", script);
            Assert.Contains("scroll-behavior: smooth", script);
            Assert.Contains("adsbygoogle", script);
        }
    }
}
