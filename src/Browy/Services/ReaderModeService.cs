using System;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Browy.Services
{
    public class ReaderArticle
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int WordCount { get; set; } = 0;
        public int ReadingTimeMinutes { get; set; } = 1;
    }

    public static class ReaderModeService
    {
        public const string ArticleDetectionScript = @"(() => {
            try {
                const p = document.querySelectorAll('article, p, .post-content, .article-content, [itemprop=""articleBody""]').length;
                return p >= 2 && (document.body ? document.body.innerText.length > 250 : false);
            } catch (e) {
                return false;
            }
        })()";

        public const string ExtractionScript = @"(() => {
            try {
                const getTitle = () => {
                    const ogTitle = document.querySelector('meta[property=""og:title""]')?.getAttribute('content');
                    if (ogTitle && ogTitle.trim().length > 0) return ogTitle.trim();
                    const h1 = document.querySelector('h1');
                    if (h1 && h1.innerText.trim().length > 0) return h1.innerText.trim();
                    return document.title || 'Untitled Article';
                };

                const getAuthor = () => {
                    const metaAuthor = document.querySelector('meta[name=""author""]')?.getAttribute('content')
                        || document.querySelector('meta[property=""article:author""]')?.getAttribute('content');
                    if (metaAuthor && metaAuthor.trim().length > 0) return metaAuthor.trim();
                    const el = document.querySelector('[rel=""author""], .byline, .author, .entry-author, .article-author');
                    return el ? el.innerText.trim() : '';
                };

                const getDate = () => {
                    const time = document.querySelector('time[datetime], meta[property=""article:published_time""]');
                    if (time) {
                        const dt = time.getAttribute('datetime') || time.getAttribute('content');
                        if (dt) {
                            try { return new Date(dt).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' }); } catch(e){}
                        }
                    }
                    return '';
                };

                const extractArticleContent = () => {
                    const candidateSelectors = [
                        'article',
                        '[itemprop=""articleBody""]',
                        '.article-content',
                        '.article-body',
                        '.post-content',
                        '.entry-content',
                        '.story-content',
                        '.content__article-body',
                        'main',
                        '[role=""main""]'
                    ];

                    let bestElem = null;
                    let maxPCount = 0;

                    for (const sel of candidateSelectors) {
                        const els = document.querySelectorAll(sel);
                        for (const el of els) {
                            const pCount = el.querySelectorAll('p').length;
                            if (pCount > maxPCount) {
                                maxPCount = pCount;
                                bestElem = el;
                            }
                        }
                    }

                    const target = bestElem ? bestElem.cloneNode(true) : document.body.cloneNode(true);

                    // Remove unwanted UI, ads, and noise
                    const junk = [
                        'script', 'style', 'noscript', 'iframe', 'svg', 'canvas',
                        'nav', 'header', 'footer', 'aside',
                        '.advertisement', '.ad', '.ads', '.sponsor', '.banner',
                        '.social-share', '.share-buttons', '.comments', '.newsletter',
                        '.cookie-banner', '.popup', '.sidebar', 'button', 'form',
                        '[role=""complementary""]', '[role=""navigation""]'
                    ];
                    junk.forEach(sel => {
                        target.querySelectorAll(sel).forEach(n => n.remove());
                    });

                    // Neutralize attributes that break clean reading
                    target.querySelectorAll('*').forEach(n => {
                        n.removeAttribute('style');
                        n.removeAttribute('color');
                        n.removeAttribute('bgcolor');
                        if (n.tagName === 'A') {
                            n.setAttribute('target', '_blank');
                            n.setAttribute('rel', 'noopener noreferrer');
                        }
                    });

                    return target.innerHTML;
                };

                const title = getTitle();
                const author = getAuthor();
                const date = getDate();
                const content = extractArticleContent();
                const domain = window.location.hostname || '';
                const url = window.location.href || '';

                const textOnly = content.replace(/<[^>]*>/g, ' ');
                const words = textOnly.trim().split(/\s+/).filter(w => w.length > 0).length;
                const readingMinutes = Math.max(1, Math.round(words / 200));

                return JSON.stringify({
                    title: title,
                    author: author,
                    date: date,
                    domain: domain,
                    url: url,
                    content: content,
                    wordCount: words,
                    readingTimeMinutes: readingMinutes
                });
            } catch (err) {
                return JSON.stringify({
                    title: document.title || 'Article',
                    author: '',
                    date: '',
                    domain: window.location.hostname || '',
                    url: window.location.href || '',
                    content: document.body ? document.body.innerHTML : '',
                    wordCount: 0,
                    readingTimeMinutes: 1,
                    error: err.toString()
                });
            }
        })()";

        public static ReaderArticle? ParseExtractionResult(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return null;
                // If wrapped in quotes from ExecuteScriptAsync, deserialize string first
                if (json.StartsWith("\"") && json.EndsWith("\""))
                {
                    json = JsonSerializer.Deserialize<string>(json) ?? json;
                }

                return JsonSerializer.Deserialize<ReaderArticle>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        public static string GenerateReaderHtml(ReaderArticle article)
        {
            string safeTitle = WebUtility.HtmlEncode(article.Title);
            string safeAuthor = WebUtility.HtmlEncode(article.Author);
            string safeDate = WebUtility.HtmlEncode(article.Date);
            string safeDomain = WebUtility.HtmlEncode(article.Domain);
            string safeUrl = WebUtility.HtmlEncode(article.Url);

            string bylineHtml = string.Empty;
            if (!string.IsNullOrWhiteSpace(safeAuthor) || !string.IsNullOrWhiteSpace(safeDate))
            {
                string authorPart = !string.IsNullOrWhiteSpace(safeAuthor) ? $"<span>By <strong>{safeAuthor}</strong></span>" : "";
                string dot = (!string.IsNullOrWhiteSpace(safeAuthor) && !string.IsNullOrWhiteSpace(safeDate)) ? "<span class=\"sep\">•</span>" : "";
                string datePart = !string.IsNullOrWhiteSpace(safeDate) ? $"<span>{safeDate}</span>" : "";
                bylineHtml = $"<div class=\"byline\">{authorPart}{dot}{datePart}</div>";
            }

            return $@"<!DOCTYPE html>
<html lang=""en"" data-theme=""dark"" data-font=""sans"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{safeTitle} - Browy Reader</title>
    <style>
        :root {{
            --bg-body: #121214;
            --bg-nav: rgba(24, 24, 27, 0.85);
            --border-nav: rgba(255, 255, 255, 0.12);
            --text-primary: #F4F4F5;
            --text-secondary: #A1A1AA;
            --text-muted: #71717A;
            --accent: #38BDF8;
            --accent-hover: #0EA5E9;
            --btn-bg: rgba(255, 255, 255, 0.08);
            --btn-hover: rgba(255, 255, 255, 0.15);
            --card-bg: rgba(255, 255, 255, 0.04);
            --font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            --font-size: 18px;
            --line-height: 1.75;
            --max-width: 720px;
        }}

        html[data-theme=""sepia""] {{
            --bg-body: #FBF0D9;
            --bg-nav: rgba(244, 231, 203, 0.88);
            --border-nav: rgba(90, 60, 30, 0.15);
            --text-primary: #332722;
            --text-secondary: #6B5B53;
            --text-muted: #8C7B73;
            --accent: #9A5A2B;
            --accent-hover: #7E451C;
            --btn-bg: rgba(90, 60, 30, 0.08);
            --btn-hover: rgba(90, 60, 30, 0.15);
            --card-bg: rgba(90, 60, 30, 0.04);
        }}

        html[data-theme=""light""] {{
            --bg-body: #FFFFFF;
            --bg-nav: rgba(255, 255, 255, 0.90);
            --border-nav: rgba(0, 0, 0, 0.10);
            --text-primary: #18181B;
            --text-secondary: #52525B;
            --text-muted: #71717A;
            --accent: #0284C7;
            --accent-hover: #0369A1;
            --btn-bg: rgba(0, 0, 0, 0.05);
            --btn-hover: rgba(0, 0, 0, 0.10);
            --card-bg: rgba(0, 0, 0, 0.03);
        }}

        html[data-font=""serif""] {{
            --font-family: 'Merriweather', 'Georgia', 'Cambria', serif;
        }}

        html[data-font=""mono""] {{
            --font-family: 'Cascadia Code', 'Consolas', 'Courier New', monospace;
        }}

        * {{
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }}

        body {{
            background-color: var(--bg-body);
            color: var(--text-primary);
            font-family: var(--font-family);
            font-size: var(--font-size);
            line-height: var(--line-height);
            transition: background-color 0.2s ease, color 0.2s ease;
            -webkit-font-smoothing: antialiased;
        }}

        /* Floating Top Header Bar */
        .reader-header {{
            position: sticky;
            top: 0;
            z-index: 100;
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 8px 24px;
            background: var(--bg-nav);
            backdrop-filter: blur(16px);
            -webkit-backdrop-filter: blur(16px);
            border-bottom: 1px solid var(--border-nav);
            gap: 12px;
        }}

        .header-left, .header-right {{
            display: flex;
            align-items: center;
            gap: 8px;
        }}

        .btn {{
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 6px 12px;
            background: var(--btn-bg);
            color: var(--text-primary);
            border: 1px solid var(--border-nav);
            border-radius: 6px;
            font-size: 13px;
            font-weight: 500;
            cursor: pointer;
            transition-property: background-color, border-color, color, transform;
            transition-duration: 140ms;
            transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
            user-select: none;
            outline: none;
        }}

        .btn:hover {{
            background: var(--btn-hover);
            border-color: var(--accent);
        }}

        .btn.active {{
            background: var(--accent);
            color: #FFFFFF;
            border-color: var(--accent);
        }}

        .badge {{
            font-size: 12px;
            color: var(--text-secondary);
            display: inline-flex;
            align-items: center;
            gap: 6px;
        }}

        .badge-source {{
            color: var(--accent);
            text-decoration: none;
            font-weight: 500;
        }}

        .badge-source:hover {{
            text-decoration: underline;
        }}

        .control-group {{
            display: flex;
            align-items: center;
            background: var(--btn-bg);
            border: 1px solid var(--border-nav);
            border-radius: 6px;
            padding: 2px;
            gap: 2px;
        }}

        .control-group .btn-icon {{
            background: transparent;
            border: none;
            color: var(--text-primary);
            padding: 4px 8px;
            font-size: 12px;
            font-weight: 600;
            border-radius: 4px;
            cursor: pointer;
            transition: background 0.15s ease;
        }}

        .control-group .btn-icon:hover {{
            background: var(--btn-hover);
        }}

        .theme-pill {{
            width: 20px;
            height: 20px;
            border-radius: 50%;
            border: 2px solid transparent;
            cursor: pointer;
            transition: transform 0.15s ease, border-color 0.15s ease;
        }}

        .theme-pill:hover {{
            transform: scale(1.1);
        }}

        .theme-pill.active {{
            border-color: var(--accent);
            transform: scale(1.15);
        }}

        .theme-pill.dark {{ background: #18181B; }}
        .theme-pill.sepia {{ background: #FBF0D9; border: 1px solid #D6C7AA; }}
        .theme-pill.light {{ background: #FFFFFF; border: 1px solid #D4D4D8; }}

        /* Main Reading Container */
        .reader-container {{
            max-width: var(--max-width);
            margin: 0 auto;
            padding: 40px 24px 80px 24px;
        }}

        .article-title {{
            font-size: 2.2em;
            line-height: 1.25;
            font-weight: 800;
            margin-bottom: 16px;
            letter-spacing: -0.02em;
            color: var(--text-primary);
        }}

        .byline {{
            display: flex;
            align-items: center;
            flex-wrap: wrap;
            gap: 8px;
            font-size: 0.88em;
            color: var(--text-secondary);
            margin-bottom: 24px;
        }}

        .byline .sep {{
            color: var(--text-muted);
        }}

        .divider {{
            height: 1px;
            background: var(--border-nav);
            margin-bottom: 36px;
        }}

        /* Content Styling */
        .article-content {{
            font-size: 1em;
            line-height: var(--line-height);
        }}

        .article-content p {{
            margin-bottom: 1.5em;
        }}

        .article-content h1,
        .article-content h2,
        .article-content h3,
        .article-content h4 {{
            color: var(--text-primary);
            font-weight: 700;
            line-height: 1.35;
            margin-top: 1.8em;
            margin-bottom: 0.8em;
        }}

        .article-content h2 {{ font-size: 1.5em; }}
        .article-content h3 {{ font-size: 1.25em; }}

        .article-content a {{
            color: var(--accent);
            text-decoration: underline;
            text-underline-offset: 3px;
        }}

        .article-content a:hover {{
            color: var(--accent-hover);
        }}

        .article-content img {{
            max-width: 100%;
            height: auto;
            border-radius: 8px;
            margin: 1.5em 0;
            display: block;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
        }}

        .article-content blockquote {{
            border-left: 3px solid var(--accent);
            padding: 12px 20px;
            margin: 1.6em 0;
            background: var(--card-bg);
            border-radius: 0 8px 8px 0;
            color: var(--text-secondary);
            font-style: italic;
        }}

        .article-content ul, .article-content ol {{
            margin: 1.4em 0 1.4em 1.8em;
        }}

        .article-content li {{
            margin-bottom: 0.5em;
        }}

        .article-content pre, .article-content code {{
            font-family: 'Cascadia Code', 'Consolas', monospace;
            font-size: 0.9em;
            background: var(--card-bg);
            border-radius: 4px;
            padding: 2px 6px;
        }}

        .article-content pre {{
            padding: 16px;
            border-radius: 8px;
            overflow-x: auto;
            margin: 1.5em 0;
            border: 1px solid var(--border-nav);
        }}

        .article-content pre code {{
            padding: 0;
            background: transparent;
        }}

        /* Footer */
        .reader-footer {{
            margin-top: 60px;
            padding-top: 24px;
            border-top: 1px solid var(--border-nav);
            text-align: center;
            font-size: 0.85em;
            color: var(--text-muted);
        }}

        .reader-footer a {{
            color: var(--accent);
            text-decoration: none;
        }}

        .reader-footer a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <header class=""reader-header"">
        <div class=""header-left"">
            <button class=""btn"" onclick=""exitReader()"" title=""Exit Reader View (Escape or Ctrl+Shift+R)"">
                <span>&#x2190;</span> Exit Reader
            </button>
            <div class=""badge"">
                <span>{article.ReadingTimeMinutes} min read ({article.WordCount:N0} words)</span>
                <span class=""sep"">&bull;</span>
                <a href=""{safeUrl}"" target=""_blank"" class=""badge-source"">{safeDomain}</a>
            </div>
        </div>

        <div class=""header-right"">
            <!-- Text-to-Speech Read Aloud -->
            <button class=""btn"" id=""tts-btn"" onclick=""toggleReadAloud()"" title=""Read article aloud"">
                <span id=""tts-icon"">&#x1F50A;</span> <span id=""tts-text"">Read Aloud</span>
            </button>

            <!-- Font Selection -->
            <div class=""control-group"">
                <button class=""btn-icon"" onclick=""setFont('sans')"" id=""font-sans"">Sans</button>
                <button class=""btn-icon"" onclick=""setFont('serif')"" id=""font-serif"">Serif</button>
                <button class=""btn-icon"" onclick=""setFont('mono')"" id=""font-mono"">Mono</button>
            </div>

            <!-- Font Size -->
            <div class=""control-group"">
                <button class=""btn-icon"" onclick=""changeFontSize(-2)"" title=""Decrease Font Size"">A&minus;</button>
                <button class=""btn-icon"" onclick=""changeFontSize(2)"" title=""Increase Font Size"">A+</button>
            </div>

            <!-- Theme Picker -->
            <div style=""display: flex; gap: 6px; align-items: center; margin-left: 6px;"">
                <div class=""theme-pill dark active"" onclick=""setTheme('dark')"" title=""Dark Theme""></div>
                <div class=""theme-pill sepia"" onclick=""setTheme('sepia')"" title=""Sepia Theme""></div>
                <div class=""theme-pill light"" onclick=""setTheme('light')"" title=""Light Theme""></div>
            </div>
        </div>
    </header>

    <main class=""reader-container"">
        <h1 class=""article-title"">{safeTitle}</h1>
        {bylineHtml}
        <div class=""divider""></div>
        <article class=""article-content"">
            {article.Content}
        </article>
        <footer class=""reader-footer"">
            End of article &bull; <a href=""javascript:void(0)"" onclick=""exitReader()"">Return to original page</a>
        </footer>
    </main>

    <script>
        function exitReader() {{
            if ('speechSynthesis' in window) {{
                window.speechSynthesis.cancel();
            }}
            if (window.chrome && window.chrome.webview) {{
                window.chrome.webview.postMessage({{ action: 'exit_reader' }});
            }} else {{
                history.back();
            }}
        }}

        let isSpeaking = false;
        function toggleReadAloud() {{
            if (!('speechSynthesis' in window)) {{
                alert('Text-to-speech is not supported in this environment.');
                return;
            }}
            if (isSpeaking) {{
                window.speechSynthesis.cancel();
                isSpeaking = false;
                document.getElementById('tts-icon').innerHTML = '&#x1F50A;';
                document.getElementById('tts-text').innerText = 'Read Aloud';
                document.getElementById('tts-btn').classList.remove('active');
            }} else {{
                const articleElem = document.querySelector('.article-content');
                const text = (articleElem ? articleElem.innerText : '') || document.body.innerText;
                if (!text || !text.trim()) return;

                const utter = new SpeechSynthesisUtterance(text.trim());
                utter.onend = () => {{
                    isSpeaking = false;
                    document.getElementById('tts-icon').innerHTML = '&#x1F50A;';
                    document.getElementById('tts-text').innerText = 'Read Aloud';
                    document.getElementById('tts-btn').classList.remove('active');
                }};
                utter.onerror = () => {{
                    isSpeaking = false;
                    document.getElementById('tts-icon').innerHTML = '&#x1F50A;';
                    document.getElementById('tts-text').innerText = 'Read Aloud';
                    document.getElementById('tts-btn').classList.remove('active');
                }};
                window.speechSynthesis.speak(utter);
                isSpeaking = true;
                document.getElementById('tts-icon').innerHTML = '&#x23F8;';
                document.getElementById('tts-text').innerText = 'Pause';
                document.getElementById('tts-btn').classList.add('active');
            }}
        }}

        window.addEventListener('keydown', (e) => {{
            if (e.key === 'Escape' || e.key === 'F9' || (e.ctrlKey && e.shiftKey && e.key === 'R')) {{
                exitReader();
            }}
        }});

        function setTheme(theme) {{
            document.documentElement.setAttribute('data-theme', theme);
            document.querySelectorAll('.theme-pill').forEach(p => p.classList.remove('active'));
            const activePill = document.querySelector('.theme-pill.' + theme);
            if (activePill) activePill.classList.add('active');
            localStorage.setItem('browy_reader_theme', theme);
        }}

        function setFont(font) {{
            document.documentElement.setAttribute('data-font', font);
            document.querySelectorAll('#font-sans, #font-serif, #font-mono').forEach(b => b.style.color = '');
            const btn = document.getElementById('font-' + font);
            if (btn) btn.style.color = 'var(--accent)';
            localStorage.setItem('browy_reader_font', font);
        }}

        let currentFontSize = 18;
        function changeFontSize(delta) {{
            currentFontSize = Math.min(32, Math.max(13, currentFontSize + delta));
            document.documentElement.style.setProperty('--font-size', currentFontSize + 'px');
            localStorage.setItem('browy_reader_fontsize', currentFontSize);
        }}

        // Restore user preferences
        (() => {{
            const savedTheme = localStorage.getItem('browy_reader_theme') || 'dark';
            setTheme(savedTheme);

            const savedFont = localStorage.getItem('browy_reader_font') || 'sans';
            setFont(savedFont);

            const savedSize = parseInt(localStorage.getItem('browy_reader_fontsize'), 10);
            if (!isNaN(savedSize)) {{
                currentFontSize = savedSize;
                document.documentElement.style.setProperty('--font-size', currentFontSize + 'px');
            }}
        }})();
    </script>
</body>
</html>";
        }
    }
}
