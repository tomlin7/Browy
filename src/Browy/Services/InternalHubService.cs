using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Browy.Models;

namespace Browy.Services
{
    public static class InternalHubService
    {
        public static string GetHistoryHtml(IEnumerable<HistoryItem> history)
        {
            string accent = BrowserSettings.Current.AccentColorHex;
            var items = history.ToList();
            if (items.Count == 0)
            {
                return GetEmptyHistoryHtml();
            }

            var now = DateTime.Now;
            var today = now.Date;
            var yesterday = today.AddDays(-1);
            var thisWeek = today.AddDays(-7);

            // Group into date buckets
            var todayItems = items.Where(i => i.VisitedAt.Date == today).ToList();
            var yesterdayItems = items.Where(i => i.VisitedAt.Date == yesterday).ToList();
            var weekItems = items.Where(i => i.VisitedAt.Date < yesterday && i.VisitedAt.Date >= thisWeek).ToList();
            var olderItems = items.Where(i => i.VisitedAt.Date < thisWeek).ToList();

            string RenderBucket(string bucketTitle, List<HistoryItem> bucketItems)
            {
                if (bucketItems.Count == 0) return string.Empty;
                var sb = new System.Text.StringBuilder();
                sb.Append($@"<div class='bucket-section'>
                    <div class='bucket-title'><span>{bucketTitle}</span> <span class='bucket-badge'>{bucketItems.Count}</span></div>
                    <div class='bucket-list'>");
                for (int i = 0; i < bucketItems.Count; i++)
                {
                    var item = bucketItems[i];
                    string domain = NavigationHelper.ExtractDomain(item.Url);
                    string title = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(item.Title) ? domain : item.Title);
                    string url = WebUtility.HtmlEncode(item.Url);
                    string time = WebUtility.HtmlEncode(item.FormattedTime);

                    sb.Append($@"
                    <div class='history-card' data-url='{url}' data-title='{title}' data-domain='{domain}'>
                        <input type='checkbox' class='hist-check' data-url='{url}' onchange='updateSelection()'>
                        <div class='card-icon'>&#xE774;</div>
                        <div class='card-info' onclick=""window.chrome.webview.postMessage(JSON.stringify({{action: 'navigate', url: '{url}'}}))"">
                            <div class='card-title'>{title}</div>
                            <div class='card-url'>{url}</div>
                        </div>
                        <div class='card-time'>{time}</div>
                        <button class='card-delete' onclick=""deleteItem('{url}')"" title='Delete from history'>&#xE74D;</button>
                    </div>");
                }
                sb.Append("</div></div>");
                return sb.ToString();
            }

            string timelineHtml = RenderBucket("Today", todayItems) +
                                 RenderBucket("Yesterday", yesterdayItems) +
                                 RenderBucket("This Week", weekItems) +
                                 RenderBucket("Earlier", olderItems);

            // Group by domain
            var domainGroups = items
                .GroupBy(i => NavigationHelper.ExtractDomain(i.Url))
                .OrderByDescending(g => g.Count())
                .ToList();

            var domainSb = new System.Text.StringBuilder();
            foreach (var g in domainGroups)
            {
                string domain = WebUtility.HtmlEncode(g.Key);
                int count = g.Count();

                domainSb.Append($@"
                <div class='domain-card' data-domain='{domain}'>
                    <div class='domain-header' onclick=""toggleDomainGroup(this)"">
                        <div class='domain-info'>
                            <span class='domain-icon'>&#xE774;</span>
                            <span class='domain-name'>{domain}</span>
                            <span class='bucket-badge'>{count} visit{(count > 1 ? "s" : "")}</span>
                        </div>
                        <div class='domain-actions'>
                            <button class='btn-danger-sm' onclick=""event.stopPropagation(); deleteDomain('{domain}')"" title='Delete all visits from this domain'>Delete Domain</button>
                            <span class='chevron'>&#xE70D;</span>
                        </div>
                    </div>
                    <div class='domain-items' style='display:none;'>");

                foreach (var item in g)
                {
                    string title = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(item.Title) ? domain : item.Title);
                    string url = WebUtility.HtmlEncode(item.Url);
                    string time = WebUtility.HtmlEncode(item.FormattedTime);

                    domainSb.Append($@"
                        <div class='history-card' data-url='{url}' data-title='{title}' data-domain='{domain}'>
                            <input type='checkbox' class='hist-check' data-url='{url}' onchange='updateSelection()'>
                            <div class='card-info' onclick=""window.chrome.webview.postMessage(JSON.stringify({{action: 'navigate', url: '{url}'}}))"">
                                <div class='card-title'>{title}</div>
                                <div class='card-url'>{url}</div>
                            </div>
                            <div class='card-time'>{time}</div>
                            <button class='card-delete' onclick=""deleteItem('{url}')"" title='Delete entry'>&#xE74D;</button>
                        </div>");
                }
                domainSb.Append("</div></div>");
            }

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<title>History - Browy</title>
<style>

  :root {{
    --bg-main: #0b0c0e;
    --bg-card: #141619;
    --bg-card-hover: #181b20;
    --border-color: rgba(255, 255, 255, 0.07);
    --border-subtle: rgba(255, 255, 255, 0.05);
    --border-hover: rgba(255, 255, 255, 0.16);
    --text-primary: #f4f4f5;
    --text-secondary: #a1a1aa;
    --text-muted: #71717a;
    --accent: {accent};
    --accent-hover: {accent};
    --danger-subtle: rgba(239, 68, 68, 0.12);
    --danger-border: rgba(239, 68, 68, 0.28);
    --font-sans: 'Segoe UI Variable Text', 'Segoe UI Variable Display', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    --font-mono: 'Cascadia Code', Consolas, 'SF Mono', monospace;
  }}
  * {{ box-sizing: border-box; margin: 0; padding: 0; user-select: none; -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale; }}

  body {{ background-color: var(--bg-main); color: var(--text-primary); font-family: var(--font-sans); min-height: 100vh; padding: 36px 24px; display: flex; justify-content: center; }}
  .container {{ width: 100%; max-width: 860px; }}
  .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }}
  .title-area {{ display: flex; align-items: center; gap: 14px; }}
  .header-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 26px; color: var(--accent); }}
  h1 {{ font-size: 22px; font-weight: 600; letter-spacing: -0.02em; color: var(--text-primary); }}
  .header-actions {{ display: flex; align-items: center; gap: 8px; }}

  .btn-primary {{
    background: #f4f4f5;
    color: #0b0c0e;
    border: 1px solid transparent;
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, color, transform, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-primary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-primary:hover {{ background: #ffffff; box-shadow: 0 2px 8px rgba(0,0,0,0.3); }}
  }}

  .btn-secondary {{
    background: transparent;
    color: var(--text-primary);
    border: 1px solid var(--border-color);
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-secondary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-secondary:hover {{ background: rgba(255, 255, 255, 0.05); border-color: var(--border-hover); }}
  }}

  .btn-danger {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 7px 14px;
    border-radius: 6px;
    cursor: pointer;
    font-size: 12.5px;
    font-weight: 500;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .btn-danger-sm {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 3px 9px;
    border-radius: 5px;
    cursor: pointer;
    font-size: 11.5px;
    font-weight: 500;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger-sm:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger-sm:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .tabs {{ display: flex; gap: 6px; margin-bottom: 18px; border-bottom: 1px solid var(--border-subtle); padding-bottom: 8px; }}
  .tab-btn {{
    background: transparent;
    border: 1px solid transparent;
    color: var(--text-secondary);
    font-size: 12.5px;
    font-weight: 500;
    padding: 6px 12px;
    border-radius: 6px;
    cursor: pointer;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, color, border-color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .tab-btn:not(:disabled):active {{ transform: scale(0.98); }}
  @media (hover: hover) {{
    .tab-btn:hover {{ background: rgba(255, 255, 255, 0.04); color: var(--text-primary); }}
  }}
  .tab-btn.active {{ background: rgba(255, 255, 255, 0.08); color: var(--text-primary); font-weight: 600; border-color: var(--border-color); }}

  .toolbar {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 18px; gap: 12px; }}
  .search-bar {{
    flex: 1;
    background: #101215;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 8px 14px;
    color: var(--text-primary);
    font-size: 13px;
    outline: none;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: border-color, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .search-bar:focus {{ border-color: rgba(255, 255, 255, 0.28); box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.15); }}
  .batch-actions {{ display: flex; align-items: center; gap: 10px; }}

  .bucket-section {{ margin-bottom: 24px; }}
  .bucket-title {{ font-size: 13px; font-weight: 600; color: var(--text-secondary); margin-bottom: 10px; display: flex; align-items: center; gap: 8px; letter-spacing: -0.01em; }}
  .bucket-badge {{ background: rgba(255, 255, 255, 0.05); border: 1px solid var(--border-color); color: var(--text-muted); font-size: 11px; font-family: var(--font-mono); padding: 1px 6px; border-radius: 4px; font-weight: 500; }}
  .bucket-list {{ display: flex; flex-direction: column; gap: 6px; }}

  .history-card {{
    display: flex;
    align-items: center;
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 10px 14px;
    gap: 12px;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.02);
    transition-property: background-color, border-color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  @media (hover: hover) {{
    .history-card:hover {{ background: var(--bg-card-hover); border-color: var(--border-hover); }}
  }}
  .hist-check {{ width: 15px; height: 15px; cursor: pointer; accent-color: var(--accent); }}
  .card-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 15px; color: var(--text-muted); width: 18px; text-align: center; }}
  .card-info {{ flex: 1; min-width: 0; cursor: pointer; }}
  .card-title {{ font-size: 13px; font-weight: 500; color: var(--text-primary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }}
  .card-url {{ font-size: 11.5px; font-family: var(--font-mono); color: var(--text-muted); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-top: 2px; }}
  .card-time {{ font-size: 11.5px; font-family: var(--font-mono); color: var(--text-muted); margin-right: 6px; }}
  .card-delete {{
    background: transparent;
    border: none;
    font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets';
    font-size: 13px;
    color: var(--text-muted);
    width: 26px;
    height: 26px;
    border-radius: 5px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, color;
    transition-duration: 120ms;
  }}
  @media (hover: hover) {{
    .card-delete:hover {{ background: var(--danger-subtle); color: #fca5a5; }}
  }}

  .domain-card {{ background: var(--bg-card); border: 1px solid var(--border-color); border-radius: 6px; margin-bottom: 8px; overflow: hidden; }}
  .domain-header {{ padding: 12px 16px; display: flex; justify-content: space-between; align-items: center; cursor: pointer; background: var(--bg-card); transition-property: background-color; transition-duration: 120ms; }}
  @media (hover: hover) {{
    .domain-header:hover {{ background: var(--bg-card-hover); }}
  }}
  .domain-info {{ display: flex; align-items: center; gap: 10px; }}
  .domain-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 15px; color: var(--accent); }}
  .domain-name {{ font-size: 13px; font-weight: 600; color: var(--text-primary); }}
  .domain-actions {{ display: flex; align-items: center; gap: 10px; }}
  .chevron {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 11px; color: var(--text-muted); transition-property: transform; transition-duration: 140ms; }}
  .domain-items {{ padding: 8px 12px; display: flex; flex-direction: column; gap: 5px; background: #0e1013; border-top: 1px solid var(--border-subtle); }}

  .modal {{ display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.65); backdrop-filter: blur(8px); align-items: center; justify-content: center; z-index: 100; overscroll-behavior: contain; }}
  .modal-content {{ background: var(--bg-card); border: 1px solid var(--border-color); border-radius: 8px; padding: 22px 24px; width: 380px; box-shadow: 0 24px 48px rgba(0,0,0,0.6), inset 0 1px 0 rgba(255,255,255,0.03); }}
  .modal-title {{ font-size: 16px; font-weight: 600; margin-bottom: 14px; color: var(--text-primary); letter-spacing: -0.01em; }}
  .timeframe-select {{ width: 100%; background: #101215; border: 1px solid var(--border-color); color: var(--text-primary); padding: 8px 12px; border-radius: 6px; margin-bottom: 18px; outline: none; font-family: inherit; font-size: 12.5px; }}
  .modal-buttons {{ display: flex; justify-content: flex-end; gap: 8px; }}

  @media (prefers-reduced-motion: reduce) {{
    *, *::before, *::after {{
      animation-duration: 0.001ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.001ms !important;
      transform: none !important;
    }}
  }}

</style>
</head>
<body>
<div class='container'>
  <div class='header'>
    <div class='title-area'>
      <div class='header-icon'>&#xE81C;</div>
      <h1>Browsing History</h1>
    </div>
    <div class='header-actions'>
      <button class='btn-secondary' onclick=""exportHistory()""><span style=""font-family: 'Segoe Fluent Icons';"">&#xE749;</span> Export JSON</button>
      <button class='btn-danger' onclick=""openClearModal()""><span style=""font-family: 'Segoe Fluent Icons';"">&#xE74D;</span> Clear Browsing Data...</button>
    </div>
  </div>

  <div class='tabs'>
    <button class='tab-btn active' id='timelineTabBtn' onclick=""switchView('timeline')"">Timeline View</button>
    <button class='tab-btn' id='domainTabBtn' onclick=""switchView('domains')"">Group by Domain</button>
  </div>

  <div class='toolbar'>
    <input type='text' id='search' class='search-bar' placeholder='Search history by title, URL or domain...' oninput='filterHistory(this.value)'>
    <div class='batch-actions'>
      <label style='display:flex; align-items:center; gap:6px; font-size:12.5px; color:#94A3B8; cursor:pointer;'>
        <input type='checkbox' id='selectAllCb' onchange='toggleSelectAll(this.checked)'> Select All
      </label>
      <button id='batchDeleteBtn' class='btn-danger' style='display:none;' onclick='deleteSelected()'>Delete Selected (0)</button>
    </div>
  </div>

  <div id='timelineView'>
    {timelineHtml}
  </div>

  <div id='domainView' style='display:none;'>
    {domainSb}
  </div>
</div>

<div class='modal' id='clearModal'>
  <div class='modal-content'>
    <div class='modal-title'>Clear Browsing Data</div>
    <p style='font-size:12.5px; color:#94A3B8; margin-bottom:14px;'>Select the timeframe of browsing history to clear:</p>
    <select id='timeframeSelect' class='timeframe-select'>
      <option value='hour'>Last hour</option>
      <option value='day'>Last 24 hours</option>
      <option value='week'>Last 7 days</option>
      <option value='all' selected>All time</option>
    </select>
    <div class='modal-buttons'>
      <button class='btn-secondary' onclick=""closeClearModal()"">Cancel</button>
      <button class='btn-danger' onclick=""confirmClearHistory()"">Clear Data</button>
    </div>
  </div>
</div>

<script>
  function switchView(view) {{
    document.getElementById('timelineView').style.display = (view === 'timeline') ? 'block' : 'none';
    document.getElementById('domainView').style.display = (view === 'domains') ? 'block' : 'none';
    document.getElementById('timelineTabBtn').classList.toggle('active', view === 'timeline');
    document.getElementById('domainTabBtn').classList.toggle('active', view === 'domains');
  }}

  function toggleDomainGroup(elem) {{
    var items = elem.nextElementSibling;
    var chevron = elem.querySelector('.chevron');
    if (items.style.display === 'none') {{
      items.style.display = 'flex';
      chevron.style.transform = 'rotate(180deg)';
    }} else {{
      items.style.display = 'none';
      chevron.style.transform = 'none';
    }}
  }}

  function filterHistory(query) {{
    var q = query.toLowerCase().trim();
    var cards = document.querySelectorAll('.history-card');
    cards.forEach(card => {{
      var title = (card.getAttribute('data-title') || '').toLowerCase();
      var url = (card.getAttribute('data-url') || '').toLowerCase();
      var domain = (card.getAttribute('data-domain') || '').toLowerCase();
      card.style.display = (title.includes(q) || url.includes(q) || domain.includes(q)) ? 'flex' : 'none';
    }});
  }}

  function updateSelection() {{
    var checked = document.querySelectorAll('.hist-check:checked');
    var btn = document.getElementById('batchDeleteBtn');
    if (checked.length > 0) {{
      btn.style.display = 'flex';
      btn.innerText = `Delete Selected (${{checked.length}})`;
    }} else {{
      btn.style.display = 'none';
    }}
  }}

  function toggleSelectAll(val) {{
    var cbs = document.querySelectorAll('.hist-check');
    cbs.forEach(cb => cb.checked = val);
    updateSelection();
  }}

  function deleteSelected() {{
    var checked = Array.from(document.querySelectorAll('.hist-check:checked')).map(cb => cb.getAttribute('data-url'));
    if (checked.length > 0) {{
      window.chrome.webview.postMessage(JSON.stringify({{ action: 'delete_history_batch', urls: checked }}));
      document.querySelectorAll('.hist-check:checked').forEach(cb => {{
        var card = cb.closest('.history-card');
        if (card) card.remove();
      }});
      updateSelection();
    }}
  }}

  function deleteItem(url) {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'delete_history_entry', url: url }}));
    document.querySelectorAll(`.history-card[data-url='${{url}}']`).forEach(c => c.remove());
    updateSelection();
  }}

  function deleteDomain(domain) {{
    if (confirm(`Delete all browsing history for ${{domain}}?`)) {{
      window.chrome.webview.postMessage(JSON.stringify({{ action: 'delete_domain_history', domain: domain }}));
      var card = document.querySelector(`.domain-card[data-domain='${{domain}}']`);
      if (card) card.remove();
      document.querySelectorAll(`.history-card[data-domain='${{domain}}']`).forEach(c => c.remove());
    }}
  }}

  function openClearModal() {{
    document.getElementById('clearModal').style.display = 'flex';
  }}

  function closeClearModal() {{
    document.getElementById('clearModal').style.display = 'none';
  }}

  function exportHistory() {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'export_history_json' }}));
  }}
  function confirmClearHistory() {{
    var tf = document.getElementById('timeframeSelect').value;
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'clear_history_timeframe', timeframe: tf }}));
    closeClearModal();
    location.reload();
  }}
</script>
</body>
</html>";
        }

        private static string GetEmptyHistoryHtml()
        {
            string accent = BrowserSettings.Current.AccentColorHex;
            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<title>History - Browy</title>
<style>

  :root {{
    --bg-main: #0b0c0e;
    --bg-card: #141619;
    --bg-card-hover: #181b20;
    --border-color: rgba(255, 255, 255, 0.07);
    --border-subtle: rgba(255, 255, 255, 0.05);
    --border-hover: rgba(255, 255, 255, 0.16);
    --text-primary: #f4f4f5;
    --text-secondary: #a1a1aa;
    --text-muted: #71717a;
    --accent: {accent};
    --accent-hover: {accent};
    --danger-subtle: rgba(239, 68, 68, 0.12);
    --danger-border: rgba(239, 68, 68, 0.28);
    --font-sans: 'Segoe UI Variable Text', 'Segoe UI Variable Display', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    --font-mono: 'Cascadia Code', Consolas, 'SF Mono', monospace;
  }}
  * {{ box-sizing: border-box; margin: 0; padding: 0; user-select: none; -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale; }}

  body {{ background-color: var(--bg-main); color: var(--text-primary); font-family: var(--font-sans); min-height: 100vh; padding: 60px 24px; display: flex; justify-content: center; }}
  .empty-state {{ text-align: center; color: var(--text-muted); margin-top: 40px; }}
  .empty-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 44px; margin-bottom: 14px; color: var(--accent); opacity: 0.8; }}
  .empty-title {{ font-size: 18px; font-weight: 600; color: var(--text-primary); margin-bottom: 6px; letter-spacing: -0.01em; }}
  .empty-sub {{ font-size: 13px; color: var(--text-secondary); }}

  @media (prefers-reduced-motion: reduce) {{
    *, *::before, *::after {{
      animation-duration: 0.001ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.001ms !important;
      transform: none !important;
    }}
  }}

</style>
</head>
<body>
  <div class='empty-state'>
    <div class='empty-icon'>&#xE81C;</div>
    <div class='empty-title'>No browsing history yet</div>
    <div class='empty-sub'>Pages you visit will show up here.</div>
  </div>
</body>
</html>";
        }

        public static string GetBookmarksHtml(IEnumerable<BookmarkItem> bookmarks)
        {
            string accent = BrowserSettings.Current.AccentColorHex;
            var items = bookmarks.ToList();
            var itemsHtml = string.Empty;

            if (items.Count == 0)
            {
                itemsHtml = @"<div class='empty-state'>
                    <div class='empty-icon'>&#xE734;</div>
                    <div class='empty-title'>No bookmarks saved yet</div>
                    <div class='empty-sub'>Bookmark pages using Ctrl+D or the star icon in the address bar.</div>
                </div>";
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    string domain = NavigationHelper.ExtractDomain(item.Url);
                    string title = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(item.Title) ? domain : item.Title);
                    string url = WebUtility.HtmlEncode(item.Url);

                    sb.Append($@"
                    <div class='bookmark-card' data-url='{url}' data-title='{title}' data-index='{i}'>
                        <div class='card-icon'>&#xE734;</div>
                        <div class='card-info' onclick=""window.chrome.webview.postMessage(JSON.stringify({{action: 'navigate', url: '{url}'}}))"">
                            <div class='card-title'>{title}</div>
                            <div class='card-url'>{url}</div>
                        </div>
                        <button class='card-delete' onclick=""deleteBookmark('{url}', {i})"" title='Delete Bookmark'>&#xE74D;</button>
                    </div>");
                }
                itemsHtml = sb.ToString();
            }

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<title>Bookmarks - Browy</title>
<style>

  :root {{
    --bg-main: #0b0c0e;
    --bg-card: #141619;
    --bg-card-hover: #181b20;
    --border-color: rgba(255, 255, 255, 0.07);
    --border-subtle: rgba(255, 255, 255, 0.05);
    --border-hover: rgba(255, 255, 255, 0.16);
    --text-primary: #f4f4f5;
    --text-secondary: #a1a1aa;
    --text-muted: #71717a;
    --accent: {accent};
    --accent-hover: {accent};
    --danger-subtle: rgba(239, 68, 68, 0.12);
    --danger-border: rgba(239, 68, 68, 0.28);
    --font-sans: 'Segoe UI Variable Text', 'Segoe UI Variable Display', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    --font-mono: 'Cascadia Code', Consolas, 'SF Mono', monospace;
  }}
  * {{ box-sizing: border-box; margin: 0; padding: 0; user-select: none; -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale; }}

  body {{ background-color: var(--bg-main); color: var(--text-primary); font-family: var(--font-sans); min-height: 100vh; padding: 36px 24px; display: flex; justify-content: center; }}
  .container {{ width: 100%; max-width: 860px; }}
  .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }}
  .title-area {{ display: flex; align-items: center; gap: 14px; }}
  .header-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 26px; color: var(--accent); }}
  h1 {{ font-size: 22px; font-weight: 600; letter-spacing: -0.02em; color: var(--text-primary); }}
  .header-actions {{ display: flex; align-items: center; gap: 8px; }}

  .btn-primary {{
    background: #f4f4f5;
    color: #0b0c0e;
    border: 1px solid transparent;
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, color, transform, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-primary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-primary:hover {{ background: #ffffff; box-shadow: 0 2px 8px rgba(0,0,0,0.3); }}
  }}

  .btn-secondary {{
    background: transparent;
    color: var(--text-primary);
    border: 1px solid var(--border-color);
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-secondary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-secondary:hover {{ background: rgba(255, 255, 255, 0.05); border-color: var(--border-hover); }}
  }}

  .btn-danger {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 7px 14px;
    border-radius: 6px;
    cursor: pointer;
    font-size: 12.5px;
    font-weight: 500;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .btn-danger-sm {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 3px 9px;
    border-radius: 5px;
    cursor: pointer;
    font-size: 11.5px;
    font-weight: 500;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger-sm:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger-sm:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .search-bar {{
    width: 100%;
    background: #101215;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 8px 14px;
    color: var(--text-primary);
    font-size: 13px;
    margin-bottom: 20px;
    outline: none;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: border-color, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .search-bar:focus {{ border-color: rgba(255, 255, 255, 0.28); box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.15); }}

  .bookmark-list {{ display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 10px; }}
  .bookmark-card {{
    display: flex;
    align-items: center;
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 12px 14px;
    gap: 12px;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.02);
    transition-property: background-color, border-color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  @media (hover: hover) {{
    .bookmark-card:hover {{ background: var(--bg-card-hover); border-color: var(--border-hover); }}
  }}
  .card-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 16px; color: var(--accent); flex-shrink: 0; }}
  .card-info {{ flex: 1; min-width: 0; cursor: pointer; }}
  .card-title {{ font-size: 13px; font-weight: 500; color: var(--text-primary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }}
  .card-url {{ font-size: 11.5px; font-family: var(--font-mono); color: var(--text-muted); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-top: 2px; }}
  .card-delete {{
    background: transparent;
    border: none;
    font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets';
    font-size: 13px;
    color: var(--text-muted);
    width: 26px;
    height: 26px;
    border-radius: 5px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, color;
    transition-duration: 120ms;
  }}
  @media (hover: hover) {{
    .card-delete:hover {{ background: var(--danger-subtle); color: #fca5a5; }}
  }}

  .empty-state {{ text-align: center; padding: 64px 0; color: var(--text-muted); grid-column: 1 / -1; }}
  .empty-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 44px; margin-bottom: 12px; color: var(--accent); opacity: 0.8; }}
  .empty-title {{ font-size: 17px; font-weight: 600; color: var(--text-primary); margin-bottom: 4px; }}
  .empty-sub {{ font-size: 12.5px; color: var(--text-secondary); }}

  @media (prefers-reduced-motion: reduce) {{
    *, *::before, *::after {{
      animation-duration: 0.001ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.001ms !important;
      transform: none !important;
    }}
  }}

</style>
</head>
<body>
<div class='container'>
  <div class='header'>
    <div class='title-area'>
      <div class='header-icon'>&#xE734;</div>
      <h1>Bookmarks</h1>
    </div>
    <div class='header-actions'>
      <button class='btn-secondary' onclick=""exportBookmarks()""><span style=""font-family: 'Segoe Fluent Icons';"">&#xE749;</span> Export HTML</button>
      <button class='btn-secondary' onclick=""importBookmarks()""><span style=""font-family: 'Segoe Fluent Icons';"">&#xE8E5;</span> Import HTML</button>
    </div>
  </div>
  <input type='text' class='search-bar' placeholder='Search bookmarks by title or URL...' oninput='filterBookmarks(this.value)'>
  <div class='bookmark-list' id='bookmarkList'>
    {itemsHtml}
  </div>
</div>
<script>
  function filterBookmarks(query) {{
    var q = query.toLowerCase().trim();
    var cards = document.querySelectorAll('.bookmark-card');
    cards.forEach(card => {{
      var title = (card.getAttribute('data-title') || '').toLowerCase();
      var url = (card.getAttribute('data-url') || '').toLowerCase();
      card.style.display = (title.includes(q) || url.includes(q)) ? 'flex' : 'none';
    }});
  }}
  function deleteBookmark(url, index) {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'delete_bookmark', url: url, index: index }}));
    var card = document.querySelector(`.bookmark-card[data-index='${{index}}']`);
    if (card) card.remove();
  }}
  function exportBookmarks() {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'export_bookmarks_html' }}));
  }}
  function importBookmarks() {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'import_bookmarks_html' }}));
  }}
</script>
</body>
</html>";
        }

        public static string GetDownloadsHtml(IEnumerable<DownloadItem> downloads)
        {
            string accent = BrowserSettings.Current.AccentColorHex;
            var items = downloads.ToList();
            var itemsHtml = string.Empty;

            if (items.Count == 0)
            {
                itemsHtml = @"<div class='empty-state'>
                    <div class='empty-icon'>&#xE896;</div>
                    <div class='empty-title'>No downloads yet</div>
                    <div class='empty-sub'>Files you download will appear here.</div>
                </div>";
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    string fileName = WebUtility.HtmlEncode(item.FileName);
                    string status = WebUtility.HtmlEncode(item.StatusText);
                    string fullPath = WebUtility.HtmlEncode(item.ResultFilePath ?? "");
                    string id = WebUtility.HtmlEncode(item.Id);
                    double percent = item.PercentComplete;

                    string details = status;
                    if (!string.IsNullOrEmpty(item.SpeedText))
                    {
                        details += $" · {item.SpeedText}";
                    }
                    if (!string.IsNullOrEmpty(item.EtaText))
                    {
                        details += $" · {item.EtaText}";
                    }
                    string detailsHtml = WebUtility.HtmlEncode(details);

                    string controls = "";
                    if (item.IsInProgress)
                    {
                        controls = $@"<button class='btn-action' onclick=""pauseDownload('{id}')"">Pause</button>
                                      <button class='btn-action' onclick=""cancelDownload('{id}')"">Cancel</button>";
                    }
                    else if (item.IsPaused)
                    {
                        controls = $@"<button class='btn-action btn-primary' onclick=""resumeDownload('{id}')"">Resume</button>
                                      <button class='btn-action' onclick=""cancelDownload('{id}')"">Cancel</button>";
                    }
                    else if (item.IsCompleted)
                    {
                        controls = $@"<button class='btn-action' onclick=""openFile('{fullPath}')"">Open</button>
                                      <button class='btn-action' onclick=""showInFolder('{fullPath}')"">Show in folder</button>";
                    }

                    string statusCategory = item.IsInProgress ? "in_progress" : (item.IsPaused ? "paused" : (item.IsCompleted ? "completed" : "cancelled"));

                    sb.Append($@"
                    <div class='download-card' data-id='{id}' data-filename='{fileName}' data-status='{statusCategory}'>
                        <div class='card-icon'>{item.IconGlyph}</div>
                        <div class='card-info'>
                            <div class='card-title'>{fileName}</div>
                            <div class='card-status'>{detailsHtml}</div>
                            <div class='progress-bar-container'>
                                <div class='progress-bar-fill' style='width: {percent:0}%;'></div>
                            </div>
                        </div>
                        <div class='card-controls'>
                            {controls}
                        </div>
                    </div>");
                }
                itemsHtml = sb.ToString();
            }

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<title>Downloads - Browy</title>
<style>

  :root {{
    --bg-main: #0b0c0e;
    --bg-card: #141619;
    --bg-card-hover: #181b20;
    --border-color: rgba(255, 255, 255, 0.07);
    --border-subtle: rgba(255, 255, 255, 0.05);
    --border-hover: rgba(255, 255, 255, 0.16);
    --text-primary: #f4f4f5;
    --text-secondary: #a1a1aa;
    --text-muted: #71717a;
    --accent: {accent};
    --accent-hover: {accent};
    --danger-subtle: rgba(239, 68, 68, 0.12);
    --danger-border: rgba(239, 68, 68, 0.28);
    --font-sans: 'Segoe UI Variable Text', 'Segoe UI Variable Display', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    --font-mono: 'Cascadia Code', Consolas, 'SF Mono', monospace;
  }}
  * {{ box-sizing: border-box; margin: 0; padding: 0; user-select: none; -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale; }}

  body {{ background-color: var(--bg-main); color: var(--text-primary); font-family: var(--font-sans); min-height: 100vh; padding: 36px 24px; display: flex; justify-content: center; }}
  .container {{ width: 100%; max-width: 860px; }}
  .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }}
  .title-area {{ display: flex; align-items: center; gap: 14px; }}
  .header-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 26px; color: var(--accent); }}
  h1 {{ font-size: 22px; font-weight: 600; letter-spacing: -0.02em; color: var(--text-primary); }}
  .header-actions {{ display: flex; align-items: center; gap: 8px; }}

  .btn-primary {{
    background: #f4f4f5;
    color: #0b0c0e;
    border: 1px solid transparent;
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, color, transform, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-primary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-primary:hover {{ background: #ffffff; box-shadow: 0 2px 8px rgba(0,0,0,0.3); }}
  }}

  .btn-secondary {{
    background: transparent;
    color: var(--text-primary);
    border: 1px solid var(--border-color);
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-secondary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-secondary:hover {{ background: rgba(255, 255, 255, 0.05); border-color: var(--border-hover); }}
  }}

  .btn-danger {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 7px 14px;
    border-radius: 6px;
    cursor: pointer;
    font-size: 12.5px;
    font-weight: 500;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .btn-danger-sm {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 3px 9px;
    border-radius: 5px;
    cursor: pointer;
    font-size: 11.5px;
    font-weight: 500;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger-sm:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger-sm:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .search-bar {{
    width: 100%;
    background: #101215;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 8px 14px;
    color: var(--text-primary);
    font-size: 13px;
    margin-bottom: 16px;
    outline: none;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: border-color, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .search-bar:focus {{ border-color: rgba(255, 255, 255, 0.28); box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.15); }}

  .filter-bar {{ display: flex; gap: 6px; margin-bottom: 16px; }}
  .filter-btn {{
    background: transparent;
    border: 1px solid var(--border-color);
    color: var(--text-secondary);
    padding: 5px 12px;
    border-radius: 6px;
    font-size: 12px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .filter-btn:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .filter-btn:hover {{ background: rgba(255, 255, 255, 0.04); color: var(--text-primary); }}
  }}
  .filter-btn.active {{ background: rgba(255, 255, 255, 0.08); color: var(--text-primary); border-color: rgba(255, 255, 255, 0.2); font-weight: 600; }}

  .download-list {{ display: flex; flex-direction: column; gap: 8px; }}
  .download-card {{
    display: flex;
    align-items: center;
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 14px 18px;
    gap: 16px;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.02);
    transition-property: background-color, border-color;
    transition-duration: 140ms;
  }}
  @media (hover: hover) {{
    .download-card:hover {{ background: var(--bg-card-hover); border-color: var(--border-hover); }}
  }}
  .card-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 22px; color: var(--accent); flex-shrink: 0; }}
  .card-info {{ flex: 1; min-width: 0; }}
  .card-title {{ font-size: 13.5px; font-weight: 600; color: var(--text-primary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }}
  .card-status {{ font-size: 12px; color: var(--text-secondary); margin-top: 3px; font-family: var(--font-mono); }}
  .progress-bar-container {{ height: 3px; background: rgba(255, 255, 255, 0.06); border-radius: 2px; margin-top: 8px; overflow: hidden; }}
  .progress-bar-fill {{ height: 100%; background: var(--accent); border-radius: 2px; transition: width 150ms ease; }}
  .card-controls {{ display: flex; align-items: center; gap: 6px; }}
  .btn-action {{
    background: rgba(255, 255, 255, 0.05);
    color: var(--text-primary);
    border: 1px solid var(--border-color);
    padding: 5px 12px;
    border-radius: 5px;
    cursor: pointer;
    font-size: 11.5px;
    font-weight: 500;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-action:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-action:hover {{ background: rgba(255, 255, 255, 0.08); border-color: var(--border-hover); }}
  }}
  .btn-action.btn-primary {{ background: #f4f4f5; color: #0b0c0e; border-color: transparent; }}
  @media (hover: hover) {{
    .btn-action.btn-primary:hover {{ background: #ffffff; }}
  }}

  .empty-state {{ text-align: center; padding: 64px 0; color: var(--text-muted); }}
  .empty-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 44px; margin-bottom: 12px; color: var(--accent); opacity: 0.8; }}
  .empty-title {{ font-size: 17px; font-weight: 600; color: var(--text-primary); margin-bottom: 4px; }}
  .empty-sub {{ font-size: 12.5px; color: var(--text-secondary); }}

  @media (prefers-reduced-motion: reduce) {{
    *, *::before, *::after {{
      animation-duration: 0.001ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.001ms !important;
      transform: none !important;
    }}
  }}

</style>
</head>
<body>
<div class='container'>
  <div class='header'>
    <div class='title-area'>
      <div class='header-icon'>&#xE896;</div>
      <h1>Downloads</h1>
    </div>
    <div class='header-actions'>
      <button class='btn-secondary' onclick=""openDownloadsFolder()"">Open Folder</button>
      <button class='btn-secondary' onclick=""clearCompleted()"">Clear Completed</button>
    </div>
  </div>
  <input type='text' id='downloadSearch' class='search-bar' placeholder='Search downloads by file name...' oninput='applyFilters()'>
  <div class='filter-bar'>
    <button class='filter-btn active' onclick=""setStatusFilter('all', this)"">All</button>
    <button class='filter-btn' onclick=""setStatusFilter('in_progress', this)"">In Progress</button>
    <button class='filter-btn' onclick=""setStatusFilter('completed', this)"">Completed</button>
    <button class='filter-btn' onclick=""setStatusFilter('paused', this)"">Paused / Other</button>
  </div>
  <div class='download-list' id='downloadList'>
    {itemsHtml}
  </div>
</div>
<script>
  var currentStatusFilter = 'all';
  function setStatusFilter(status, btn) {{
    currentStatusFilter = status;
    document.querySelectorAll('.filter-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    applyFilters();
  }}
  function applyFilters() {{
    var q = (document.getElementById('downloadSearch').value || '').toLowerCase().trim();
    var cards = document.querySelectorAll('.download-card');
    cards.forEach(card => {{
      var name = (card.getAttribute('data-filename') || '').toLowerCase();
      var status = card.getAttribute('data-status') || '';
      var matchesQuery = name.includes(q);
      var matchesStatus = (currentStatusFilter === 'all') || (status === currentStatusFilter);
      card.style.display = (matchesQuery && matchesStatus) ? 'flex' : 'none';
    }});
  }}
  function openFile(path) {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'open_download_file', path: path }}));
  }}
  function showInFolder(path) {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'show_download_in_folder', path: path }}));
  }}
  function pauseDownload(id) {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'pause_download', id: id }}));
  }}
  function resumeDownload(id) {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'resume_download', id: id }}));
  }}
  function cancelDownload(id) {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'cancel_download', id: id }}));
  }}
  function openDownloadsFolder() {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'open_downloads_folder' }}));
  }}
  function clearCompleted() {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'clear_completed_downloads' }}));
    location.reload();
  }}
</script>
</body>
</html>";
        }

        public static string GetExtensionsHtml(IEnumerable<ExtensionItem> extensions)
        {
            string accent = BrowserSettings.Current.AccentColorHex;
            var items = extensions.ToList();
            var itemsHtml = string.Empty;

            if (items.Count == 0)
            {
                itemsHtml = @"<div class='empty-state'>
                    <div class='empty-icon'>&#xE74C;</div>
                    <div class='empty-title'>No extensions installed</div>
                    <div class='empty-sub'>Load an unpacked Chrome or Edge extension to expand Browy's capabilities.</div>
                </div>";
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                foreach (var ext in items)
                {
                    string name = WebUtility.HtmlEncode(ext.Name);
                    string version = WebUtility.HtmlEncode(ext.Version);
                    string desc = WebUtility.HtmlEncode(ext.Description);
                    string id = WebUtility.HtmlEncode(ext.Id);
                    string path = WebUtility.HtmlEncode(ext.FolderPath);
                    string checkedAttr = ext.IsEnabled ? "checked" : "";

                    sb.Append($@"
                    <div class='ext-card' data-id='{id}'>
                        <div class='ext-icon'>&#xE74C;</div>
                        <div class='ext-info'>
                            <div class='ext-header'>
                                <span class='ext-name'>{name}</span>
                                <span class='ext-version'>v{version}</span>
                            </div>
                            <div class='ext-desc'>{(!string.IsNullOrEmpty(desc) ? desc : "No description provided.")}</div>
                            <div class='ext-path' title='{path}'>{path}</div>
                        </div>
                        <div class='ext-actions'>
                            <label class='switch'>
                                <input type='checkbox' {checkedAttr} onchange=""toggleExtension('{id}', this.checked)"">
                                <span class='slider'></span>
                            </label>
                            <button class='btn-remove' onclick=""removeExtension('{id}')"" title='Remove Extension'>&#xE74D;</button>
                        </div>
                    </div>");
                }
                itemsHtml = sb.ToString();
            }

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<title>Extensions - Browy</title>
<style>

  :root {{
    --bg-main: #0b0c0e;
    --bg-card: #141619;
    --bg-card-hover: #181b20;
    --border-color: rgba(255, 255, 255, 0.07);
    --border-subtle: rgba(255, 255, 255, 0.05);
    --border-hover: rgba(255, 255, 255, 0.16);
    --text-primary: #f4f4f5;
    --text-secondary: #a1a1aa;
    --text-muted: #71717a;
    --accent: {accent};
    --accent-hover: {accent};
    --danger-subtle: rgba(239, 68, 68, 0.12);
    --danger-border: rgba(239, 68, 68, 0.28);
    --font-sans: 'Segoe UI Variable Text', 'Segoe UI Variable Display', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    --font-mono: 'Cascadia Code', Consolas, 'SF Mono', monospace;
  }}
  * {{ box-sizing: border-box; margin: 0; padding: 0; user-select: none; -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale; }}

  body {{ background-color: var(--bg-main); color: var(--text-primary); font-family: var(--font-sans); min-height: 100vh; padding: 36px 24px; display: flex; justify-content: center; }}
  .container {{ width: 100%; max-width: 860px; }}
  .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }}
  .title-area {{ display: flex; align-items: center; gap: 14px; }}
  .header-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 26px; color: var(--accent); }}
  h1 {{ font-size: 22px; font-weight: 600; letter-spacing: -0.02em; color: var(--text-primary); }}

  .btn-primary {{
    background: #f4f4f5;
    color: #0b0c0e;
    border: 1px solid transparent;
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, color, transform, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-primary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-primary:hover {{ background: #ffffff; box-shadow: 0 2px 8px rgba(0,0,0,0.3); }}
  }}

  .btn-secondary {{
    background: transparent;
    color: var(--text-primary);
    border: 1px solid var(--border-color);
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-secondary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-secondary:hover {{ background: rgba(255, 255, 255, 0.05); border-color: var(--border-hover); }}
  }}

  .btn-danger {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 7px 14px;
    border-radius: 6px;
    cursor: pointer;
    font-size: 12.5px;
    font-weight: 500;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .btn-danger-sm {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 3px 9px;
    border-radius: 5px;
    cursor: pointer;
    font-size: 11.5px;
    font-weight: 500;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger-sm:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger-sm:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .ext-list {{ display: flex; flex-direction: column; gap: 10px; }}
  .ext-card {{
    display: flex;
    align-items: flex-start;
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 16px 18px;
    gap: 16px;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.02);
    transition-property: background-color, border-color;
    transition-duration: 140ms;
  }}
  @media (hover: hover) {{
    .ext-card:hover {{ background: var(--bg-card-hover); border-color: var(--border-hover); }}
  }}
  .ext-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 22px; color: var(--accent); padding-top: 2px; flex-shrink: 0; }}
  .ext-info {{ flex: 1; min-width: 0; }}
  .ext-header {{ display: flex; align-items: center; gap: 8px; margin-bottom: 4px; }}
  .ext-name {{ font-size: 14px; font-weight: 600; color: var(--text-primary); }}
  .ext-version {{ font-size: 11px; font-family: var(--font-mono); background: rgba(255, 255, 255, 0.06); border: 1px solid var(--border-color); color: var(--text-secondary); padding: 1px 6px; border-radius: 4px; }}
  .ext-desc {{ font-size: 12.5px; color: var(--text-secondary); line-height: 1.45; margin-bottom: 6px; }}
  .ext-path {{ font-size: 11px; color: var(--text-muted); font-family: var(--font-mono); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }}
  .ext-actions {{ display: flex; align-items: center; gap: 12px; margin-left: 8px; }}

  .btn-remove {{
    background: transparent;
    border: none;
    font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets';
    font-size: 14px;
    color: var(--text-muted);
    width: 28px;
    height: 28px;
    border-radius: 5px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, color;
    transition-duration: 120ms;
  }}
  @media (hover: hover) {{
    .btn-remove:hover {{ background: var(--danger-subtle); color: #fca5a5; }}
  }}

  .switch {{ position: relative; display: inline-block; width: 36px; height: 20px; }}
  .switch input {{ opacity: 0; width: 0; height: 0; }}
  .slider {{ position: absolute; cursor: pointer; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(255, 255, 255, 0.12); border-radius: 20px; transition-property: background-color; transition-duration: 140ms; }}
  .slider:before {{ position: absolute; content: ''; height: 14px; width: 14px; left: 3px; bottom: 3px; background-color: #f4f4f5; border-radius: 50%; transition-property: transform; transition-duration: 140ms; }}
  input:checked + .slider {{ background-color: var(--accent); }}
  input:checked + .slider:before {{ transform: translateX(16px); }}

  .empty-state {{ text-align: center; padding: 64px 0; color: var(--text-muted); }}
  .empty-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 44px; margin-bottom: 12px; color: var(--accent); opacity: 0.8; }}
  .empty-title {{ font-size: 17px; font-weight: 600; color: var(--text-primary); margin-bottom: 4px; }}
  .empty-sub {{ font-size: 12.5px; color: var(--text-secondary); }}

  @media (prefers-reduced-motion: reduce) {{
    *, *::before, *::after {{
      animation-duration: 0.001ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.001ms !important;
      transform: none !important;
    }}
  }}

</style>
</head>
<body>
<div class='container'>
  <div class='header'>
    <div class='title-area'>
      <div class='header-icon'>&#xE74C;</div>
      <h1>Extensions</h1>
    </div>
    <button class='btn-primary' onclick=""loadUnpackedExtension()""><span style=""font-family: 'Segoe Fluent Icons';"">&#xE710;</span> Load Unpacked Extension</button>
  </div>
  <div class='ext-list' id='extList'>
    {itemsHtml}
  </div>
</div>
<script>
  function loadUnpackedExtension() {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'load_unpacked_extension' }}));
  }}
  function toggleExtension(id, enabled) {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'toggle_extension', id: id, enabled: enabled }}));
  }}
  function removeExtension(id) {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'remove_extension', id: id }}));
    var card = document.querySelector(`.ext-card[data-id='${{id}}']`);
    if (card) card.remove();
  }}
</script>
</body>
</html>";
        }

        public static string GetAboutHtml(int tabsCount = 1, int spacesCount = 1)
        {
            var settings = BrowserSettings.Current;
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.3.0";
            string osDesc = RuntimeInformation.OSDescription;
            string frameworkDesc = RuntimeInformation.FrameworkDescription;
            string arch = RuntimeInformation.ProcessArchitecture.ToString();
            string machine = Environment.MachineName;

            long workingSetMb = 0;
            try { workingSetMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024); } catch { }
            long gcHeapMb = 0;
            try { gcHeapMb = GC.GetTotalMemory(false) / (1024 * 1024); } catch { }

            string accent = settings.AccentColorHex;
            string backdrop = settings.BackdropMaterial;
            string shieldStatus = settings.IsShieldEnabled ? "Enabled (Proactive Ad & Tracker Interception)" : "Disabled";

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<title>About Browy - System Diagnostics</title>
<style>

  :root {{
    --bg-main: #0b0c0e;
    --bg-card: #141619;
    --bg-card-hover: #181b20;
    --border-color: rgba(255, 255, 255, 0.07);
    --border-subtle: rgba(255, 255, 255, 0.05);
    --border-hover: rgba(255, 255, 255, 0.16);
    --text-primary: #f4f4f5;
    --text-secondary: #a1a1aa;
    --text-muted: #71717a;
    --accent: {accent};
    --accent-hover: {accent};
    --danger-subtle: rgba(239, 68, 68, 0.12);
    --danger-border: rgba(239, 68, 68, 0.28);
    --font-sans: 'Segoe UI Variable Text', 'Segoe UI Variable Display', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    --font-mono: 'Cascadia Code', Consolas, 'SF Mono', monospace;
  }}
  * {{ box-sizing: border-box; margin: 0; padding: 0; user-select: none; -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale; }}

  body {{ background-color: var(--bg-main); color: var(--text-primary); font-family: var(--font-sans); min-height: 100vh; padding: 36px 24px; display: flex; justify-content: center; }}
  .container {{ width: 100%; max-width: 860px; }}
  .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }}
  .title-area {{ display: flex; align-items: center; gap: 14px; }}
  .header-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 28px; color: var(--accent); }}
  h1 {{ font-size: 22px; font-weight: 600; letter-spacing: -0.02em; color: var(--text-primary); }}
  .header-badge {{ font-size: 11px; font-family: var(--font-mono); background: rgba(255, 255, 255, 0.06); color: var(--text-primary); border: 1px solid var(--border-color); padding: 2px 7px; border-radius: 4px; font-weight: 500; margin-left: 8px; vertical-align: middle; }}
  .header-actions {{ display: flex; align-items: center; gap: 8px; }}

  .btn-primary {{
    background: #f4f4f5;
    color: #0b0c0e;
    border: 1px solid transparent;
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, color, transform, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-primary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-primary:hover {{ background: #ffffff; box-shadow: 0 2px 8px rgba(0,0,0,0.3); }}
  }}

  .btn-secondary {{
    background: transparent;
    color: var(--text-primary);
    border: 1px solid var(--border-color);
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-secondary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-secondary:hover {{ background: rgba(255, 255, 255, 0.05); border-color: var(--border-hover); }}
  }}

  .btn-danger {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 7px 14px;
    border-radius: 6px;
    cursor: pointer;
    font-size: 12.5px;
    font-weight: 500;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .btn-danger-sm {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 3px 9px;
    border-radius: 5px;
    cursor: pointer;
    font-size: 11.5px;
    font-weight: 500;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger-sm:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger-sm:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(380px, 1fr)); gap: 14px; margin-bottom: 24px; }}
  .card {{ background: var(--bg-card); border: 1px solid var(--border-color); border-radius: 8px; padding: 18px 20px; box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.02); }}
  .card-header {{ display: flex; align-items: center; gap: 10px; margin-bottom: 12px; }}
  .card-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 16px; color: var(--accent); }}
  .card-title {{ font-size: 14px; font-weight: 600; color: var(--text-primary); }}
  .info-table {{ width: 100%; border-collapse: collapse; }}
  .info-table tr {{ border-bottom: 1px solid var(--border-subtle); }}
  .info-table tr:last-child {{ border-bottom: none; }}
  .info-table td {{ padding: 7px 0; font-size: 12.5px; }}
  .info-table td.label {{ color: var(--text-secondary); width: 45%; }}
  .info-table td.value {{ color: var(--text-primary); font-weight: 500; word-break: break-all; }}
  .value-code {{ font-family: var(--font-mono); font-size: 11.5px; background: rgba(255, 255, 255, 0.05); border: 1px solid var(--border-color); padding: 2px 6px; border-radius: 4px; color: var(--text-primary); }}
  .toast {{ position: fixed; bottom: 24px; right: 24px; background: #141619; border: 1px solid rgba(255, 255, 255, 0.12); color: #f4f4f5; padding: 10px 16px; border-radius: 6px; font-size: 12.5px; display: none; box-shadow: 0 12px 32px rgba(0,0,0,0.6); z-index: 1000; }}

  @media (prefers-reduced-motion: reduce) {{
    *, *::before, *::after {{
      animation-duration: 0.001ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.001ms !important;
      transform: none !important;
    }}
  }}

</style>
</head>
<body>
<div class='container'>
  <div class='header'>
    <div class='title-area'>
      <div class='header-icon'>&#xE774;</div>
      <div>
        <h1>Browy <span class='header-badge'>v{version}</span></h1>
        <div style='font-size:12.5px; color:#94A3B8; margin-top:2px;'>Modern Arc/Zen-Inspired Windows 11 Fluent Browser</div>
      </div>
    </div>
    <div class='header-actions'>
      <button class='btn-primary' onclick=""copyDiagnostics()""><span style=""font-family: 'Segoe Fluent Icons';"">&#xE8C8;</span> Copy Info</button>
      <button class='btn-secondary' onclick=""openSettings()""><span style=""font-family: 'Segoe Fluent Icons';"">&#xE713;</span> Settings</button>
    </div>
  </div>

  <div class='grid'>
    <div class='card'>
      <div class='card-header'>
        <div class='card-icon'>&#xE7F4;</div>
        <div class='card-title'>Operating System & Shell</div>
      </div>
      <table class='info-table'>
        <tr><td class='label'>OS Version</td><td class='value'>{WebUtility.HtmlEncode(osDesc)}</td></tr>
        <tr><td class='label'>Process Architecture</td><td class='value'><span class='value-code'>{arch}</span></td></tr>
        <tr><td class='label'>Computer Name</td><td class='value'>{WebUtility.HtmlEncode(machine)}</td></tr>
        <tr><td class='label'>Backdrop Material</td><td class='value'><span class='value-code'>{backdrop}</span></td></tr>
        <tr><td class='label'>Accent Color</td><td class='value'><span style='display:inline-block; width:12px; height:12px; border-radius:50%; background:{accent}; vertical-align:middle; margin-right:6px;'></span>{accent}</td></tr>
      </table>
    </div>

    <div class='card'>
      <div class='card-header'>
        <div class='card-icon'>&#xE950;</div>
        <div class='card-title'>Runtime & Web Engine</div>
      </div>
      <table class='info-table'>
        <tr><td class='label'>.NET Runtime</td><td class='value'>{WebUtility.HtmlEncode(frameworkDesc)}</td></tr>
        <tr><td class='label'>Web Engine</td><td class='value'>Microsoft Edge WebView2 Evergreen</td></tr>
        <tr><td class='label'>Browser Channel</td><td class='value'>Production Stable</td></tr>
        <tr><td class='label'>Browy Shield</td><td class='value'>{shieldStatus}</td></tr>
        <tr><td class='label'>Startup Mode</td><td class='value'>{settings.StartupBehavior}</td></tr>
      </table>
    </div>

    <div class='card'>
      <div class='card-header'>
        <div class='card-icon'>&#xE9D9;</div>
        <div class='card-title'>Memory & Performance</div>
      </div>
      <table class='info-table'>
        <tr><td class='label'>Process Working Set</td><td class='value'><span class='value-code'>{workingSetMb} MB</span></td></tr>
        <tr><td class='label'>GC Heap Memory</td><td class='value'><span class='value-code'>{gcHeapMb} MB</span></td></tr>
        <tr><td class='label'>Sleeping Tabs Feature</td><td class='value'>{(settings.EnableIdleTabSuspension ? $"Enabled ({settings.IdleTabSuspensionMinutes} min idle)" : "Disabled")}</td></tr>
        <tr><td class='label'>Mouse Gestures</td><td class='value'>{(settings.EnableMouseGestures ? "Enabled" : "Disabled")}</td></tr>
        <tr><td class='label'>Trackpad Swipes</td><td class='value'>{(settings.EnableTrackpadSwipe ? "Enabled" : "Disabled")}</td></tr>
      </table>
    </div>

    <div class='card'>
      <div class='card-header'>
        <div class='card-icon'>&#xE7C3;</div>
        <div class='card-title'>Session Workspace Statistics</div>
      </div>
      <table class='info-table'>
        <tr><td class='label'>Active Spaces Count</td><td class='value'><span class='value-code'>{spacesCount}</span></td></tr>
        <tr><td class='label'>Open Tabs Count</td><td class='value'><span class='value-code'>{tabsCount}</span></td></tr>
        <tr><td class='label'>Installed Extensions</td><td class='value'><span class='value-code'>{ExtensionService.Extensions.Count}</span></td></tr>
        <tr><td class='label'>Bookmarks Bar</td><td class='value'>{(settings.ShowBookmarksBar ? "Visible" : "Hidden")}</td></tr>
        <tr><td class='label'>AI Copilot Provider</td><td class='value'>{settings.AiProvider}</td></tr>
      </table>
    </div>
  </div>

  <div class='card' style='text-align:center; padding:24px;'>
    <div style='font-size:14px; font-weight:600; color:#F8FAFC; margin-bottom:6px;'>Browy Browser is fully up to date</div>
    <div style='font-size:12px; color:#94A3B8; margin-bottom:16px;'>Built with WPF and Edge WebView2 for modern, fluid Windows 11 browsing.</div>
    <div style='display:flex; justify-content:center; gap:12px;'>
      <button class='btn-secondary' onclick=""openUrl('https://github.com/billy-browy/browy')""><span style=""font-family: 'Segoe Fluent Icons';"">&#xE774;</span> GitHub Repository</button>
      <button class='btn-secondary' onclick=""openUrl('browy://shortcuts')""><span style=""font-family: 'Segoe Fluent Icons';"">&#xE765;</span> Shortcuts Reference</button>
    </div>
  </div>
</div>

<div class='toast' id='toast'>Diagnostics copied to clipboard!</div>

<script>
  function openUrl(url) {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'navigate', url: url }}));
  }}
  function openSettings() {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'navigate', url: 'browy://settings' }}));
  }}
  function copyDiagnostics() {{
    var text = `Browy Version: {version}\\nOS: {osDesc}\\nArch: {arch}\\n.NET: {frameworkDesc}\\nProcess RAM: {workingSetMb} MB\\nGC Memory: {gcHeapMb} MB\\nSpaces: {spacesCount}\\nTabs: {tabsCount}`;
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'copy_diagnostics', text: text }}));
    var toast = document.getElementById('toast');
    toast.style.display = 'block';
    setTimeout(() => {{ toast.style.display = 'none'; }}, 2500);
  }}
</script>
</body>
</html>";
        }

        public static string GetShortcutsHtml(Dictionary<string, string>? customShortcuts = null)
        {
            var settings = BrowserSettings.Current;
            string accent = settings.AccentColorHex;

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<title>Keyboard Shortcuts - Browy</title>
<style>

  :root {{
    --bg-main: #0b0c0e;
    --bg-card: #141619;
    --bg-card-hover: #181b20;
    --border-color: rgba(255, 255, 255, 0.07);
    --border-subtle: rgba(255, 255, 255, 0.05);
    --border-hover: rgba(255, 255, 255, 0.16);
    --text-primary: #f4f4f5;
    --text-secondary: #a1a1aa;
    --text-muted: #71717a;
    --accent: {accent};
    --accent-hover: {accent};
    --danger-subtle: rgba(239, 68, 68, 0.12);
    --danger-border: rgba(239, 68, 68, 0.28);
    --font-sans: 'Segoe UI Variable Text', 'Segoe UI Variable Display', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    --font-mono: 'Cascadia Code', Consolas, 'SF Mono', monospace;
  }}
  * {{ box-sizing: border-box; margin: 0; padding: 0; user-select: none; -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale; }}

  body {{ background-color: var(--bg-main); color: var(--text-primary); font-family: var(--font-sans); min-height: 100vh; padding: 36px 24px; display: flex; justify-content: center; }}
  .container {{ width: 100%; max-width: 860px; }}
  .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }}
  .title-area {{ display: flex; align-items: center; gap: 14px; }}
  .header-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 26px; color: var(--accent); }}
  h1 {{ font-size: 22px; font-weight: 600; letter-spacing: -0.02em; color: var(--text-primary); }}
  .header-actions {{ display: flex; align-items: center; gap: 8px; }}

  .btn-primary {{
    background: #f4f4f5;
    color: #0b0c0e;
    border: 1px solid transparent;
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, color, transform, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-primary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-primary:hover {{ background: #ffffff; box-shadow: 0 2px 8px rgba(0,0,0,0.3); }}
  }}

  .btn-secondary {{
    background: transparent;
    color: var(--text-primary);
    border: 1px solid var(--border-color);
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-secondary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-secondary:hover {{ background: rgba(255, 255, 255, 0.05); border-color: var(--border-hover); }}
  }}

  .btn-danger {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 7px 14px;
    border-radius: 6px;
    cursor: pointer;
    font-size: 12.5px;
    font-weight: 500;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .btn-danger-sm {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 3px 9px;
    border-radius: 5px;
    cursor: pointer;
    font-size: 11.5px;
    font-weight: 500;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger-sm:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger-sm:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .search-bar {{
    width: 100%;
    background: #101215;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 8px 14px;
    color: var(--text-primary);
    font-size: 13px;
    margin-bottom: 20px;
    outline: none;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: border-color, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .search-bar:focus {{ border-color: rgba(255, 255, 255, 0.28); box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.15); }}

  .section {{ margin-bottom: 24px; }}
  .section-title {{ font-size: 14px; font-weight: 600; color: var(--text-primary); margin-bottom: 10px; display: flex; align-items: center; gap: 8px; }}
  .section-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 15px; color: var(--accent); }}
  .shortcuts-grid {{ display: grid; grid-template-columns: repeat(auto-fill, minmax(360px, 1fr)); gap: 8px; }}
  .shortcut-card {{
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 10px 14px;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.02);
    transition-property: background-color, border-color;
    transition-duration: 120ms;
  }}
  @media (hover: hover) {{
    .shortcut-card:hover {{ background: var(--bg-card-hover); border-color: var(--border-hover); }}
  }}
  .shortcut-desc {{ font-size: 12.5px; font-weight: 500; color: var(--text-primary); }}
  .shortcut-keys {{ display: flex; gap: 4px; align-items: center; }}
  .key {{
    background: #101215;
    border: 1px solid var(--border-color);
    border-bottom: 2px solid rgba(255, 255, 255, 0.15);
    border-radius: 5px;
    padding: 2px 7px;
    font-size: 11px;
    font-weight: 600;
    color: var(--text-primary);
    font-family: var(--font-mono);
  }}
  .plus {{ color: var(--text-muted); font-size: 10px; }}

  @media (prefers-reduced-motion: reduce) {{
    *, *::before, *::after {{
      animation-duration: 0.001ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.001ms !important;
      transform: none !important;
    }}
  }}

</style>
</head>
<body>
<div class='container'>
  <div class='header'>
    <div class='title-area'>
      <div class='header-icon'>&#xE765;</div>
      <div>
        <h1>Keyboard Shortcuts</h1>
        <div style='font-size:12.5px; color:#94A3B8; margin-top:2px;'>Comprehensive command reference and mouse gestures</div>
      </div>
    </div>
    <div class='header-actions'>
      <button class='btn-secondary' onclick=""openCustomize()""><span style=""font-family: 'Segoe Fluent Icons';"">&#xE713;</span> Customize in Settings</button>
    </div>
  </div>

  <input type='text' id='scSearch' class='search-bar' placeholder='Search shortcuts by action name or key (e.g. Tab, Reload, Split)...' oninput='filterShortcuts(this.value)'>

  <div class='section'>
    <div class='section-title'><span class='section-icon'>&#xE7C3;</span> Tabs &amp; Windows</div>
    <div class='shortcuts-grid'>
      <div class='shortcut-card' data-search='new tab ctrl t'><div class='shortcut-desc'>New Tab</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>T</span></div></div>
      <div class='shortcut-card' data-search='close tab ctrl w'><div class='shortcut-desc'>Close Current Tab</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>W</span></div></div>
      <div class='shortcut-card' data-search='middle click close tab'><div class='shortcut-desc'>Close Tab (Quick Action)</div><div class='shortcut-keys'><span class='key'>Middle Click</span></div></div>
      <div class='shortcut-card' data-search='reopen closed tab ctrl shift t'><div class='shortcut-desc'>Reopen Closed Tab</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>Shift</span><span class='plus'>+</span><span class='key'>T</span></div></div>
      <div class='shortcut-card' data-search='next tab ctrl tab'><div class='shortcut-desc'>Next Tab</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>Tab</span></div></div>
      <div class='shortcut-card' data-search='previous tab ctrl shift tab'><div class='shortcut-desc'>Previous Tab</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>Shift</span><span class='plus'>+</span><span class='key'>Tab</span></div></div>
      <div class='shortcut-card' data-search='switch to tab 1-8 ctrl 1 8'><div class='shortcut-desc'>Switch to Tab 1 through 8</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>1..8</span></div></div>
      <div class='shortcut-card' data-search='switch to last tab ctrl 9'><div class='shortcut-desc'>Switch to Last Tab</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>9</span></div></div>
      <div class='shortcut-card' data-search='mute unmute tab ctrl m'><div class='shortcut-desc'>Mute / Unmute Tab Audio</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>M</span></div></div>
      <div class='shortcut-card' data-search='inprivate window ctrl shift n'><div class='shortcut-desc'>New InPrivate Window</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>Shift</span><span class='plus'>+</span><span class='key'>N</span></div></div>
    </div>
  </div>

  <div class='section'>
    <div class='section-title'><span class='section-icon'>&#xE72C;</span> Navigation &amp; Webpage</div>
    <div class='shortcuts-grid'>
      <div class='shortcut-card' data-search='focus address bar omnibox ctrl l'><div class='shortcut-desc'>Focus Address Bar</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>L</span></div></div>
      <div class='shortcut-card' data-search='reload page ctrl r f5'><div class='shortcut-desc'>Reload Page</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>R</span></div></div>
      <div class='shortcut-card' data-search='hard reload cache bypass ctrl f5'><div class='shortcut-desc'>Hard Reload (Bypass Cache)</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>F5</span></div></div>
      <div class='shortcut-card' data-search='navigate back alt left'><div class='shortcut-desc'>Go Back</div><div class='shortcut-keys'><span class='key'>Alt</span><span class='plus'>+</span><span class='key'>&#x2190;</span></div></div>
      <div class='shortcut-card' data-search='navigate forward alt right'><div class='shortcut-desc'>Go Forward</div><div class='shortcut-keys'><span class='key'>Alt</span><span class='plus'>+</span><span class='key'>&#x2192;</span></div></div>
      <div class='shortcut-card' data-search='find in page ctrl f'><div class='shortcut-desc'>Find in Page</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>F</span></div></div>
      <div class='shortcut-card' data-search='bookmark page ctrl d'><div class='shortcut-desc'>Bookmark Page</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>D</span></div></div>
      <div class='shortcut-card' data-search='print pdf ctrl p'><div class='shortcut-desc'>Print or Save as PDF</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>P</span></div></div>
      <div class='shortcut-card' data-search='view page source ctrl u'><div class='shortcut-desc'>View Page Source</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>U</span></div></div>
      <div class='shortcut-card' data-search='developer tools f12 ctrl shift i'><div class='shortcut-desc'>Developer Tools</div><div class='shortcut-keys'><span class='key'>F12</span></div></div>
    </div>
  </div>

  <div class='section'>
    <div class='section-title'><span class='section-icon'>&#xE7FB;</span> Views, Spaces &amp; Tools</div>
    <div class='shortcuts-grid'>
      <div class='shortcut-card' data-search='command palette ctrl k ctrl shift p'><div class='shortcut-desc'>Command Palette</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>K</span></div></div>
      <div class='shortcut-card' data-search='split view dual pane ctrl backslash'><div class='shortcut-desc'>Toggle Dual-Pane Split View</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>\</span></div></div>
      <div class='shortcut-card' data-search='collapse sidebar vertical tabs ctrl s'><div class='shortcut-desc'>Toggle Vertical Sidebar</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>S</span></div></div>
      <div class='shortcut-card' data-search='ai copilot panel ctrl j'><div class='shortcut-desc'>Toggle AI Copilot Panel</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>J</span></div></div>
      <div class='shortcut-card' data-search='bookmarks bar ctrl shift b'><div class='shortcut-desc'>Toggle Bookmarks Bar</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>Shift</span><span class='plus'>+</span><span class='key'>B</span></div></div>
      <div class='shortcut-card' data-search='reader mode f9 ctrl shift r'><div class='shortcut-desc'>Toggle Reader Mode</div><div class='shortcut-keys'><span class='key'>F9</span></div></div>
      <div class='shortcut-card' data-search='fullscreen f11'><div class='shortcut-desc'>Toggle Fullscreen</div><div class='shortcut-keys'><span class='key'>F11</span></div></div>
      <div class='shortcut-card' data-search='history drawer ctrl h'><div class='shortcut-desc'>Browsing History</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>H</span></div></div>
      <div class='shortcut-card' data-search='downloads ctrl shift j'><div class='shortcut-desc'>File Downloads</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>Shift</span><span class='plus'>+</span><span class='key'>J</span></div></div>
      <div class='shortcut-card' data-search='switch space ctrl alt 1 9'><div class='shortcut-desc'>Switch Workspace Space</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>Alt</span><span class='plus'>+</span><span class='key'>1..9</span></div></div>
      <div class='shortcut-card' data-search='new space workspace ctrl alt n'><div class='shortcut-desc'>New Workspace Space</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>Alt</span><span class='plus'>+</span><span class='key'>N</span></div></div>
      <div class='shortcut-card' data-search='zoom in out reset ctrl + - 0'><div class='shortcut-desc'>Zoom In / Out / Reset</div><div class='shortcut-keys'><span class='key'>Ctrl</span><span class='plus'>+</span><span class='key'>+/-/0</span></div></div>
    </div>
  </div>

  <div class='section'>
    <div class='section-title'><span class='section-icon'>&#xE708;</span> Mouse Gestures (Right-Click Drag)</div>
    <div class='shortcuts-grid'>
      <div class='shortcut-card' data-search='gesture back drag left'><div class='shortcut-desc'>Navigate Back</div><div class='shortcut-keys'><span class='key'>Hold Right-Click + Drag &#x2190;</span></div></div>
      <div class='shortcut-card' data-search='gesture forward drag right'><div class='shortcut-desc'>Navigate Forward</div><div class='shortcut-keys'><span class='key'>Hold Right-Click + Drag &#x2192;</span></div></div>
      <div class='shortcut-card' data-search='gesture new tab drag down'><div class='shortcut-desc'>Open New Tab</div><div class='shortcut-keys'><span class='key'>Hold Right-Click + Drag &#x2193;</span></div></div>
      <div class='shortcut-card' data-search='gesture reload drag up'><div class='shortcut-desc'>Reload Active Tab</div><div class='shortcut-keys'><span class='key'>Hold Right-Click + Drag &#x2191;</span></div></div>
    </div>
  </div>
</div>

<script>
  function filterShortcuts(query) {{
    var q = query.toLowerCase().trim();
    var cards = document.querySelectorAll('.shortcut-card');
    cards.forEach(card => {{
      var text = (card.getAttribute('data-search') || '').toLowerCase();
      card.style.display = text.includes(q) ? 'flex' : 'none';
    }});
  }}
  function openCustomize() {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'navigate', url: 'browy://settings#shortcuts' }}));
  }}
</script>
</body>
</html>";
        }

        public static string GetFlagsHtml(IDictionary<string, bool>? featureFlags = null)
        {
            string accent = BrowserSettings.Current.AccentColorHex;
            var flags = FeatureFlagsService.AllFlags;
            var sb = new System.Text.StringBuilder();

            foreach (var flag in flags)
            {
                bool explicitVal = false;
                bool isExplicit = featureFlags != null && featureFlags.TryGetValue(flag.Key, out explicitVal);
                bool effectiveVal = isExplicit ? explicitVal : flag.DefaultValue;
                bool isModified = isExplicit && (explicitVal != flag.DefaultValue);

                string selectedDefault = !isExplicit ? "selected" : "";
                string selectedEnabled = (isExplicit && explicitVal) ? "selected" : "";
                string selectedDisabled = (isExplicit && !explicitVal) ? "selected" : "";

                string statusBadge = isModified
                    ? "<span class='badge badge-modified'>Modified</span>"
                    : "<span class='badge badge-default'>Default</span>";

                sb.Append($@"
        <div class='flag-card' data-key='{flag.Key}' data-category='{flag.Category}' data-search='{flag.Name.ToLowerInvariant()} {flag.Description.ToLowerInvariant()} {flag.Key.ToLowerInvariant()} {flag.Category.ToLowerInvariant()}'>
          <div class='flag-info'>
            <div class='flag-header'>
              <span class='flag-name'>{WebUtility.HtmlEncode(flag.Name)}</span>
              {statusBadge}
              <span class='badge badge-cat'>{WebUtility.HtmlEncode(flag.Category)}</span>
            </div>
            <div class='flag-desc'>{WebUtility.HtmlEncode(flag.Description)}</div>
            <div class='flag-anchor'>#{flag.Key}</div>
          </div>
          <div class='flag-action'>
            <select class='flag-select' onchange=""onFlagChanged('{flag.Key}', this.value)"">
              <option value='default' {selectedDefault}>Default ({(flag.DefaultValue ? "Enabled" : "Disabled")})</option>
              <option value='enabled' {selectedEnabled}>Enabled</option>
              <option value='disabled' {selectedDisabled}>Disabled</option>
            </select>
          </div>
        </div>");
            }

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='utf-8'/>
  <title>Experiments & Flags - Browy</title>
  <style>

  :root {{
    --bg-main: #0b0c0e;
    --bg-card: #141619;
    --bg-card-hover: #181b20;
    --border-color: rgba(255, 255, 255, 0.07);
    --border-subtle: rgba(255, 255, 255, 0.05);
    --border-hover: rgba(255, 255, 255, 0.16);
    --text-primary: #f4f4f5;
    --text-secondary: #a1a1aa;
    --text-muted: #71717a;
    --accent: {accent};
    --accent-hover: {accent};
    --danger-subtle: rgba(239, 68, 68, 0.12);
    --danger-border: rgba(239, 68, 68, 0.28);
    --font-sans: 'Segoe UI Variable Text', 'Segoe UI Variable Display', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    --font-mono: 'Cascadia Code', Consolas, 'SF Mono', monospace;
  }}
  * {{ box-sizing: border-box; margin: 0; padding: 0; user-select: none; -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale; }}

  body {{ background-color: var(--bg-main); color: var(--text-primary); font-family: var(--font-sans); min-height: 100vh; padding: 36px 24px 100px 24px; display: flex; justify-content: center; }}
  .container {{ max-width: 860px; width: 100%; }}
  .header {{ display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px; }}
  .title-row {{ display: flex; align-items: center; gap: 14px; }}
  .icon {{
    width: 40px;
    height: 40px;
    border-radius: 8px;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid var(--border-color);
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: 'Segoe Fluent Icons';
    font-size: 20px;
    color: var(--accent);
  }}
  .title-text h1 {{ font-size: 22px; font-weight: 600; color: var(--text-primary); letter-spacing: -0.02em; }}
  .title-text p {{ font-size: 12.5px; color: var(--text-secondary); margin-top: 2px; }}

  .warning-box {{
    background: rgba(245, 158, 11, 0.08);
    border: 1px solid rgba(245, 158, 11, 0.22);
    border-radius: 6px;
    padding: 12px 16px;
    font-size: 12.5px;
    color: #fde68a;
    display: flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 20px;
    line-height: 1.45;
  }}
  .warning-box span.icon-warn {{ font-family: 'Segoe Fluent Icons'; font-size: 16px; color: #fbbf24; flex-shrink: 0; }}

  .controls-row {{ display: flex; gap: 10px; align-items: center; margin-bottom: 16px; }}
  .search-input {{
    flex: 1;
    background: #101215;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 8px 14px;
    color: var(--text-primary);
    font-size: 13px;
    outline: none;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: border-color, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .search-input:focus {{ border-color: rgba(255, 255, 255, 0.28); box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.15); }}

  .btn-reset {{
    background: transparent;
    border: 1px solid var(--border-color);
    color: var(--text-secondary);
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12px;
    font-weight: 500;
    cursor: pointer;
    white-space: nowrap;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-reset:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-reset:hover {{ background: var(--danger-subtle); border-color: var(--danger-border); color: #fca5a5; }}
  }}

  .categories {{ display: flex; gap: 6px; overflow-x: auto; margin-bottom: 18px; padding-bottom: 4px; }}
  .cat-pill {{
    background: transparent;
    border: 1px solid var(--border-color);
    padding: 4px 10px;
    border-radius: 14px;
    font-size: 11.5px;
    color: var(--text-secondary);
    cursor: pointer;
    white-space: nowrap;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color;
    transition-duration: 140ms;
  }}
  @media (hover: hover) {{
    .cat-pill:hover {{ background: rgba(255, 255, 255, 0.04); color: var(--text-primary); }}
  }}
  .cat-pill.active {{ background: rgba(255, 255, 255, 0.08); border-color: rgba(255, 255, 255, 0.2); color: var(--text-primary); font-weight: 600; }}

  .flags-list {{ display: flex; flex-direction: column; gap: 8px; }}
  .flag-card {{
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 14px 18px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 16px;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.02);
    transition-property: background-color, border-color;
    transition-duration: 140ms;
  }}
  @media (hover: hover) {{
    .flag-card:hover {{ background: var(--bg-card-hover); border-color: var(--border-hover); }}
  }}
  .flag-info {{ flex: 1; min-width: 0; }}
  .flag-header {{ display: flex; align-items: center; gap: 8px; margin-bottom: 4px; flex-wrap: wrap; }}
  .flag-name {{ font-size: 13.5px; font-weight: 600; color: var(--text-primary); }}
  .badge {{ font-size: 10px; font-family: var(--font-mono); font-weight: 600; padding: 2px 6px; border-radius: 4px; text-transform: uppercase; letter-spacing: 0.04em; }}
  .badge-modified {{ background: rgba(245, 158, 11, 0.15); color: #fbbf24; border: 1px solid rgba(245, 158, 11, 0.3); }}
  .badge-default {{ background: rgba(255, 255, 255, 0.05); color: var(--text-muted); border: 1px solid var(--border-color); }}
  .badge-cat {{ background: rgba(255, 255, 255, 0.05); color: var(--text-secondary); border: 1px solid var(--border-subtle); }}
  .flag-desc {{ font-size: 12px; color: var(--text-secondary); line-height: 1.4; margin-bottom: 4px; }}
  .flag-anchor {{ font-size: 11px; font-family: var(--font-mono); color: var(--text-muted); }}

  .flag-action {{ flex-shrink: 0; }}
  .flag-select {{
    background: #101215;
    border: 1px solid var(--border-color);
    color: var(--text-primary);
    padding: 6px 10px;
    border-radius: 6px;
    font-size: 12px;
    font-family: inherit;
    outline: none;
    cursor: pointer;
    -webkit-tap-highlight-color: transparent;
    transition-property: border-color;
    transition-duration: 140ms;
  }}
  .flag-select:focus {{ border-color: rgba(255, 255, 255, 0.28); }}

  .relaunch-bar {{
    position: fixed;
    bottom: 24px;
    left: 50%;
    transform: translateX(-50%);
    background: #141619;
    border: 1px solid rgba(255, 255, 255, 0.14);
    box-shadow: 0 16px 36px rgba(0,0,0,0.6), inset 0 1px 0 rgba(255,255,255,0.04);
    border-radius: 8px;
    padding: 12px 20px;
    display: none;
    align-items: center;
    gap: 16px;
    z-index: 1000;
  }}
  .relaunch-content {{ display: flex; align-items: center; gap: 12px; }}
  .relaunch-text {{ font-size: 12.5px; color: var(--text-primary); }}
  .btn-relaunch {{
    background: #f4f4f5;
    color: #0b0c0e;
    border: 1px solid transparent;
    padding: 6px 14px;
    border-radius: 5px;
    font-size: 12px;
    font-weight: 600;
    cursor: pointer;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, transform;
    transition-duration: 140ms;
  }}
  .btn-relaunch:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-relaunch:hover {{ background: #ffffff; }}
  }}

  @media (prefers-reduced-motion: reduce) {{
    *, *::before, *::after {{
      animation-duration: 0.001ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.001ms !important;
      transform: none !important;
    }}
  }}

</style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <div class='title-row'>
        <div class='icon'>&#xE7C1;</div>
        <div class='title-text'>
          <h1>Experiments & Feature Flags</h1>
          <p>Carefully test experimental runtime features, rendering behaviors, and power-user optimizations.</p>
        </div>
      </div>
    </div>

    <div class='warning-box'>
      <span class='icon-warn'>&#xE7BA;</span>
      <div>
        <strong>Warning: Experimental features ahead!</strong>
        By enabling these features, you could experience subtle behavioral changes or experimental rendering features.
      </div>
    </div>

    <div class='controls-row'>
      <input type='text' class='search-input' id='flagSearch' placeholder='Search experimental flags (name, description, key)...' oninput='filterFlags()' />
      <button class='btn-reset' onclick='resetAllFlags()'>Reset all to default</button>
    </div>

    <div class='categories'>
      <div class='cat-pill active' onclick=""selectCategory('All', this)"">All</div>
      <div class='cat-pill' onclick=""selectCategory('Privacy & Security', this)"">Privacy & Security</div>
      <div class='cat-pill' onclick=""selectCategory('Performance', this)"">Performance</div>
      <div class='cat-pill' onclick=""selectCategory('Productivity', this)"">Productivity</div>
      <div class='cat-pill' onclick=""selectCategory('Browsing Experience', this)"">Browsing Experience</div>
      <div class='cat-pill' onclick=""selectCategory('Media & Audio', this)"">Media & Audio</div>
      <div class='cat-pill' onclick=""selectCategory('User Interface', this)"">User Interface</div>
    </div>

    <div class='flags-list' id='flagsList'>
      {sb}
    </div>
  </div>

  <div class='relaunch-bar' id='relaunchBar' style='display:none;'>
    <div class='relaunch-text'>Your changes will take effect after restarting Browy.</div>
    <button class='btn-relaunch' onclick='relaunchBrowser()'>Relaunch Now</button>
  </div>

  <script>
    var currentCategory = 'All';

    function selectCategory(cat, el) {{
      currentCategory = cat;
      document.querySelectorAll('.cat-pill').forEach(p => p.classList.remove('active'));
      el.classList.add('active');
      filterFlags();
    }}

    function filterFlags() {{
      var q = (document.getElementById('flagSearch').value || '').toLowerCase().trim();
      var cards = document.querySelectorAll('.flag-card');

      cards.forEach(card => {{
        var cat = card.getAttribute('data-category') || '';
        var search = card.getAttribute('data-search') || '';

        var matchCat = (currentCategory === 'All') || (cat === currentCategory);
        var matchQuery = !q || search.includes(q);

        card.style.display = (matchCat && matchQuery) ? 'flex' : 'none';
      }});
    }}

    function onFlagChanged(key, value) {{
      window.chrome.webview.postMessage(JSON.stringify({{
        action: 'set_feature_flag',
        key: key,
        value: value
      }}));
      document.getElementById('relaunchBar').style.display = 'flex';
    }}

    function resetAllFlags() {{
      window.chrome.webview.postMessage(JSON.stringify({{ action: 'reset_feature_flags' }}));
      location.reload();
    }}

    function relaunchBrowser() {{
      window.chrome.webview.postMessage(JSON.stringify({{ action: 'relaunch_browser' }}));
    }}
  </script>
</body>
</html>";
        }

        public static string GetSearchEnginesHtml(IEnumerable<SearchEngineItem>? engines = null)
        {
            var settings = BrowserSettings.Current;
            string accent = settings.AccentColorHex;
            var list = (engines ?? settings.SearchEngines ?? BrowserSettingsData.GetDefaultSearchEngines()).ToList();

            var cardsSb = new System.Text.StringBuilder();
            foreach (var item in list)
            {
                string id = WebUtility.HtmlEncode(item.Id);
                string name = WebUtility.HtmlEncode(item.Name);
                string keyword = WebUtility.HtmlEncode(item.Keyword);
                string queryUrl = WebUtility.HtmlEncode(item.QueryUrl);
                string glyph = string.IsNullOrWhiteSpace(item.IconGlyph) ? "&#xE721;" : WebUtility.HtmlEncode(item.IconGlyph);
                string isDefaultBadge = item.IsDefault
                    ? "<span class='badge badge-default-active'>Default</span>"
                    : $"<button class='btn-action' onclick=\"setDefaultEngine('{id}')\">Set as default</button>";

                string deleteBtn = (!item.IsDefault && !item.IsBuiltIn)
                    ? $"<button class='btn-danger-sm' onclick=\"deleteEngine('{id}')\" title='Remove engine'>&#xE74D;</button>"
                    : "";

                string testUrl = item.QueryUrl.Contains("%s")
                    ? item.QueryUrl.Replace("%s", "browy")
                    : item.QueryUrl + "browy";

                cardsSb.Append($@"
                <div class='engine-card' data-search='{name.ToLowerInvariant()} {keyword.ToLowerInvariant()}'>
                    <div class='engine-icon'>{glyph}</div>
                    <div class='engine-info'>
                        <div class='engine-name-row'>
                            <span class='engine-name'>{name}</span>
                            <span class='bang-pill'>{keyword}</span>
                            {(item.IsBuiltIn ? "<span class='badge badge-builtin'>Built-in</span>" : "<span class='badge badge-custom'>Custom</span>")}
                        </div>
                        <div class='engine-url' title='{queryUrl}'>{queryUrl}</div>
                    </div>
                    <div class='engine-actions'>
                        <button class='btn-action' onclick=""testEngine('{testUrl}')"" title='Test query with ""browy""'>Test</button>
                        {isDefaultBadge}
                        {deleteBtn}
                    </div>
                </div>");
            }

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8'>
  <title>Search Engines &amp; Bangs - Browy</title>
  <style>

  :root {{
    --bg-main: #0b0c0e;
    --bg-card: #141619;
    --bg-card-hover: #181b20;
    --border-color: rgba(255, 255, 255, 0.07);
    --border-subtle: rgba(255, 255, 255, 0.05);
    --border-hover: rgba(255, 255, 255, 0.16);
    --text-primary: #f4f4f5;
    --text-secondary: #a1a1aa;
    --text-muted: #71717a;
    --accent: {accent};
    --accent-hover: {accent};
    --danger-subtle: rgba(239, 68, 68, 0.12);
    --danger-border: rgba(239, 68, 68, 0.28);
    --font-sans: 'Segoe UI Variable Text', 'Segoe UI Variable Display', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    --font-mono: 'Cascadia Code', Consolas, 'SF Mono', monospace;
  }}
  * {{ box-sizing: border-box; margin: 0; padding: 0; user-select: none; -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale; }}

  body {{ background-color: var(--bg-main); color: var(--text-primary); font-family: var(--font-sans); min-height: 100vh; padding: 36px 24px 80px; display: flex; justify-content: center; }}
  .container {{ width: 100%; max-width: 860px; }}
  .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }}
  .title-row {{ display: flex; align-items: center; gap: 14px; }}
  .icon {{
    width: 40px;
    height: 40px;
    border-radius: 8px;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid var(--border-color);
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: 'Segoe Fluent Icons';
    font-size: 20px;
    color: var(--accent);
  }}
  h1 {{ font-size: 22px; font-weight: 600; color: var(--text-primary); letter-spacing: -0.02em; }}
  p {{ font-size: 12.5px; color: var(--text-secondary); margin-top: 2px; }}

  .btn-primary {{
    background: #f4f4f5;
    color: #0b0c0e;
    border: 1px solid transparent;
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, color, transform, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-primary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-primary:hover {{ background: #ffffff; box-shadow: 0 2px 8px rgba(0,0,0,0.3); }}
  }}

  .btn-secondary {{
    background: transparent;
    color: var(--text-primary);
    border: 1px solid var(--border-color);
    padding: 7px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-secondary:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-secondary:hover {{ background: rgba(255, 255, 255, 0.05); border-color: var(--border-hover); }}
  }}

  .btn-danger {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 7px 14px;
    border-radius: 6px;
    cursor: pointer;
    font-size: 12.5px;
    font-weight: 500;
    font-family: inherit;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .btn-danger-sm {{
    background: var(--danger-subtle);
    color: #fca5a5;
    border: 1px solid var(--danger-border);
    padding: 3px 9px;
    border-radius: 5px;
    cursor: pointer;
    font-size: 11.5px;
    font-weight: 500;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-danger-sm:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-danger-sm:hover {{ background: rgba(239, 68, 68, 0.2); border-color: rgba(239, 68, 68, 0.4); color: #ffffff; }}
  }}

  .search-row {{ display: flex; gap: 10px; margin-bottom: 16px; }}
  .search-input {{
    flex: 1;
    background: #101215;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 8px 14px;
    color: var(--text-primary);
    font-size: 13px;
    outline: none;
    font-family: inherit;
    -webkit-tap-highlight-color: transparent;
    transition-property: border-color, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .search-input:focus {{ border-color: rgba(255, 255, 255, 0.28); box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.15); }}

  .bang-simulator {{
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 16px 18px;
    margin-bottom: 20px;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.02);
  }}
  .sim-title {{ font-size: 13px; font-weight: 600; color: var(--text-primary); margin-bottom: 4px; display: flex; align-items: center; gap: 6px; }}
  .sim-desc {{ font-size: 12px; color: var(--text-secondary); margin-bottom: 10px; }}
  .sim-form {{ display: flex; gap: 8px; }}
  .sim-input {{
    flex: 1;
    background: #101215;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 7px 12px;
    color: var(--text-primary);
    font-size: 12.5px;
    font-family: var(--font-mono);
    outline: none;
    transition-property: border-color;
    transition-duration: 140ms;
  }}
  .sim-input:focus {{ border-color: rgba(255, 255, 255, 0.28); }}

  .add-form {{
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 18px 20px;
    margin-bottom: 20px;
    display: none;
    flex-direction: column;
    gap: 12px;
  }}
  .add-form h2 {{ font-size: 15px; font-weight: 600; color: var(--text-primary); }}
  .form-grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 12px; }}
  .form-group {{ display: flex; flex-direction: column; gap: 4px; }}
  .form-group label {{ font-size: 11.5px; font-weight: 500; color: var(--text-secondary); }}
  .form-group input {{
    background: #101215;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 7px 12px;
    color: var(--text-primary);
    font-size: 12.5px;
    font-family: var(--font-mono);
    outline: none;
  }}
  .form-group input:focus {{ border-color: rgba(255, 255, 255, 0.28); }}
  .form-actions {{ display: flex; justify-content: flex-end; gap: 8px; margin-top: 4px; }}

  .engines-list {{ display: flex; flex-direction: column; gap: 8px; }}
  .engine-card {{
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 12px 16px;
    display: flex;
    align-items: center;
    gap: 14px;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.02);
    transition-property: background-color, border-color;
    transition-duration: 120ms;
  }}
  @media (hover: hover) {{
    .engine-card:hover {{ background: var(--bg-card-hover); border-color: var(--border-hover); }}
  }}
  .engine-icon {{
    font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets';
    font-size: 18px;
    color: var(--accent);
    width: 34px;
    height: 34px;
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid var(--border-color);
    border-radius: 6px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }}
  .engine-info {{ flex: 1; min-width: 0; }}
  .engine-name-row {{ display: flex; align-items: center; gap: 8px; margin-bottom: 3px; flex-wrap: wrap; }}
  .engine-name {{ font-size: 13.5px; font-weight: 600; color: var(--text-primary); }}
  .bang-pill {{
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid var(--border-color);
    padding: 1px 6px;
    border-radius: 4px;
    font-family: var(--font-mono);
    font-size: 11px;
    color: var(--text-primary);
    font-weight: 600;
  }}
  .badge {{ font-size: 10px; font-family: var(--font-mono); font-weight: 600; padding: 2px 6px; border-radius: 4px; text-transform: uppercase; letter-spacing: 0.04em; }}
  .badge-default-active {{ background: rgba(255, 255, 255, 0.08); color: var(--text-primary); border: 1px solid var(--border-hover); }}
  .badge-builtin {{ background: rgba(255, 255, 255, 0.04); color: var(--text-muted); border: 1px solid var(--border-subtle); }}
  .badge-custom {{ background: rgba(16, 185, 129, 0.12); color: #6ee7b7; border: 1px solid rgba(16, 185, 129, 0.25); }}
  .engine-url {{ font-size: 11.5px; color: var(--text-muted); font-family: var(--font-mono); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }}
  .engine-actions {{ display: flex; gap: 6px; align-items: center; }}
  .btn-action {{
    background: transparent;
    border: 1px solid var(--border-color);
    color: var(--text-secondary);
    padding: 4px 10px;
    border-radius: 5px;
    font-size: 11px;
    font-family: inherit;
    cursor: pointer;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }}
  .btn-action:not(:disabled):active {{ transform: scale(0.96); }}
  @media (hover: hover) {{
    .btn-action:hover {{ background: rgba(255, 255, 255, 0.06); color: var(--text-primary); border-color: var(--border-hover); }}
  }}

  @media (prefers-reduced-motion: reduce) {{
    *, *::before, *::after {{
      animation-duration: 0.001ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.001ms !important;
      transform: none !important;
    }}
  }}

</style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <div class='title-row'>
        <div class='icon'>&#xE721;</div>
        <div>
          <h1>Search Engines &amp; Bangs</h1>
          <p>Configure default search providers and instant omnibox bang shortcuts (e.g. <code>!yt</code>, <code>!gh</code>, <code>!w</code>, <code>!ddg</code>)</p>
        </div>
      </div>
      <button class='btn-primary' onclick='toggleAddForm()'>+ Add Search Engine</button>
    </div>

    <!-- Bang Simulator -->
    <div class='bang-simulator'>
      <div class='sim-title'><span style=""font-family: 'Segoe Fluent Icons';"">&#xE768;</span> Test Bang Omnibox Query</div>
      <div class='sim-desc'>Type a shortcut followed by your search term (e.g., <code>!gh webview2</code> or <code>!yt synthwave</code>) to test instant bang routing:</div>
      <div class='sim-form'>
        <input type='text' id='simInput' class='sim-input' placeholder='!gh browy' onkeydown='if(event.key === ""Enter"") runSimSearch()' />
        <button class='btn-primary' onclick='runSimSearch()'>Go</button>
      </div>
    </div>

    <!-- Add Engine Form -->
    <div class='add-form' id='addForm'>
      <h2>Add Custom Search Engine</h2>
      <div class='form-grid'>
        <div class='form-group'>
          <label>Engine Name</label>
          <input type='text' id='engineName' placeholder='e.g. Stack Overflow' />
        </div>
        <div class='form-group'>
          <label>Shortcut / Bang</label>
          <input type='text' id='engineKeyword' placeholder='!so' />
        </div>
      </div>
      <div class='form-group'>
        <label>URL with %s in place of query</label>
        <input type='text' id='engineUrl' placeholder='https://stackoverflow.com/search?q=%s' />
      </div>
      <div class='form-actions'>
        <button class='btn-secondary' onclick='toggleAddForm()'>Cancel</button>
        <button class='btn-primary' onclick='submitAddEngine()'>Save Engine</button>
      </div>
    </div>

    <!-- Search / Filter -->
    <div class='search-row'>
      <input type='text' id='searchFilter' class='search-input' placeholder='Filter search engines and shortcuts...' oninput='filterEngines(this.value)' />
    </div>

    <!-- Engines List -->
    <div class='engines-list' id='enginesList'>
      {cardsSb}
    </div>
  </div>

  <script>
    function toggleAddForm() {{
      var form = document.getElementById('addForm');
      form.style.display = form.style.display === 'flex' ? 'none' : 'flex';
      if (form.style.display === 'flex') {{
        document.getElementById('engineName').focus();
      }}
    }}

    function filterEngines(q) {{
      q = (q || '').toLowerCase().trim();
      var cards = document.querySelectorAll('.engine-card');
      cards.forEach(c => {{
        var text = c.getAttribute('data-search') || '';
        c.style.display = (!q || text.includes(q)) ? 'flex' : 'none';
      }});
    }}

    function setDefaultEngine(id) {{
      window.chrome.webview.postMessage(JSON.stringify({{
        action: 'set_default_search_engine',
        id: id
      }}));
      location.reload();
    }}

    function deleteEngine(id) {{
      window.chrome.webview.postMessage(JSON.stringify({{
        action: 'delete_search_engine',
        id: id
      }}));
      location.reload();
    }}

    function testEngine(url) {{
      window.chrome.webview.postMessage(JSON.stringify({{
        action: 'navigate',
        url: url
      }}));
    }}

    function runSimSearch() {{
      var input = document.getElementById('simInput').value.trim();
      if (!input) return;
      window.chrome.webview.postMessage(JSON.stringify({{
        action: 'omnibox_bang_search',
        query: input
      }}));
    }}

    function submitAddEngine() {{
      var name = document.getElementById('engineName').value.trim();
      var keyword = document.getElementById('engineKeyword').value.trim();
      var url = document.getElementById('engineUrl').value.trim();

      if (!name || !keyword || !url) {{
        alert('Please fill out all fields.');
        return;
      }}
      if (!keyword.startsWith('!')) {{
        keyword = '!' + keyword;
      }}

      window.chrome.webview.postMessage(JSON.stringify({{
        action: 'add_search_engine',
        name: name,
        keyword: keyword,
        queryUrl: url
      }}));

      toggleAddForm();
      location.reload();
    }}
  </script>
</body>
</html>";
        }
    }
}
