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
            var itemsHtml = string.Empty;

            if (items.Count == 0)
            {
                itemsHtml = @"<div class='empty-state'>
                    <div class='empty-icon'>&#xE81C;</div>
                    <div class='empty-title'>No browsing history yet</div>
                    <div class='empty-sub'>Pages you visit will show up here.</div>
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
                    string time = WebUtility.HtmlEncode(item.FormattedTime);

                    sb.Append($@"
                    <div class='history-card' data-url='{url}' data-title='{title}' data-index='{i}'>
                        <div class='card-icon'>&#xE774;</div>
                        <div class='card-info' onclick=""window.chrome.webview.postMessage(JSON.stringify({{action: 'navigate', url: '{url}'}}))"">
                            <div class='card-title'>{title}</div>
                            <div class='card-url'>{url}</div>
                        </div>
                        <div class='card-time'>{time}</div>
                        <button class='card-delete' onclick=""deleteItem('{url}', {i})"" title='Delete from history'>&#xE74D;</button>
                    </div>");
                }
                itemsHtml = sb.ToString();
            }

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<title>History - Browy</title>
<style>
  * {{ box-sizing: border-box; margin: 0; padding: 0; font-family: 'Segoe UI Variable Display', 'Segoe UI', system-ui, sans-serif; }}
  body {{ background: #0D0D12; color: #F1F5F9; min-height: 100vh; padding: 48px 24px; display: flex; justify-content: center; }}
  .container {{ width: 100%; max-width: 840px; }}
  .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 28px; }}
  .title-area {{ display: flex; align-items: center; gap: 14px; }}
  .header-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 28px; color: #6366F1; }}
  h1 {{ font-size: 26px; font-weight: 600; letter-spacing: -0.02em; }}
  .btn-danger {{ background: #EF44441F; color: #F87171; border: 1px solid #EF44443F; padding: 8px 16px; border-radius: 8px; cursor: pointer; font-size: 13px; font-weight: 500; transition: all 0.15s ease; display: flex; align-items: center; gap: 6px; }}
  .btn-danger:hover {{ background: #EF44443F; color: #FFFFFF; }}
  .search-bar {{ width: 100%; background: #181822; border: 1px solid #282838; border-radius: 10px; padding: 12px 18px; color: #FFFFFF; font-size: 14px; margin-bottom: 24px; outline: none; transition: border-color 0.15s ease; }}
  .search-bar:focus {{ border-color: #6366F1; box-shadow: 0 0 0 2px #6366F13F; }}
  .history-list {{ display: flex; flex-direction: column; gap: 8px; }}
  .history-card {{ display: flex; align-items: center; background: #14141E; border: 1px solid #202030; border-radius: 10px; padding: 12px 16px; gap: 14px; transition: all 0.12s ease; }}
  .history-card:hover {{ background: #1A1A28; border-color: #303048; transform: translateY(-1px); }}
  .card-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 16px; color: #94A3B8; width: 24px; text-align: center; }}
  .card-info {{ flex: 1; min-width: 0; cursor: pointer; }}
  .card-title {{ font-size: 14px; font-weight: 500; color: #F8FAFC; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }}
  .card-url {{ font-size: 12px; color: #64748B; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-top: 2px; }}
  .card-time {{ font-size: 12px; color: #64748B; margin-right: 6px; }}
  .card-delete {{ background: transparent; border: none; font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 13px; color: #64748B; width: 28px; height: 28px; border-radius: 6px; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: all 0.12s ease; }}
  .card-delete:hover {{ background: #EF444420; color: #F87171; }}
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
      <div class='header-icon'>&#xE81C;</div>
      <h1>Browsing History</h1>
    </div>
    <button class='btn-danger' onclick=""clearAllHistory()""><span style=""font-family: 'Segoe Fluent Icons';"">&#xE74D;</span> Clear History</button>
  </div>
  <input type='text' id='search' class='search-bar' placeholder='Search history by title, URL or domain...' oninput='filterHistory(this.value)'>
  <div class='history-list' id='historyList'>
    {itemsHtml}
  </div>
</div>
<script>
  function filterHistory(query) {{
    var q = query.toLowerCase().trim();
    var cards = document.querySelectorAll('.history-card');
    cards.forEach(card => {{
      var title = (card.getAttribute('data-title') || '').toLowerCase();
      var url = (card.getAttribute('data-url') || '').toLowerCase();
      card.style.display = (title.includes(q) || url.includes(q)) ? 'flex' : 'none';
    }});
  }}
  function deleteItem(url, index) {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'delete_history_entry', url: url, index: index }}));
    var card = document.querySelector(`.history-card[data-index='${{index}}']`);
    if (card) card.remove();
  }}
  function clearAllHistory() {{
    if (confirm('Are you sure you want to clear your entire browsing history?')) {{
      window.chrome.webview.postMessage(JSON.stringify({{ action: 'clear_all_history' }}));
      document.getElementById('historyList').innerHTML = `<div class='empty-state'><div class='empty-icon'>&#xE81C;</div><div class='empty-title'>No browsing history yet</div><div class='empty-sub'>Pages you visit will show up here.</div></div>`;
    }}
  }}
</script>
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

                    sb.Append($@"
                    <div class='download-card'>
                        <div class='card-icon'>&#xE896;</div>
                        <div class='card-info'>
                            <div class='card-title'>{fileName}</div>
                            <div class='card-status'>{status}</div>
                        </div>
                        <button class='btn-action' onclick=""openFile('{fullPath}')"" title='Open file'>Open</button>
                        <button class='btn-action' onclick=""showInFolder('{fullPath}')"" title='Show in folder'>Show in folder</button>
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
  .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 28px; }}
  .title-area {{ display: flex; align-items: center; gap: 14px; }}
  .header-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 28px; color: #10B981; }}
  h1 {{ font-size: 26px; font-weight: 600; letter-spacing: -0.02em; }}
  .download-list {{ display: flex; flex-direction: column; gap: 10px; }}
  .download-card {{ display: flex; align-items: center; background: #14141E; border: 1px solid #202030; border-radius: 10px; padding: 14px 18px; gap: 14px; transition: all 0.12s ease; }}
  .download-card:hover {{ background: #1A1A28; border-color: #303048; }}
  .card-icon {{ font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets'; font-size: 20px; color: #10B981; }}
  .card-info {{ flex: 1; min-width: 0; }}
  .card-title {{ font-size: 14px; font-weight: 500; color: #F8FAFC; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }}
  .card-status {{ font-size: 12px; color: #64748B; margin-top: 2px; }}
  .btn-action {{ background: #202030; color: #E2E8F0; border: 1px solid #303048; padding: 6px 12px; border-radius: 6px; cursor: pointer; font-size: 12px; transition: all 0.12s ease; }}
  .btn-action:hover {{ background: #303048; color: #FFFFFF; }}
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
  </div>
  <div class='download-list'>
    {itemsHtml}
  </div>
</div>
<script>
  function openFile(path) {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'open_download_file', path: path }}));
  }}
  function showInFolder(path) {{
    window.chrome.webview.postMessage(JSON.stringify({{ action: 'show_download_in_folder', path: path }}));
  }}
</script>
</body>
</html>";
        }
    }
}
