using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Browy.Models;

namespace Browy.Services
{
    public static class InternalHubService
    {
        public static string GetHistoryHtml(IEnumerable<HistoryItem> history)
        {
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
  * {{ box-sizing: border-box; margin: 0; padding: 0; font-family: 'Segoe UI Variable Display', 'Segoe UI', system-ui, sans-serif; }}
  body {{ background: #0D0D12; color: #F1F5F9; min-height: 100vh; padding: 40px 24px; display: flex; justify-content: center; }}
  .container {{ width: 100%; max-width: 860px; }}
  .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }}
  .title-area {{ display: flex; align-items: center; gap: 14px; }}
  .header-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 28px; color: #6366F1; }}
  h1 {{ font-size: 26px; font-weight: 600; letter-spacing: -0.02em; }}
  .header-actions {{ display: flex; align-items: center; gap: 10px; }}
  .btn-danger {{ background: #EF44441F; color: #F87171; border: 1px solid #EF44443F; padding: 8px 16px; border-radius: 8px; cursor: pointer; font-size: 13px; font-weight: 500; transition: all 0.15s ease; display: flex; align-items: center; gap: 6px; }}
  .btn-danger:hover {{ background: #EF44443F; color: #FFFFFF; }}
  .btn-danger-sm {{ background: #EF44441F; color: #F87171; border: 1px solid #EF44443F; padding: 4px 10px; border-radius: 6px; cursor: pointer; font-size: 11.5px; font-weight: 500; }}
  .btn-danger-sm:hover {{ background: #EF44443F; color: #FFFFFF; }}
  .tabs {{ display: flex; gap: 8px; margin-bottom: 18px; border-bottom: 1px solid #202030; padding-bottom: 8px; }}
  .tab-btn {{ background: transparent; border: none; color: #94A3B8; font-size: 13.5px; font-weight: 500; padding: 8px 14px; border-radius: 6px; cursor: pointer; transition: all 0.15s ease; }}
  .tab-btn.active {{ background: #1E1E2E; color: #FFFFFF; font-weight: 600; }}
  .toolbar {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 18px; gap: 12px; }}
  .search-bar {{ flex: 1; background: #181822; border: 1px solid #282838; border-radius: 10px; padding: 11px 16px; color: #FFFFFF; font-size: 13.5px; outline: none; transition: border-color 0.15s ease; }}
  .search-bar:focus {{ border-color: #6366F1; box-shadow: 0 0 0 2px #6366F13F; }}
  .batch-actions {{ display: flex; align-items: center; gap: 12px; }}
  .bucket-section {{ margin-bottom: 24px; }}
  .bucket-title {{ font-size: 14px; font-weight: 600; color: #CBD5E1; margin-bottom: 10px; display: flex; align-items: center; gap: 8px; }}
  .bucket-badge {{ background: #202030; color: #94A3B8; font-size: 11px; padding: 2px 7px; border-radius: 10px; font-weight: normal; }}
  .bucket-list {{ display: flex; flex-direction: column; gap: 8px; }}
  .history-card {{ display: flex; align-items: center; background: #14141E; border: 1px solid #202030; border-radius: 10px; padding: 12px 16px; gap: 14px; transition: all 0.12s ease; }}
  .history-card:hover {{ background: #1A1A28; border-color: #303048; transform: translateY(-1px); }}
  .hist-check {{ width: 16px; height: 16px; cursor: pointer; accent-color: #6366F1; }}
  .card-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 16px; color: #94A3B8; width: 20px; text-align: center; }}
  .card-info {{ flex: 1; min-width: 0; cursor: pointer; }}
  .card-title {{ font-size: 13.5px; font-weight: 500; color: #F8FAFC; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }}
  .card-url {{ font-size: 12px; color: #64748B; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-top: 2px; }}
  .card-time {{ font-size: 12px; color: #64748B; margin-right: 6px; }}
  .card-delete {{ background: transparent; border: none; font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 13px; color: #64748B; width: 28px; height: 28px; border-radius: 6px; cursor: pointer; display: flex; align-items: center; justify-content: center; }}
  .card-delete:hover {{ background: #EF444420; color: #F87171; }}
  .domain-card {{ background: #14141E; border: 1px solid #202030; border-radius: 10px; margin-bottom: 10px; overflow: hidden; }}
  .domain-header {{ padding: 14px 18px; display: flex; justify-content: space-between; align-items: center; cursor: pointer; background: #161622; }}
  .domain-header:hover {{ background: #1C1C2C; }}
  .domain-info {{ display: flex; align-items: center; gap: 12px; }}
  .domain-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 16px; color: #818CF8; }}
  .domain-name {{ font-size: 14px; font-weight: 600; color: #F8FAFC; }}
  .domain-actions {{ display: flex; align-items: center; gap: 12px; }}
  .chevron {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 12px; color: #94A3B8; transition: transform 0.2s; }}
  .domain-items {{ padding: 10px 14px; display: flex; flex-direction: column; gap: 6px; background: #0E0E16; }}
  .modal {{ display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.6); align-items: center; justify-content: center; z-index: 100; }}
  .modal-content {{ background: #181822; border: 1px solid #282838; border-radius: 12px; padding: 24px; width: 380px; box-shadow: 0 20px 40px rgba(0,0,0,0.5); }}
  .modal-title {{ font-size: 18px; font-weight: 600; margin-bottom: 14px; }}
  .timeframe-select {{ width: 100%; background: #202030; border: 1px solid #303048; color: white; padding: 10px; border-radius: 8px; margin-bottom: 20px; outline: none; }}
  .modal-buttons {{ display: flex; justify-content: flex-end; gap: 10px; }}
  .btn-secondary {{ background: #202030; color: #CBD5E1; border: 1px solid #303048; padding: 8px 16px; border-radius: 8px; cursor: pointer; font-size: 13px; }}
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
            return @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<title>History - Browy</title>
<style>
  * { box-sizing: border-box; margin: 0; padding: 0; font-family: 'Segoe UI Variable Display', 'Segoe UI', system-ui, sans-serif; }
  body { background: #0D0D12; color: #F1F5F9; min-height: 100vh; padding: 60px 24px; display: flex; justify-content: center; }
  .empty-state { text-align: center; color: #64748B; margin-top: 40px; }
  .empty-icon { font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 48px; margin-bottom: 12px; opacity: 0.5; color: #6366F1; }
  .empty-title { font-size: 20px; font-weight: 600; color: #CBD5E1; margin-bottom: 6px; }
  .empty-sub { font-size: 13px; }
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
  * {{ box-sizing: border-box; margin: 0; padding: 0; font-family: 'Segoe UI Variable Display', 'Segoe UI', system-ui, sans-serif; }}
  body {{ background: #0D0D12; color: #F1F5F9; min-height: 100vh; padding: 48px 24px; display: flex; justify-content: center; }}
  .container {{ width: 100%; max-width: 840px; }}
  .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 28px; }}
  .title-area {{ display: flex; align-items: center; gap: 14px; }}
  .header-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 28px; color: #F59E0B; }}
  h1 {{ font-size: 26px; font-weight: 600; letter-spacing: -0.02em; }}
  .search-bar {{ width: 100%; background: #181822; border: 1px solid #282838; border-radius: 10px; padding: 12px 18px; color: #FFFFFF; font-size: 14px; margin-bottom: 24px; outline: none; transition: border-color 0.15s ease; }}
  .search-bar:focus {{ border-color: #F59E0B; box-shadow: 0 0 0 2px #F59E0B3F; }}
  .bookmark-list {{ display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 12px; }}
  .bookmark-card {{ display: flex; align-items: center; background: #14141E; border: 1px solid #202030; border-radius: 10px; padding: 14px 16px; gap: 12px; transition: all 0.12s ease; }}
  .bookmark-card:hover {{ background: #1A1A28; border-color: #303048; transform: translateY(-1px); }}
  .card-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 16px; color: #F59E0B; }}
  .card-info {{ flex: 1; min-width: 0; cursor: pointer; }}
  .card-title {{ font-size: 13.5px; font-weight: 500; color: #F8FAFC; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }}
  .card-url {{ font-size: 11.5px; color: #64748B; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-top: 2px; }}
  .card-delete {{ background: transparent; border: none; font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 13px; color: #64748B; width: 28px; height: 28px; border-radius: 6px; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: all 0.12s ease; }}
  .card-delete:hover {{ background: #EF444420; color: #F87171; }}
  .empty-state {{ text-align: center; padding: 64px 0; color: #64748B; grid-column: 1 / -1; }}
  .empty-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 48px; margin-bottom: 12px; opacity: 0.5; }}
  .empty-title {{ font-size: 18px; font-weight: 600; color: #CBD5E1; margin-bottom: 4px; }}
  .empty-sub {{ font-size: 13px; }}
</style>
</head>
<body>
<div class='container'>
  <div class='header'>
    <div class='title-area'>
      <div class='header-icon'>&#xE734;</div>
      <h1>Bookmarks</h1>
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
</script>
</body>
</html>";
        }

        public static string GetDownloadsHtml(IEnumerable<DownloadItem> downloads)
        {
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

                    sb.Append($@"
                    <div class='download-card' data-id='{id}' data-filename='{fileName}'>
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
  * {{ box-sizing: border-box; margin: 0; padding: 0; font-family: 'Segoe UI Variable Display', 'Segoe UI', system-ui, sans-serif; }}
  body {{ background: #0D0D12; color: #F1F5F9; min-height: 100vh; padding: 48px 24px; display: flex; justify-content: center; }}
  .container {{ width: 100%; max-width: 840px; }}
  .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }}
  .title-area {{ display: flex; align-items: center; gap: 14px; }}
  .header-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 28px; color: #10B981; }}
  h1 {{ font-size: 26px; font-weight: 600; letter-spacing: -0.02em; }}
  .header-actions {{ display: flex; align-items: center; gap: 10px; }}
  .btn-secondary {{ background: #181822; color: #CBD5E1; border: 1px solid #282838; padding: 8px 14px; border-radius: 8px; cursor: pointer; font-size: 13px; font-weight: 500; transition: all 0.15s ease; }}
  .btn-secondary:hover {{ background: #222230; color: #FFFFFF; }}
  .search-bar {{ width: 100%; background: #181822; border: 1px solid #282838; border-radius: 10px; padding: 12px 18px; color: #FFFFFF; font-size: 14px; margin-bottom: 24px; outline: none; transition: border-color 0.15s ease; }}
  .search-bar:focus {{ border-color: #10B981; box-shadow: 0 0 0 2px #10B9813F; }}
  .download-list {{ display: flex; flex-direction: column; gap: 12px; }}
  .download-card {{ display: flex; align-items: center; background: #14141E; border: 1px solid #202030; border-radius: 12px; padding: 16px 20px; gap: 16px; transition: all 0.12s ease; }}
  .download-card:hover {{ background: #1A1A28; border-color: #303048; }}
  .card-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 24px; color: #10B981; }}
  .card-info {{ flex: 1; min-width: 0; }}
  .card-title {{ font-size: 14px; font-weight: 600; color: #F8FAFC; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }}
  .card-status {{ font-size: 12px; color: #94A3B8; margin-top: 3px; }}
  .progress-bar-container {{ height: 4px; background: #202030; border-radius: 2px; margin-top: 8px; overflow: hidden; }}
  .progress-bar-fill {{ height: 100%; background: #10B981; border-radius: 2px; transition: width 0.3s ease; }}
  .card-controls {{ display: flex; align-items: center; gap: 8px; }}
  .btn-action {{ background: #202030; color: #E2E8F0; border: 1px solid #303048; padding: 6px 14px; border-radius: 6px; cursor: pointer; font-size: 12px; font-weight: 500; transition: all 0.12s ease; }}
  .btn-action:hover {{ background: #303048; color: #FFFFFF; }}
  .btn-action.btn-primary {{ background: #10B981; color: #FFFFFF; border-color: #059669; }}
  .btn-action.btn-primary:hover {{ background: #059669; }}
  .empty-state {{ text-align: center; padding: 64px 0; color: #64748B; }}
  .empty-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 48px; margin-bottom: 12px; opacity: 0.5; }}
  .empty-title {{ font-size: 18px; font-weight: 600; color: #CBD5E1; margin-bottom: 4px; }}
  .empty-sub {{ font-size: 13px; }}
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
  <input type='text' class='search-bar' placeholder='Search downloads by file name...' oninput='filterDownloads(this.value)'>
  <div class='download-list' id='downloadList'>
    {itemsHtml}
  </div>
</div>
<script>
  function filterDownloads(query) {{
    var q = query.toLowerCase().trim();
    var cards = document.querySelectorAll('.download-card');
    cards.forEach(card => {{
      var name = (card.getAttribute('data-filename') || '').toLowerCase();
      card.style.display = name.includes(q) ? 'flex' : 'none';
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
  * {{ box-sizing: border-box; margin: 0; padding: 0; font-family: 'Segoe UI Variable Display', 'Segoe UI', system-ui, sans-serif; }}
  body {{ background: #0D0D12; color: #F1F5F9; min-height: 100vh; padding: 48px 24px; display: flex; justify-content: center; }}
  .container {{ width: 100%; max-width: 840px; }}
  .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 28px; }}
  .title-area {{ display: flex; align-items: center; gap: 14px; }}
  .header-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 28px; color: #8B5CF6; }}
  h1 {{ font-size: 26px; font-weight: 600; letter-spacing: -0.02em; }}
  .btn-primary {{ background: #8B5CF6; color: #FFFFFF; border: none; padding: 9px 18px; border-radius: 8px; cursor: pointer; font-size: 13px; font-weight: 600; display: flex; align-items: center; gap: 8px; transition: background 0.15s ease; }}
  .btn-primary:hover {{ background: #7C3AED; }}
  .ext-list {{ display: flex; flex-direction: column; gap: 12px; }}
  .ext-card {{ display: flex; align-items: flex-start; background: #14141E; border: 1px solid #202030; border-radius: 12px; padding: 18px 20px; gap: 16px; transition: all 0.12s ease; }}
  .ext-card:hover {{ background: #1A1A28; border-color: #303048; }}
  .ext-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 24px; color: #A78BFA; padding-top: 2px; }}
  .ext-info {{ flex: 1; min-width: 0; }}
  .ext-header {{ display: flex; align-items: center; gap: 8px; margin-bottom: 4px; }}
  .ext-name {{ font-size: 15px; font-weight: 600; color: #F8FAFC; }}
  .ext-version {{ font-size: 11px; background: #28283C; color: #94A3B8; padding: 2px 6px; border-radius: 4px; }}
  .ext-desc {{ font-size: 12.5px; color: #94A3B8; line-height: 1.4; margin-bottom: 8px; }}
  .ext-path {{ font-size: 11px; color: #64748B; font-family: Consolas, monospace; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }}
  .ext-actions {{ display: flex; align-items: center; gap: 14px; margin-left: 8px; }}
  .btn-remove {{ background: transparent; border: none; font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 14px; color: #64748B; width: 32px; height: 32px; border-radius: 6px; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: all 0.12s ease; }}
  .btn-remove:hover {{ background: #EF444420; color: #F87171; }}
  .switch {{ position: relative; display: inline-block; width: 40px; height: 22px; }}
  .switch input {{ opacity: 0; width: 0; height: 0; }}
  .slider {{ position: absolute; cursor: pointer; top: 0; left: 0; right: 0; bottom: 0; background-color: #2D3748; transition: .2s; border-radius: 22px; }}
  .slider:before {{ position: absolute; content: ''; height: 16px; width: 16px; left: 3px; bottom: 3px; background-color: white; transition: .2s; border-radius: 50%; }}
  input:checked + .slider {{ background-color: #8B5CF6; }}
  input:checked + .slider:before {{ transform: translateX(18px); }}
  .empty-state {{ text-align: center; padding: 64px 0; color: #64748B; }}
  .empty-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 48px; margin-bottom: 12px; opacity: 0.5; }}
  .empty-title {{ font-size: 18px; font-weight: 600; color: #CBD5E1; margin-bottom: 4px; }}
  .empty-sub {{ font-size: 13px; }}
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
    }
}
