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
    --bg-main: #0b0c0e;
    --bg-card: #141619;
    --bg-card-hover: #181b20;
    --border-color: rgba(255, 255, 255, 0.07);
    --border-subtle: rgba(255, 255, 255, 0.05);
    --border-hover: rgba(255, 255, 255, 0.16);
    --text-primary: #f4f4f5;
    --text-secondary: #a1a1aa;
    --text-muted: #71717a;
    --accent: {{accent}};
    --font-sans: 'Segoe UI Variable Text', 'Segoe UI Variable Display', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    --font-mono: 'Cascadia Code', Consolas, 'SF Mono', monospace;
  }

  * {
    box-sizing: border-box;
    margin: 0;
    padding: 0;
    user-select: none;
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
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
    padding: 24px 24px 20px 24px;
    overflow-x: hidden;
  }

  /* Top Bar */
  .top-bar {
    width: 100%;
    max-width: 960px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 12px;
  }

  .top-brand {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    cursor: pointer;
    -webkit-tap-highlight-color: transparent;
    opacity: 0.7;
    transition-property: opacity, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  .top-brand:not(:disabled):active {
    transform: scale(0.96);
  }

  @media (hover: hover) {
    .top-brand:hover {
      opacity: 1;
    }
  }

  .brand-icon {
    width: 18px;
    height: 18px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--text-primary);
  }

  .brand-icon svg {
    width: 15px;
    height: 15px;
    fill: currentColor;
  }

  .brand-name {
    font-size: 11px;
    font-family: var(--font-mono);
    font-weight: 600;
    letter-spacing: 0.12em;
    color: var(--text-primary);
  }

  .top-actions {
    display: inline-flex;
    align-items: center;
    gap: 8px;
  }

  .top-btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 5px 11px;
    background: transparent;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    color: var(--text-secondary);
    font-size: 11px;
    font-family: var(--font-mono);
    letter-spacing: 0.04em;
    cursor: pointer;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  .top-btn:not(:disabled):active {
    transform: scale(0.96);
  }

  @media (hover: hover) {
    .top-btn:hover {
      background: rgba(255, 255, 255, 0.04);
      color: var(--text-primary);
      border-color: var(--border-hover);
    }
  }

  .top-btn svg {
    width: 13px;
    height: 13px;
    stroke: currentColor;
    stroke-width: 1.75;
    fill: none;
  }

  /* Center Container */
  .center-container {
    width: 100%;
    max-width: 600px;
    display: flex;
    flex-direction: column;
    align-items: center;
    margin-top: auto;
    margin-bottom: auto;
  }

  /* Clock & Greeting */
  .greeting {
    font-size: 11.5px;
    color: var(--text-muted);
    font-family: var(--font-mono);
    letter-spacing: 0.12em;
    text-transform: uppercase;
    margin-bottom: 4px;
  }

  .clock {
    font-size: 64px;
    font-weight: 300;
    font-family: var(--font-sans);
    font-variant-numeric: tabular-nums;
    letter-spacing: -2.5px;
    color: var(--text-primary);
    line-height: 1;
    margin-bottom: 28px;
  }

  /* Search Box */
  .search-form {
    width: 100%;
    max-width: 580px;
    position: relative;
    margin-bottom: 32px;
  }

  .search-box-wrap {
    display: flex;
    align-items: center;
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 0 10px 0 14px;
    height: 44px;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.02);
    transition-property: border-color, background-color, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  @media (hover: hover) {
    .search-box-wrap:hover {
      border-color: var(--border-hover);
    }
  }

  .search-box-wrap:focus-within {
    border-color: rgba(255, 255, 255, 0.24);
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.4), inset 0 1px 0 rgba(255, 255, 255, 0.04);
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
    stroke-width: 1.75;
    fill: none;
  }

  .search-input {
    flex: 1;
    background: transparent;
    border: none;
    color: var(--text-primary);
    font-size: 13.5px;
    font-family: inherit;
    outline: none;
    user-select: text;
  }

  .search-input::placeholder {
    color: var(--text-muted);
    font-size: 13px;
  }

  .engine-select {
    background: #101215;
    border: 1px solid var(--border-color);
    border-radius: 5px;
    color: var(--text-secondary);
    font-size: 11px;
    font-family: var(--font-mono);
    padding: 3px 8px;
    cursor: pointer;
    outline: none;
    -webkit-tap-highlight-color: transparent;
    transition-property: border-color, color, background-color;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  @media (hover: hover) {
    .engine-select:hover {
      border-color: var(--border-hover);
      color: var(--text-primary);
    }
  }

  .engine-select:focus {
    border-color: rgba(255, 255, 255, 0.24);
    color: var(--text-primary);
  }

  .engine-select option {
    background: #141619;
    color: #f4f4f5;
  }

  /* Speed Dial Grid */
  .speed-dial-grid {
    display: grid;
    grid-template-columns: repeat(5, minmax(0, 1fr));
    gap: 10px;
    width: 100%;
    max-width: 580px;
    margin-bottom: 24px;
  }

  @media (max-width: 640px) {
    .speed-dial-grid {
      grid-template-columns: repeat(3, minmax(0, 1fr));
    }
  }

  .speed-tile {
    position: relative;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 14px 8px 10px 8px;
    cursor: pointer;
    -webkit-tap-highlight-color: transparent;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.02);
    transition-property: background-color, border-color, transform, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
    text-decoration: none;
  }

  .speed-tile:not(:disabled):active {
    transform: scale(0.96);
  }

  @media (hover: hover) {
    .speed-tile:hover {
      background: var(--bg-card-hover);
      border-color: var(--border-hover);
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.35);
    }
  }

  .tile-icon {
    width: 36px;
    height: 36px;
    border-radius: 6px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    font-family: var(--font-mono);
    font-weight: 600;
    margin-bottom: 8px;
    border: 1px solid var(--border-color);
    background: rgba(255, 255, 255, 0.04);
    color: var(--text-primary);
    letter-spacing: 0.02em;
  }

  .tile-title {
    font-size: 11px;
    font-family: var(--font-sans);
    color: var(--text-secondary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 80px;
    transition-property: color;
    transition-duration: 120ms;
  }

  .tile-remove {
    position: absolute;
    top: 4px;
    right: 4px;
    width: 18px;
    height: 18px;
    border-radius: 4px;
    background: transparent;
    border: none;
    color: var(--text-muted);
    cursor: pointer;
    display: none;
    align-items: center;
    justify-content: center;
    -webkit-tap-highlight-color: transparent;
    transition-property: color, background-color;
    transition-duration: 120ms;
  }

  .tile-remove svg {
    width: 11px;
    height: 11px;
    stroke: currentColor;
    stroke-width: 2;
    fill: none;
  }

  @media (hover: hover) {
    .speed-tile:hover .tile-title {
      color: var(--text-primary);
    }

    .speed-tile:hover .tile-remove {
      display: flex;
    }

    .tile-remove:hover {
      color: #ef4444;
      background: rgba(239, 68, 68, 0.12);
    }
  }

  .add-tile {
    border-style: dashed;
    background: transparent;
    box-shadow: none;
  }

  .add-tile .tile-icon {
    border: 1px dashed var(--border-color);
    background: transparent;
    color: var(--text-muted);
  }

  .add-tile .tile-icon svg {
    width: 14px;
    height: 14px;
    stroke: currentColor;
    stroke-width: 1.75;
    fill: none;
  }

  @media (hover: hover) {
    .add-tile:hover {
      border-color: var(--border-hover);
      background: rgba(255, 255, 255, 0.02);
    }
    .add-tile:hover .tile-icon {
      color: var(--text-primary);
      border-color: var(--border-hover);
    }
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
    padding: 2px 6px;
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
    background: rgba(0, 0, 0, 0.65);
    backdrop-filter: blur(8px);
    display: none;
    align-items: center;
    justify-content: center;
    z-index: 1000;
    overscroll-behavior: contain;
  }

  .modal-overlay.active {
    display: flex;
  }

  .modal-box {
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 22px 24px;
    width: 90%;
    max-width: 380px;
    box-shadow: 0 24px 48px rgba(0, 0, 0, 0.6), inset 0 1px 0 rgba(255, 255, 255, 0.03);
  }

  .modal-title {
    font-size: 14px;
    font-weight: 600;
    margin-bottom: 16px;
    color: var(--text-primary);
    letter-spacing: -0.01em;
  }

  .modal-input {
    width: 100%;
    background: #101215;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    padding: 8px 12px;
    color: var(--text-primary);
    font-size: 12.5px;
    font-family: var(--font-mono);
    outline: none;
    margin-bottom: 12px;
    transition-property: border-color, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  .modal-input:focus {
    border-color: rgba(255, 255, 255, 0.28);
    box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.15);
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
    font-size: 12px;
    font-family: var(--font-sans);
    cursor: pointer;
    border: 1px solid transparent;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  .btn:not(:disabled):active {
    transform: scale(0.96);
  }

  .btn-ghost {
    background: transparent;
    color: var(--text-secondary);
    border-color: var(--border-color);
  }

  @media (hover: hover) {
    .btn-ghost:hover {
      background: rgba(255, 255, 255, 0.05);
      color: var(--text-primary);
      border-color: var(--border-hover);
    }
  }

  .btn-primary {
    background: #f4f4f5;
    color: #0b0c0e;
    font-weight: 500;
  }

  @media (hover: hover) {
    .btn-primary:hover {
      background: #ffffff;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
    }
  }

  /* Reduced Motion Guard */
  @media (prefers-reduced-motion: reduce) {
    *, *::before, *::after {
      animation-duration: 0.001ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.001ms !important;
      transform: none !important;
    }
  }
</style>
</head>
<body>

  <!-- Top Bar -->
  <div class="top-bar">
    <div class="top-brand" onclick="navigate('browy://newtab')" title="Browy New Tab">
      <span class="brand-icon">
        <svg viewBox="0 0 24 24"><path d="M4 4h9a5 5 0 0 1 5 5 5 5 0 0 1-5 5H4V4zm0 10h10a5 5 0 0 1 5 5 5 5 0 0 1-5 5H4v-10z"></path></svg>
      </span>
      <span class="brand-name">BROWY</span>
    </div>
    <div class="top-actions">
      <button class="top-btn" onclick="navigate('browy://history')" title="Browsing History (Ctrl+H)">
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
        <span>HISTORY</span>
      </button>
      <button class="top-btn" onclick="navigate('browy://bookmarks')" title="Bookmarks (Ctrl+Shift+O)">
        <svg viewBox="0 0 24 24"><path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"></path></svg>
        <span>BOOKMARKS</span>
      </button>
      <button class="top-btn" onclick="navigate('browy://settings')" title="Settings (Ctrl+,)">
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="3"></circle><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path></svg>
        <span>SETTINGS</span>
      </button>
    </div>
  </div>

  <!-- Center Hero -->
  <div class="center-container">
    <div class="greeting" id="greeting">&nbsp;</div>
    <div class="clock" id="clock">12:00</div>

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
    <div class="hint-pill"><span class="kbd">Ctrl+S</span> Sidebar</div>
    <div class="hint-pill"><span class="kbd">Ctrl+Shift+R</span> Reader</div>
  </div>

  <!-- Add Shortcut Modal -->
  <div class="modal-overlay" id="add-modal" onclick="if (event.target === this) closeAddModal()">
    <div class="modal-box">
      <div class="modal-title">Add Shortcut</div>
      <input type="text" class="modal-input" id="shortcut-name" placeholder="Name (e.g. GitHub)" onkeydown="if (event.key === 'Enter') confirmAddShortcut()">
      <input type="text" class="modal-input" id="shortcut-url" placeholder="URL (e.g. https://github.com)" onkeydown="if (event.key === 'Enter') confirmAddShortcut()">
      <div class="modal-actions">
        <button class="btn btn-ghost" onclick="closeAddModal()">Cancel</button>
        <button class="btn btn-primary" onclick="confirmAddShortcut()">Add</button>
      </div>
    </div>
  </div>

<script>
  function navigate(url) {
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage(JSON.stringify({ action: 'navigate', url: url }));
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
      window.chrome.webview.postMessage(JSON.stringify({
        action: 'set_search_engine',
        name: sel,
        url: searchEngines[sel]
      }));
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
        if (e.target.closest('.tile-remove')) return;
        navigate(s.url);
      };

      const displayIcon = s.icon || s.name.substring(0, 2).toUpperCase();

      tile.innerHTML = `
        <div class="tile-icon">${displayIcon}</div>
        <div class="tile-title" title="${s.name}">${s.name}</div>
        <button class="tile-remove" onclick="removeShortcut(event, ${idx})" title="Remove shortcut">
          <svg viewBox="0 0 24 24"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
        </button>
      `;
      grid.appendChild(tile);
    });

    // Add Tile button
    const addTile = document.createElement('div');
    addTile.className = 'speed-tile add-tile';
    addTile.onclick = openAddModal;
    addTile.innerHTML = `
      <div class="tile-icon">
        <svg viewBox="0 0 24 24"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
      </div>
      <div class="tile-title">Add</div>
    `;
    grid.appendChild(addTile);
  }

  function removeShortcut(e, idx) {
    if (e) {
      e.preventDefault();
      e.stopPropagation();
    }
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

  // Close modal on Escape
  window.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') closeAddModal();
  });

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
