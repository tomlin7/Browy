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

        private static readonly HashSet<string> WhitelistedDomains = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> DynamicBlockedPatterns = new();

        public static int BlockedDomainCount => BlockedDomains.Count;

        public static void AddToWhitelist(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain)) return;
            string clean = domain.Trim().ToLowerInvariant();
            if (clean.StartsWith("www.")) clean = clean.Substring(4);
            lock (WhitelistedDomains)
            {
                WhitelistedDomains.Add(clean);
            }
        }

        public static void RemoveFromWhitelist(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain)) return;
            string clean = domain.Trim().ToLowerInvariant();
            if (clean.StartsWith("www.")) clean = clean.Substring(4);
            lock (WhitelistedDomains)
            {
                WhitelistedDomains.Remove(clean);
            }
        }

        public static bool IsWhitelisted(string domainOrUrl)
        {
            if (string.IsNullOrWhiteSpace(domainOrUrl)) return false;
            string clean = domainOrUrl.Trim().ToLowerInvariant();
            if (clean.StartsWith("http://") || clean.StartsWith("https://"))
            {
                clean = NavigationHelper.ExtractDomain(clean);
            }
            if (clean.StartsWith("www.")) clean = clean.Substring(4);

            lock (WhitelistedDomains)
            {
                return WhitelistedDomains.Contains(clean);
            }
        }

        public static void AddCustomFilterDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain)) return;
            string clean = domain.Trim().ToLowerInvariant();
            if (clean.StartsWith("www.")) clean = clean.Substring(4);
            lock (BlockedDomains)
            {
                BlockedDomains.Add(clean);
            }
        }

        public static void AddCustomFilterPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return;
            lock (DynamicBlockedPatterns)
            {
                DynamicBlockedPatterns.Add(pattern.Trim());
            }
        }

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

                if (IsWhitelisted(host))
                {
                    return false;
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

                lock (DynamicBlockedPatterns)
                {
                    foreach (var pattern in DynamicBlockedPatterns)
                    {
                        if (path.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            reason = "Custom Filter Pattern";
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }
    }
}
