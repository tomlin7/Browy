using System;

namespace Browy.Services
{
    public static class NewTabPageService
    {
        public static string GetNewTabHtml()
        {
            var settings = BrowserSettings.Current;
            string engineName = settings.SearchEngineName;
            string engineUrl = settings.SearchEngineUrl;
            string accent = settings.AccentColorHex;

            return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>New Tab - Browy</title>
<style>
  :root {
    --bg-main: #0c0d0e;
    --text-primary: #fafafa;
    --text-secondary: #a1a1aa;
    --text-muted: #71717a;
    --card-bg: rgba(255, 255, 255, 0.025);
    --card-hover: rgba(255, 255, 255, 0.05);
    --border-color: rgba(255, 255, 255, 0.08);
    --border-hover: rgba(255, 255, 255, 0.16);
    --accent: {{accent}};
    --font-sans: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    --font-mono: 'Cascadia Code', 'Consolas', monospace;
  }

  * {
    box-sizing: border-box;
    margin: 0;
    padding: 0;
    user-select: none;
  }

  body {
    background-color: var(--bg-main);
    color: var(--text-primary);
    font-family: var(--font-sans);
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: space-between;
    padding: 32px 24px 24px 24px;
    overflow-x: hidden;
  }

  /* Top Right Status */
  .top-bar {
    width: 100%;
    max-width: 900px;
    display: flex;
    justify-content: flex-end;
    align-items: center;
    gap: 12px;
  }

  .top-btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 5px 12px;
    background: transparent;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    color: var(--text-secondary);
    font-size: 12px;
    font-family: var(--font-mono);
    cursor: pointer;
    transition: background-color 0.15s ease, border-color 0.15s ease, color 0.15s ease;
  }

  .top-btn:hover {
    background: var(--card-hover);
    color: var(--text-primary);
    border-color: var(--border-hover);
  }

  .top-btn svg {
    width: 13px;
    height: 13px;
    stroke: currentColor;
    stroke-width: 2;
    fill: none;
  }

  /* Center Container */
  .center-container {
    width: 100%;
    max-width: 680px;
    display: flex;
    flex-direction: column;
    align-items: center;
    margin-top: auto;
    margin-bottom: auto;
  }

  /* Clock & Greeting */
  .clock {
    font-size: 56px;
    font-weight: 400;
    font-family: var(--font-mono);
    letter-spacing: -1.5px;
    color: var(--text-primary);
    margin-bottom: 2px;
  }

  .greeting {
    font-size: 13px;
    color: var(--text-muted);
    font-family: var(--font-mono);
    letter-spacing: 0.05em;
    text-transform: uppercase;
    margin-bottom: 32px;
  }

  /* Search Box */
  .search-form {
    width: 100%;
    max-width: 580px;
    position: relative;
    margin-bottom: 40px;
  }

  .search-box-wrap {
    display: flex;
    align-items: center;
    background: rgba(255, 255, 255, 0.02);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 0 14px;
    height: 44px;
    transition: border-color 0.15s ease, background-color 0.15s ease;
  }

  .search-box-wrap:hover,
  .search-box-wrap:focus-within {
    border-color: var(--border-hover);
    background: rgba(255, 255, 255, 0.035);
  }

  .search-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--text-muted);
    margin-right: 10px;
  }

  .search-icon svg {
    width: 15px;
    height: 15px;
    stroke: currentColor;
    stroke-width: 2;
    fill: none;
  }

  .search-input {
    flex: 1;
    background: transparent;
    border: none;
    color: var(--text-primary);
    font-size: 14px;
    font-family: inherit;
    outline: none;
    user-select: text;
  }

  .search-input::placeholder {
    color: var(--text-muted);
    font-size: 13px;
  }

  .engine-select {
    background: transparent;
    border: 1px solid var(--border-color);
    border-radius: 4px;
    color: var(--text-secondary);
    font-size: 11px;
    font-family: var(--font-mono);
    padding: 3px 6px;
    cursor: pointer;
    outline: none;
    transition: border-color 0.15s ease, color 0.15s ease;
  }

  .engine-select:hover {
    border-color: var(--border-hover);
    color: var(--text-primary);
  }

  .engine-select option {
    background: #18181b;
    color: #fafafa;
  }

  /* Speed Dial Grid */
  .speed-dial-grid {
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 10px;
    width: 100%;
    margin-bottom: 24px;
  }

  @media (max-width: 680px) {
    .speed-dial-grid {
      grid-template-columns: repeat(3, 1fr);
    }
  }

  .speed-tile {
    position: relative;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    background: var(--card-bg);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 14px 8px 10px 8px;
    cursor: pointer;
    transition: background-color 0.15s ease, border-color 0.15s ease;
    text-decoration: none;
  }

  .speed-tile:hover {
    background: var(--card-hover);
    border-color: var(--border-hover);
  }

  .tile-icon {
    width: 34px;
    height: 34px;
    border-radius: 6px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 13px;
    font-family: var(--font-mono);
    font-weight: 600;
    margin-bottom: 8px;
    border: 1px solid rgba(255, 255, 255, 0.08);
    background: rgba(255, 255, 255, 0.04);
    color: var(--text-primary);
  }

  .tile-title {
    font-size: 11px;
    font-family: var(--font-mono);
    color: var(--text-secondary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 80px;
  }

  .speed-tile:hover .tile-title {
    color: var(--text-primary);
  }

  .tile-remove {
    position: absolute;
    top: 4px;
    right: 4px;
    width: 16px;
    height: 16px;
    border-radius: 3px;
    background: transparent;
    border: none;
    color: var(--text-muted);
    font-size: 11px;
    cursor: pointer;
    display: none;
    align-items: center;
    justify-content: center;
    line-height: 1;
  }

  .speed-tile:hover .tile-remove {
    display: flex;
  }

  .tile-remove:hover {
    color: #ef4444;
    background: rgba(239, 68, 68, 0.1);
  }

  .add-tile {
    border-style: dashed;
    background: transparent;
  }

  .add-tile .tile-icon {
    border: 1px dashed var(--border-color);
    background: transparent;
    color: var(--text-muted);
    font-size: 15px;
    font-weight: 300;
  }

  .add-tile:hover .tile-icon {
    color: var(--text-primary);
    border-color: var(--border-hover);
  }

  /* Footer Keyboard Hints */
  .footer-hints {
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 16px;
    font-family: var(--font-mono);
    font-size: 11px;
    color: var(--text-muted);
  }

  .hint-pill {
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }

  .kbd {
    display: inline-block;
    padding: 1.5px 5px;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid var(--border-color);
    border-radius: 4px;
    font-size: 10px;
    color: var(--text-secondary);
  }

  /* Modal */
  .modal-overlay {
    position: fixed;
    top: 0; left: 0; right: 0; bottom: 0;
    background: rgba(0, 0, 0, 0.6);
    backdrop-filter: blur(4px);
    display: none;
    align-items: center;
    justify-content: center;
    z-index: 1000;
  }

  .modal-overlay.active {
    display: flex;
  }

  .modal-box {
    background: #141517;
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 24px;
    width: 90%;
    max-width: 380px;
  }

  .modal-title {
    font-size: 14px;
    font-weight: 600;
    margin-bottom: 16px;
    color: var(--text-primary);
  }

  .modal-input {
    width: 100%;
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 8px 12px;
    color: var(--text-primary);
    font-size: 13px;
    font-family: inherit;
    outline: none;
    margin-bottom: 12px;
  }

  .modal-input:focus {
    border-color: var(--border-hover);
  }

  .modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    margin-top: 16px;
  }

  .btn {
    padding: 6px 12px;
    border-radius: 6px;
    font-size: 12px;
    font-family: var(--font-mono);
    cursor: pointer;
    border: 1px solid transparent;
    transition: background-color 0.15s ease;
  }

  .btn-ghost {
    background: transparent;
    color: var(--text-secondary);
    border-color: var(--border-color);
  }

  .btn-ghost:hover {
    background: rgba(255, 255, 255, 0.05);
    color: var(--text-primary);
  }

  .btn-primary {
    background: #fafafa;
    color: #09090b;
    font-weight: 500;
  }

  .btn-primary:hover {
    background: #ffffff;
  }
</style>
</head>
<body>

  <!-- Top Right Bar -->
  <div class="top-bar">
    <button class="top-btn" onclick="navigate('browy://settings')">
      <svg viewBox="0 0 24 24"><path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"></path><circle cx="12" cy="12" r="3"></circle></svg>
      <span>SETTINGS</span>
    </button>
  </div>

  <!-- Center Hero -->
  <div class="center-container">
    <div class="clock" id="clock">12:00</div>
    <div class="greeting" id="greeting">BROWY</div>

    <!-- Search Box Form -->
    <form class="search-form" id="search-form" onsubmit="handleSearch(event)">
      <div class="search-box-wrap">
        <span class="search-icon">
          <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
        </span>
        <input type="text"
               class="search-input"
               id="search-input"
               placeholder="Search or enter web address..."
               autocomplete="off"
               autofocus>
        <select class="engine-select" id="engine-select" onchange="saveEnginePreference()">
          <option value="Google">Google</option>
          <option value="Bing">Bing</option>
          <option value="DuckDuckGo">DuckDuckGo</option>
          <option value="Brave">Brave</option>
          <option value="Ecosia">Ecosia</option>
        </select>
      </div>
    </form>

    <!-- Speed Dial Shortcuts Grid -->
    <div class="speed-dial-grid" id="speed-dial-grid">
      <!-- Populated via JS -->
    </div>
  </div>

  <!-- Footer Keyboard Hints -->
  <div class="footer-hints">
    <div class="hint-pill"><span class="kbd">Ctrl+K</span> Palette</div>
    <div class="hint-pill"><span class="kbd">Ctrl+T</span> New Tab</div>
    <div class="hint-pill"><span class="kbd">Ctrl+Shift+R</span> Reader</div>
    <div class="hint-pill"><span class="kbd">Ctrl+S</span> Sidebar</div>
  </div>

  <!-- Add Shortcut Modal -->
  <div class="modal-overlay" id="add-modal">
    <div class="modal-box">
      <div class="modal-title">Add Shortcut</div>
      <input type="text" class="modal-input" id="shortcut-name" placeholder="Name (e.g. GitHub)">
      <input type="text" class="modal-input" id="shortcut-url" placeholder="URL (e.g. https://github.com)">
      <div class="modal-actions">
        <button class="btn btn-ghost" onclick="closeAddModal()">Cancel</button>
        <button class="btn btn-primary" onclick="confirmAddShortcut()">Add</button>
      </div>
    </div>
  </div>

<script>
  function navigate(url) {
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage({ action: 'navigate', url: url });
    } else {
      window.location.href = url;
    }
  }

  // Live Clock & Greeting
  function updateTime() {
    const now = new Date();
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    document.getElementById('clock').textContent = `${hours}:${minutes}`;

    const options = { weekday: 'short', month: 'short', day: 'numeric' };
    const dateStr = now.toLocaleDateString(undefined, options);
    document.getElementById('greeting').textContent = `${dateStr}`;
  }
  updateTime();
  setInterval(updateTime, 1000);

  // Search Engines
  const searchEngines = {
    'Google': 'https://www.google.com/search?q=',
    'Bing': 'https://www.bing.com/search?q=',
    'DuckDuckGo': 'https://duckduckgo.com/?q=',
    'Brave': 'https://search.brave.com/search?q=',
    'Ecosia': 'https://www.ecosia.org/search?q='
  };

  function saveEnginePreference() {
    const sel = document.getElementById('engine-select').value;
    localStorage.setItem('browy_newtab_engine', sel);
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage({
        action: 'set_search_engine',
        name: sel,
        url: searchEngines[sel]
      });
    }
  }

  function handleSearch(e) {
    e.preventDefault();
    const query = document.getElementById('search-input').value.trim();
    if (!query) return;

    if (query.startsWith('http://') || query.startsWith('https://') || query.startsWith('browy://') || query.startsWith('file://')) {
      navigate(query);
      return;
    }

    if (!query.includes(' ') && (query.includes('.') || query === 'localhost')) {
      navigate('https://' + query);
      return;
    }

    const sel = document.getElementById('engine-select').value;
    const base = searchEngines[sel] || searchEngines['Google'];
    navigate(base + encodeURIComponent(query));
  }

  // Speed Dial Management (Clean monochrome text glyphs, zero emojis)
  const defaultShortcuts = [
    { name: 'Google', url: 'https://www.google.com', icon: 'G' },
    { name: 'YouTube', url: 'https://www.youtube.com', icon: 'YT' },
    { name: 'GitHub', url: 'https://github.com', icon: 'GH' },
    { name: 'Reddit', url: 'https://www.reddit.com', icon: 'RD' },
    { name: 'ChatGPT', url: 'https://chatgpt.com', icon: 'AI' },
    { name: 'Wikipedia', url: 'https://www.wikipedia.org', icon: 'W' },
    { name: 'X', url: 'https://x.com', icon: 'X' },
    { name: 'Hacker News', url: 'https://news.ycombinator.com', icon: 'HN' },
    { name: 'Settings', url: 'browy://settings', icon: 'SET' }
  ];

  function getShortcuts() {
    const saved = localStorage.getItem('browy_speed_dial');
    if (saved) {
      try { return JSON.parse(saved); } catch (e) {}
    }
    return defaultShortcuts;
  }

  function saveShortcuts(list) {
    localStorage.setItem('browy_speed_dial', JSON.stringify(list));
    renderShortcuts();
  }

  function renderShortcuts() {
    const grid = document.getElementById('speed-dial-grid');
    grid.innerHTML = '';
    const list = getShortcuts();

    list.forEach((s, idx) => {
      const tile = document.createElement('div');
      tile.className = 'speed-tile';
      tile.onclick = (e) => {
        if (e.target.classList.contains('tile-remove')) return;
        navigate(s.url);
      };

      const displayIcon = s.icon || s.name.substring(0, 2).toUpperCase();

      tile.innerHTML = `
        <div class="tile-icon">${displayIcon}</div>
        <div class="tile-title" title="${s.name}">${s.name}</div>
        <button class="tile-remove" onclick="removeShortcut(${idx})" title="Remove shortcut">&times;</button>
      `;
      grid.appendChild(tile);
    });

    // Add Tile button
    const addTile = document.createElement('div');
    addTile.className = 'speed-tile add-tile';
    addTile.onclick = openAddModal;
    addTile.innerHTML = `
      <div class="tile-icon">+</div>
      <div class="tile-title">Add</div>
    `;
    grid.appendChild(addTile);
  }

  function removeShortcut(idx) {
    const list = getShortcuts();
    list.splice(idx, 1);
    saveShortcuts(list);
  }

  function openAddModal() {
    document.getElementById('shortcut-name').value = '';
    document.getElementById('shortcut-url').value = '';
    document.getElementById('add-modal').classList.add('active');
    document.getElementById('shortcut-name').focus();
  }

  function closeAddModal() {
    document.getElementById('add-modal').classList.remove('active');
  }

  function confirmAddShortcut() {
    const name = document.getElementById('shortcut-name').value.trim();
    let url = document.getElementById('shortcut-url').value.trim();
    if (!name || !url) return;

    if (!url.startsWith('http://') && !url.startsWith('https://') && !url.startsWith('browy://')) {
      url = 'https://' + url;
    }

    const list = getShortcuts();
    list.push({
      name: name,
      url: url,
      icon: name.substring(0, 2).toUpperCase()
    });

    saveShortcuts(list);
    closeAddModal();
  }

  // Restore engine
  (() => {
    const savedEngine = localStorage.getItem('browy_newtab_engine') || '{{engineName}}';
    const sel = document.getElementById('engine-select');
    if (sel) {
      for (let i = 0; i < sel.options.length; i++) {
        if (sel.options[i].value === savedEngine) {
          sel.selectedIndex = i;
          break;
        }
      }
    }
    renderShortcuts();
  })();
</script>
</body>
</html>
""";
        }
    }
}
