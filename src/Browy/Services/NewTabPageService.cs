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

            return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>New Tab - Browy</title>
<style>
  :root {
    --bg-main: #0c0c10;
    --text-primary: #F8FAFC;
    --text-secondary: #94A3B8;
    --text-muted: #64748B;
    --card-bg: rgba(255, 255, 255, 0.04);
    --card-hover: rgba(255, 255, 255, 0.08);
    --border-color: rgba(255, 255, 255, 0.08);
    --border-hover: rgba(59, 130, 246, 0.5);
    --accent: #3B82F6;
    --accent-hover: #2563EB;
    --accent-glow: rgba(59, 130, 246, 0.25);
    --font-family: 'Segoe UI Variable Text', 'Segoe UI', system-ui, -apple-system, sans-serif;
  }

  * {
    box-sizing: border-box;
    margin: 0;
    padding: 0;
    user-select: none;
  }

  body {
    background: radial-gradient(circle at 50% 20%, rgba(30, 41, 59, 0.4) 0%, var(--bg-main) 70%);
    background-color: var(--bg-main);
    color: var(--text-primary);
    font-family: var(--font-family);
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: space-between;
    padding: 40px 24px 24px 24px;
    overflow-x: hidden;
  }

  /* Top Right Status */
  .top-bar {
    width: 100%;
    max-width: 960px;
    display: flex;
    justify-content: flex-end;
    align-items: center;
    gap: 12px;
  }

  .top-btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 6px 14px;
    background: var(--card-bg);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    color: var(--text-secondary);
    font-size: 12.5px;
    cursor: pointer;
    transition: all 0.15s ease;
  }

  .top-btn:hover {
    background: var(--card-hover);
    color: var(--text-primary);
    border-color: rgba(255, 255, 255, 0.15);
  }

  /* Center Container */
  .center-container {
    width: 100%;
    max-width: 760px;
    display: flex;
    flex-direction: column;
    align-items: center;
    margin-top: auto;
    margin-bottom: auto;
  }

  /* Clock & Greeting */
  .clock {
    font-size: 64px;
    font-weight: 300;
    letter-spacing: -1.5px;
    color: #FFFFFF;
    margin-bottom: 4px;
    text-shadow: 0 4px 20px rgba(0, 0, 0, 0.4);
  }

  .greeting {
    font-size: 18px;
    color: var(--text-secondary);
    font-weight: 400;
    margin-bottom: 28px;
  }

  /* Search Box */
  .search-form {
    width: 100%;
    max-width: 620px;
    position: relative;
    margin-bottom: 36px;
  }

  .search-box-wrap {
    display: flex;
    align-items: center;
    background: rgba(26, 26, 36, 0.75);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border: 1px solid var(--border-color);
    border-radius: 14px;
    padding: 0 16px;
    height: 52px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.35);
    transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  }

  .search-box-wrap:hover,
  .search-box-wrap:focus-within {
    border-color: var(--accent);
    box-shadow: 0 8px 36px var(--accent-glow), 0 0 0 1px var(--accent);
    background: rgba(30, 30, 42, 0.9);
  }

  .search-icon {
    font-size: 16px;
    color: var(--text-muted);
    margin-right: 12px;
  }

  .search-input {
    flex: 1;
    background: transparent;
    border: none;
    color: var(--text-primary);
    font-size: 15px;
    font-family: inherit;
    outline: none;
    user-select: text;
  }

  .search-input::placeholder {
    color: var(--text-muted);
  }

  .engine-select {
    background: rgba(255, 255, 255, 0.06);
    border: 1px solid var(--border-color);
    border-radius: 6px;
    color: var(--text-secondary);
    font-size: 11.5px;
    padding: 4px 8px;
    cursor: pointer;
    outline: none;
    transition: all 0.15s ease;
  }

  .engine-select:hover {
    background: rgba(255, 255, 255, 0.1);
    color: var(--text-primary);
  }

  /* Speed Dial Grid */
  .speed-dial-grid {
    display: grid;
    grid-template-columns: repeat(5, 1fr);
    gap: 14px;
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
    border-radius: 12px;
    padding: 16px 10px 12px 10px;
    cursor: pointer;
    transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
    text-decoration: none;
  }

  .speed-tile:hover {
    background: var(--card-hover);
    border-color: rgba(255, 255, 255, 0.2);
    transform: translateY(-3px);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.25);
  }

  .tile-icon {
    width: 44px;
    height: 44px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 18px;
    font-weight: 700;
    margin-bottom: 8px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
  }

  .tile-title {
    font-size: 12px;
    font-weight: 500;
    color: var(--text-secondary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 90px;
  }

  .speed-tile:hover .tile-title {
    color: var(--text-primary);
  }

  .tile-remove {
    position: absolute;
    top: 4px;
    right: 4px;
    width: 18px;
    height: 18px;
    border-radius: 50%;
    background: rgba(0, 0, 0, 0.5);
    border: none;
    color: var(--text-muted);
    font-size: 11px;
    line-height: 18px;
    text-align: center;
    cursor: pointer;
    opacity: 0;
    transition: opacity 0.15s ease, color 0.15s ease, background 0.15s ease;
  }

  .speed-tile:hover .tile-remove {
    opacity: 1;
  }

  .tile-remove:hover {
    background: #EF4444;
    color: #FFFFFF;
  }

  /* Add Tile Button */
  .add-tile {
    border-style: dashed;
    border-color: rgba(255, 255, 255, 0.12);
  }

  .add-tile:hover {
    border-color: var(--accent);
  }

  .add-tile .tile-icon {
    background: rgba(255, 255, 255, 0.05);
    color: var(--text-muted);
  }

  .add-tile:hover .tile-icon {
    color: var(--accent);
    background: rgba(59, 130, 246, 0.1);
  }

  /* Bottom Hints */
  .footer-hints {
    display: flex;
    align-items: center;
    gap: 16px;
    font-size: 11.5px;
    color: var(--text-muted);
  }

  .hint-pill {
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }

  .kbd {
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid rgba(255, 255, 255, 0.12);
    border-radius: 4px;
    padding: 2px 6px;
    font-size: 10.5px;
    font-family: monospace;
    color: var(--text-secondary);
  }

  /* Modal Dialog for Adding Shortcut */
  .modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.7);
    backdrop-filter: blur(8px);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
    opacity: 0;
    pointer-events: none;
    transition: opacity 0.2s ease;
  }

  .modal-overlay.active {
    opacity: 1;
    pointer-events: auto;
  }

  .modal-box {
    width: 360px;
    background: #161622;
    border: 1px solid var(--border-color);
    border-radius: 12px;
    padding: 20px;
    box-shadow: 0 16px 40px rgba(0, 0, 0, 0.5);
  }

  .modal-title {
    font-size: 15px;
    font-weight: 600;
    margin-bottom: 14px;
  }

  .modal-input {
    width: 100%;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 8px 12px;
    color: var(--text-primary);
    font-size: 13px;
    margin-bottom: 12px;
    outline: none;
    font-family: inherit;
    user-select: text;
  }

  .modal-input:focus {
    border-color: var(--accent);
  }

  .modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    margin-top: 16px;
  }

  .btn {
    padding: 6px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    cursor: pointer;
    border: 1px solid transparent;
  }

  .btn-ghost {
    background: transparent;
    color: var(--text-secondary);
  }

  .btn-ghost:hover {
    background: rgba(255, 255, 255, 0.05);
    color: var(--text-primary);
  }

  .btn-primary {
    background: var(--accent);
    color: #FFFFFF;
  }

  .btn-primary:hover {
    background: var(--accent-hover);
  }
</style>
</head>
<body>

  <!-- Top Right Bar -->
  <div class="top-bar">
    <button class="top-btn" onclick="navigate('browy://settings')">
      <span>&#x2699;</span> Settings
    </button>
  </div>

  <!-- Center Hero -->
  <div class="center-container">
    <div class="clock" id="clock">12:00</div>
    <div class="greeting" id="greeting">Good day</div>

    <!-- Search Box Form -->
    <form class="search-form" id="search-form" onsubmit="handleSearch(event)">
      <div class="search-box-wrap">
        <span class="search-icon">&#x1F50D;</span>
        <input type="text"
               class="search-input"
               id="search-input"
               placeholder="Search the web or type a URL..."
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
    <div class="hint-pill"><span class="kbd">Ctrl+K</span> Command Palette</div>
    <div class="hint-pill"><span class="kbd">Ctrl+T</span> New Tab</div>
    <div class="hint-pill"><span class="kbd">Ctrl+Shift+R</span> Reader Mode</div>
    <div class="hint-pill"><span class="kbd">Ctrl+S</span> Toggle Tabs</div>
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
    const hours = now.getHours();
    const minutes = String(now.getMinutes()).padStart(2, '0');
    document.getElementById('clock').textContent = `${hours}:${minutes}`;

    let greet = 'Good evening';
    if (hours >= 5 && hours < 12) greet = 'Good morning';
    else if (hours >= 12 && hours < 17) greet = 'Good afternoon';
    else if (hours >= 17 && hours < 22) greet = 'Good evening';
    else greet = 'Good night';

    const options = { weekday: 'long', month: 'short', day: 'numeric' };
    const dateStr = now.toLocaleDateString(undefined, options);
    document.getElementById('greeting').textContent = `${greet} • ${dateStr}`;
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

    // Direct URL check
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

  // Speed Dial Management
  const defaultShortcuts = [
    { name: 'Google', url: 'https://www.google.com', bg: '#4285F4', color: '#FFF', icon: 'G' },
    { name: 'YouTube', url: 'https://www.youtube.com', bg: '#EF4444', color: '#FFF', icon: '▶' },
    { name: 'GitHub', url: 'https://github.com', bg: '#24292F', color: '#FFF', icon: '⌥' },
    { name: 'Reddit', url: 'https://www.reddit.com', bg: '#FF4500', color: '#FFF', icon: '●' },
    { name: 'ChatGPT', url: 'https://chatgpt.com', bg: '#10A37F', color: '#FFF', icon: 'AI' },
    { name: 'Wikipedia', url: 'https://www.wikipedia.org', bg: '#334155', color: '#FFF', icon: 'W' },
    { name: 'X / Twitter', url: 'https://x.com', bg: '#000000', color: '#FFF', icon: '𝕏' },
    { name: 'Hacker News', url: 'https://news.ycombinator.com', bg: '#FF6600', color: '#FFF', icon: 'Y' },
    { name: 'Settings', url: 'browy://settings', bg: '#3B82F6', color: '#FFF', icon: '⚙' }
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

      tile.innerHTML = `
        <div class="tile-icon" style="background:${s.bg || '#334155'}; color:${s.color || '#FFF'}">${s.icon || s.name[0]}</div>
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
      <div class="tile-title">Add Shortcut</div>
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
    const bgColors = ['#3B82F6', '#8B5CF6', '#EC4899', '#10B981', '#F59E0B', '#6366F1'];
    const randomBg = bgColors[Math.floor(Math.random() * bgColors.length)];

    list.push({
      name: name,
      url: url,
      bg: randomBg,
      color: '#FFFFFF',
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
