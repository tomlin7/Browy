using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace Browy.Services
{
    public static class PageEnhancementService
    {
        public const string ForceDarkCss = @"
html {
    filter: invert(90%) hue-rotate(180deg) !important;
    background: #121212 !important;
}
img, video, picture, canvas, svg, [style*='background-image'] {
    filter: invert(100%) hue-rotate(180deg) !important;
}
iframe {
    filter: invert(100%) hue-rotate(180deg) !important;
}";

        public const string SmoothScrollCss = @"
html {
    scroll-behavior: smooth !important;
}";

        public const string CosmeticAdBlockCss = @"
[id*='google_ads'], [class*='adsbygoogle'], [id*='ad-banner'], [class*='ad-banner'], 
[id*='ad-container'], [class*='ad-container'], [id*='ad_slot'], [class*='ad_slot'],
[id^='ad_'], [class^='ad_'], [id$='-ad'], [class$='-ad'] {
    display: none !important;
    height: 0 !important;
    min-height: 0 !important;
    visibility: hidden !important;
    pointer-events: none !important;
}";

        public static string BuildInjectedStyle(bool forceDark, bool smoothScroll, bool cosmeticAdBlock)
        {
            var sb = new System.Text.StringBuilder();

            if (forceDark)
            {
                sb.AppendLine(ForceDarkCss);
            }
            if (smoothScroll)
            {
                sb.AppendLine(SmoothScrollCss);
            }
            if (cosmeticAdBlock)
            {
                sb.AppendLine(CosmeticAdBlockCss);
            }

            if (sb.Length == 0) return string.Empty;

            string escapedCss = sb.ToString()
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "")
                .Replace("\n", " ");

            return $@"(() => {{
                try {{
                    const styleId = '__browy_page_enhancements__';
                    let el = document.getElementById(styleId);
                    if (!el) {{
                        el = document.createElement('style');
                        el.id = styleId;
                        document.head ? document.head.appendChild(el) : document.documentElement.appendChild(el);
                    }}
                    el.textContent = ""{escapedCss}"";
                }} catch (e) {{ }}
            }})();";
        }

        public static async Task ApplyActiveEnhancementsAsync(CoreWebView2? core, string? url)
        {
            if (core == null || string.IsNullOrWhiteSpace(url)) return;
            if (url.StartsWith("browy://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            bool forceDark = BrowserSettings.IsFeatureEnabled("experimental_force_dark", false);
            bool smoothScroll = BrowserSettings.IsFeatureEnabled("experimental_smooth_scroll", true);
            bool cosmeticAdBlock = BrowserSettings.IsFeatureEnabled("experimental_adblock_cosmetic", true) && BrowserSettings.Current.IsShieldEnabled;

            if (!forceDark && !smoothScroll && !cosmeticAdBlock)
            {
                return;
            }

            string script = BuildInjectedStyle(forceDark, smoothScroll, cosmeticAdBlock);
            if (!string.IsNullOrEmpty(script))
            {
                try
                {
                    await core.ExecuteScriptAsync(script);
                }
                catch { }
            }
        }
    }
}
