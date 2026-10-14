using System;
using System.Collections.Generic;

namespace Browy.Services
{
    public static class ShieldService
    {
        // High-performance HashSet of top ad and tracking domains
        private static readonly HashSet<string> BlockedDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            // Google Ads & Tracking
            "doubleclick.net",
            "adservice.google.com",
            "googleadservices.com",
            "googlesyndication.com",
            "pagead2.googlesyndication.com",
            "tpc.googlesyndication.com",
            "google-analytics.com",
            "ssl.google-analytics.com",

            // Major Ad Exchanges & Networks
            "adnxs.com",
            "ib.adnxs.com",
            "amazon-adsystem.com",
            "aax.amazon-adsystem.com",
            "criteo.com",
            "static.criteo.net",
            "outbrain.com",
            "widgets.outbrain.com",
            "taboola.com",
            "trc.taboola.com",
            "rubiconproject.com",
            "pubmatic.com",
            "openx.net",
            "casalemedia.com",
            "adroll.com",
            "advertising.com",
            "smartadserver.com",
            "yieldmo.com",
            "bidswitch.net",
            "sharethrough.com",
            "revcontent.com",
            "infolinks.com",
            "media.net",
            "popcash.net",
            "popads.net",
            "propellerads.com",
            "zergnet.com",
            "mgid.com",

            // Trackers & Telemetry
            "scorecardresearch.com",
            "sb.scorecardresearch.com",
            "quantserve.com",
            "edge.quantserve.com",
            "hotjar.com",
            "static.hotjar.com",
            "clarity.ms",
            "c.clarity.ms",
            "mouseflow.com",
            "segment.io",
            "api.segment.io",
            "mixpanel.com",
            "api.mixpanel.com",
            "newrelic.com",
            "bam.nr-data.net",
            "optimizely.com",
            "logx.optimizely.com",
            "crazyegg.com"
        };

        private static readonly string[] BlockedUrlPatterns =
        {
            "/pagead/js/",
            "/ads/google",
            "/gtag/js?id=G-",
            "/fbevents.js",
            "connect.facebook.net/en_US/fbevents.js",
            "/adserver/",
            "/ads.js"
        };

        public static bool IsBlocked(string url, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(url)) return false;

            try
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                // Check domain and parent domains
                string host = uri.Host;
                if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                {
                    host = host.Substring(4);
                }

                if (BlockedDomains.Contains(host))
                {
                    reason = $"Ad/Tracker Domain: {host}";
                    return true;
                }

                // Check subdomains (e.g., ads.example.com -> example.com)
                string[] parts = host.Split('.');
                if (parts.Length > 2)
                {
                    for (int i = 1; i < parts.Length - 1; i++)
                    {
                        string parentDomain = string.Join(".", parts, i, parts.Length - i);
                        if (BlockedDomains.Contains(parentDomain))
                        {
                            reason = $"Ad/Tracker Network: {parentDomain}";
                            return true;
                        }
                    }
                }

                // Check specific script paths
                string path = uri.PathAndQuery;
                foreach (var pattern in BlockedUrlPatterns)
                {
                    if (path.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        reason = "Ad/Tracking Script Pattern";
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }
    }
}
