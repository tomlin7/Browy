using System;

namespace Browy.Services
{
    public static class SettingsPageService
    {
        public static string GetSettingsHtml()
        {
            var settings = BrowserSettings.Current;
            string currentEngine = settings.SearchEngineName;

            return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Settings - Browy</title>
<style>
  :root {
    --bg-main: #0c0c10;
    --bg-card: #16161d;
    --bg-card-hover: #1e1e27;
    --border-color: rgba(255, 255, 255, 0.08);
    --border-subtle: rgba(255, 255, 255, 0.04);
    --text-primary: #f8fafc;
    --text-secondary: #94a3b8;
    --text-muted: #64748b;
    --accent: #3b82f6;
    --accent-hover: #2563eb;
    --accent-glow: rgba(59, 130, 246, 0.25);
    --danger: #ef4444;
    --danger-hover: #dc2626;
    --font-family: 'Segoe UI Variable Text', 'Segoe UI', system-ui, -apple-system, sans-serif;
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
    font-family: var(--font-family);
    display: flex;
    height: 100vh;
    overflow: hidden;
  }

  /* Left Sidebar Navigation */
  .sidebar {
    width: 240px;
    background: rgba(18, 18, 24, 0.7);
    border-right: 1px solid var(--border-color);
    padding: 28px 14px;
    display: flex;
    flex-direction: column;
    gap: 6px;
    flex-shrink: 0;
  }

  .brand {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 0 12px 20px 12px;
    border-bottom: 1px solid var(--border-color);
    margin-bottom: 12px;
  }

  .brand-icon {
    width: 26px;
    height: 26px;
    border-radius: 7px;
    background: linear-gradient(135deg, #3b82f6, #8b5cf6);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 14px;
    font-weight: bold;
    color: white;
  }

  .brand-title {
    font-size: 15px;
    font-weight: 600;
    letter-spacing: -0.3px;
  }

  .nav-item {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 10px 14px;
    border-radius: 8px;
    color: var(--text-secondary);
    font-size: 13px;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.15s ease;
  }

  .nav-item:hover {
    background: rgba(255, 255, 255, 0.05);
    color: var(--text-primary);
  }

  .nav-item.active {
    background: rgba(59, 130, 246, 0.15);
    color: #60a5fa;
    font-weight: 600;
  }

  .nav-icon {
    font-size: 15px;
    width: 18px;
    text-align: center;
  }

  /* Content Area */
  .content {
    flex: 1;
    overflow-y: auto;
    padding: 36px 48px;
  }

  .content-section {
    display: none;
    max-width: 780px;
  }

  .content-section.active {
    display: block;
    animation: fadeIn 0.18s ease-out;
  }

  @keyframes fadeIn {
    from { opacity: 0; transform: translateY(6px); }
    to { opacity: 1; transform: translateY(0); }
  }

  .section-title {
    font-size: 24px;
    font-weight: 600;
    letter-spacing: -0.5px;
    margin-bottom: 6px;
  }

  .section-subtitle {
    font-size: 13px;
    color: var(--text-secondary);
    margin-bottom: 26px;
  }

  /* Settings Card & Groups */
  .settings-group {
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 12px;
    margin-bottom: 22px;
    overflow: hidden;
  }

  .settings-row {
    padding: 16px 20px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    border-bottom: 1px solid var(--border-subtle);
  }

  .settings-row:last-child {
    border-bottom: none;
  }

  .row-info {
    max-width: 65%;
  }

  .row-title {
    font-size: 13.5px;
    font-weight: 500;
    margin-bottom: 3px;
  }

  .row-desc {
    font-size: 12px;
    color: var(--text-secondary);
  }

  /* Select dropdown */
  select {
    background: rgba(255, 255, 255, 0.07);
    border: 1px solid var(--border-color);
    color: var(--text-primary);
    padding: 8px 14px;
    border-radius: 7px;
    font-size: 12.5px;
    font-family: inherit;
    outline: none;
    cursor: pointer;
  }

  select:focus {
    border-color: var(--accent);
  }

  /* Buttons */
  .btn {
    padding: 8px 16px;
    border-radius: 7px;
    font-size: 12.5px;
    font-weight: 500;
    font-family: inherit;
    cursor: pointer;
    border: 1px solid transparent;
    transition: all 0.15s ease;
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }

  .btn-primary {
    background: var(--accent);
    color: white;
  }

  .btn-primary:hover {
    background: var(--accent-hover);
  }

  .btn-secondary {
    background: rgba(255, 255, 255, 0.08);
    border-color: var(--border-color);
    color: var(--text-primary);
  }

  .btn-secondary:hover {
    background: rgba(255, 255, 255, 0.13);
  }

  .btn-danger {
    background: rgba(239, 68, 68, 0.15);
    border-color: rgba(239, 68, 68, 0.3);
    color: #fca5a5;
  }

  .btn-danger:hover {
    background: rgba(239, 68, 68, 0.25);
  }

  /* Toggle Switch */
  .toggle-switch {
    position: relative;
    width: 42px;
    height: 22px;
    display: inline-block;
  }

  .toggle-switch input {
    opacity: 0;
    width: 0;
    height: 0;
  }

  .toggle-slider {
    position: absolute;
    cursor: pointer;
    top: 0; left: 0; right: 0; bottom: 0;
    background-color: rgba(255, 255, 255, 0.15);
    border-radius: 22px;
    transition: 0.2s;
  }

  .toggle-slider:before {
    position: absolute;
    content: "";
    height: 16px;
    width: 16px;
    left: 3px;
    bottom: 3px;
    background-color: white;
    border-radius: 50%;
    transition: 0.2s;
  }

  input:checked + .toggle-slider {
    background-color: var(--accent);
  }

  input:checked + .toggle-slider:before {
    transform: translateX(20px);
  }

  /* Shortcuts Table */
  .shortcuts-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 12.5px;
  }

  .shortcuts-table th {
    text-align: left;
    padding: 12px 18px;
    font-size: 11px;
    color: var(--text-muted);
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    border-bottom: 1px solid var(--border-color);
  }

  .shortcuts-table td {
    padding: 12px 18px;
    border-bottom: 1px solid var(--border-subtle);
  }

  .kbd {
    display: inline-block;
    padding: 3px 8px;
    font-size: 11px;
    font-family: monospace;
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid var(--border-color);
    border-radius: 5px;
    color: #e2e8f0;
  }

  /* Toast Notification */
  .toast {
    position: fixed;
    bottom: 28px;
    right: 28px;
    background: #1e293b;
    border: 1px solid #3b82f6;
    color: #f8fafc;
    padding: 12px 20px;
    border-radius: 8px;
    box-shadow: 0 10px 25px rgba(0,0,0,0.5);
    font-size: 13px;
    display: none;
    align-items: center;
    gap: 8px;
    z-index: 999;
  }
</style>
</head>
<body>

  <!-- Navigation Sidebar -->
  <div class="sidebar">
    <div class="brand">
      <div class="brand-icon">B</div>
      <div class="brand-title">Settings</div>
    </div>
    <div class="nav-item active" onclick="switchSection('general', this)">
      <span class="nav-icon">&#x2699;</span> General
    </div>
    <div class="nav-item" onclick="switchSection('appearance', this)">
      <span class="nav-icon">&#x1F3A8;</span> Appearance
    </div>
    <div class="nav-item" onclick="switchSection('privacy', this)">
      <span class="nav-icon">&#x1F512;</span> Privacy & Security
    </div>
    <div class="nav-item" onclick="switchSection('downloads', this)">
      <span class="nav-icon">&#x2B07;</span> Downloads
    </div>
    <div class="nav-item" onclick="switchSection('shortcuts', this)">
      <span class="nav-icon">&#x2328;</span> Keyboard Shortcuts
    </div>
    <div class="nav-item" onclick="switchSection('about', this)">
      <span class="nav-icon">&#x2139;</span> About Browy
    </div>
  </div>

  <!-- Main Content Area -->
  <div class="content">

    <!-- GENERAL SECTION -->
    <div id="general" class="content-section active">
      <h1 class="section-title">General</h1>
      <p class="section-subtitle">Manage search engines, startup behavior, and default browsing experience.</p>

      <div class="settings-group">
        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Default Search Engine</div>
            <div class="row-desc">Used when queries are typed directly into the Omnibox.</div>
          </div>
          <select id="searchEngineSelect" onchange="onSearchEngineChange(this.value)">
            <option value="Google" {{(currentEngine == "Google" ? "selected" : "")}}>Google</option>
            <option value="DuckDuckGo" {{(currentEngine == "DuckDuckGo" ? "selected" : "")}}>DuckDuckGo</option>
            <option value="Bing" {{(currentEngine == "Bing" ? "selected" : "")}}>Bing</option>
            <option value="Brave" {{(currentEngine == "Brave" ? "selected" : "")}}>Brave Search</option>
          </select>
        </div>

        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">On Startup</div>
            <div class="row-desc">Open the Browy start page or continue with your favorite site.</div>
          </div>
          <select>
            <option selected>Open the Browy Start Page</option>
            <option>Restore previous session tabs</option>
          </select>
        </div>
      </div>
    </div>

    <!-- APPEARANCE SECTION -->
    <div id="appearance" class="content-section">
      <h1 class="section-title">Appearance</h1>
      <p class="section-subtitle">Customize the Windows 11 Fluent design, acrylic blur, and tab layouts.</p>

      <div class="settings-group">
        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Windows 11 Acrylic Backdrop</div>
            <div class="row-desc">Render native DWM desktop acrylic blur behind the browser window.</div>
          </div>
          <label class="toggle-switch">
            <input type="checkbox" checked disabled>
            <span class="toggle-slider"></span>
          </label>
        </div>

        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Theme Mode</div>
            <div class="row-desc">Browy is optimized with an immersive modern dark palette.</div>
          </div>
          <select>
            <option selected>Dark (Arc & Zen Fluent)</option>
          </select>
        </div>

        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Tab Bar Style</div>
            <div class="row-desc">Toggle between vertical sidebar tabs (Arc style) and horizontal strip.</div>
          </div>
          <button class="btn btn-secondary" onclick="postAction('toggle_sidebar')">Toggle Sidebar (Ctrl+S)</button>
        </div>
      </div>
    </div>

    <!-- PRIVACY & SECURITY SECTION -->
    <div id="privacy" class="content-section">
      <h1 class="section-title">Privacy & Security</h1>
      <p class="section-subtitle">Manage browsing history, cached assets, and website permissions.</p>

      <div class="settings-group">
        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Browsing History</div>
            <div class="row-desc">Erase all recorded pages from your local history drawer.</div>
          </div>
          <button class="btn btn-danger" onclick="postAction('clear_history')">Clear History</button>
        </div>

        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Cookies and Site Data</div>
            <div class="row-desc">Clear cached images, cookies, and local web storage.</div>
          </div>
          <button class="btn btn-secondary" onclick="postAction('clear_cookies')">Clear Cache & Cookies</button>
        </div>

        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Do Not Track</div>
            <div class="row-desc">Send automated "Do Not Track" request headers with browsing traffic.</div>
          </div>
          <label class="toggle-switch">
            <input type="checkbox" checked>
            <span class="toggle-slider"></span>
          </label>
        </div>
      </div>
    </div>

    <!-- DOWNLOADS SECTION -->
    <div id="downloads" class="content-section">
      <h1 class="section-title">Downloads</h1>
      <p class="section-subtitle">Manage downloaded files and storage folders.</p>

      <div class="settings-group">
        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Download Location</div>
            <div class="row-desc">Files are saved directly to your Windows User Downloads folder.</div>
          </div>
          <button class="btn btn-secondary" onclick="postAction('open_downloads_folder')">Open Downloads Folder</button>
        </div>
      </div>
    </div>

    <!-- SHORTCUTS CHEATSHEET -->
    <div id="shortcuts" class="content-section">
      <h1 class="section-title">Keyboard Shortcuts</h1>
      <p class="section-subtitle">Full quick-reference guide for navigating Browy efficiently.</p>

      <div class="settings-group">
        <table class="shortcuts-table">
          <thead>
            <tr>
              <th>Action</th>
              <th>Shortcut</th>
            </tr>
          </thead>
          <tbody>
            <tr><td>Open Command Palette</td><td><span class="kbd">Ctrl</span> + <span class="kbd">K</span></td></tr>
            <tr><td>Open Settings</td><td><span class="kbd">Ctrl</span> + <span class="kbd">,</span></td></tr>
            <tr><td>New Tab</td><td><span class="kbd">Ctrl</span> + <span class="kbd">T</span></td></tr>
            <tr><td>Close Active Tab</td><td><span class="kbd">Ctrl</span> + <span class="kbd">W</span></td></tr>
            <tr><td>Reopen Closed Tab</td><td><span class="kbd">Ctrl</span> + <span class="kbd">Shift</span> + <span class="kbd">T</span></td></tr>
            <tr><td>Focus Address Bar (Omnibox)</td><td><span class="kbd">Ctrl</span> + <span class="kbd">L</span></td></tr>
            <tr><td>Find in Page</td><td><span class="kbd">Ctrl</span> + <span class="kbd">F</span></td></tr>
            <tr><td>Downloads Manager Drawer</td><td><span class="kbd">Ctrl</span> + <span class="kbd">Shift</span> + <span class="kbd">J</span></td></tr>
            <tr><td>Browsing History Drawer</td><td><span class="kbd">Ctrl</span> + <span class="kbd">H</span></td></tr>
            <tr><td>Mute / Unmute Audio</td><td><span class="kbd">Ctrl</span> + <span class="kbd">M</span></td></tr>
            <tr><td>Toggle Full Screen Mode</td><td><span class="kbd">F11</span></td></tr>
            <tr><td>Toggle Left Sidebar</td><td><span class="kbd">Ctrl</span> + <span class="kbd">S</span></td></tr>
            <tr><td>Developer Tools</td><td><span class="kbd">F12</span></td></tr>
            <tr><td>Reload Page</td><td><span class="kbd">Ctrl</span> + <span class="kbd">R</span></td></tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- ABOUT SECTION -->
    <div id="about" class="content-section">
      <h1 class="section-title">About Browy</h1>
      <p class="section-subtitle">A modern, Arc/Zen-inspired Windows 11 browser built with .NET 9 and WebView2.</p>

      <div class="settings-group">
        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Version</div>
            <div class="row-desc">0.2.0 (Windows 11 Fluent Acrylic Edition)</div>
          </div>
        </div>
        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Core Engine</div>
            <div class="row-desc">Microsoft Edge WebView2 Chromium + .NET 9 WPF</div>
          </div>
        </div>
        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Source Repository</div>
            <div class="row-desc">github.com/tomlin7/Browy</div>
          </div>
          <button class="btn btn-secondary" onclick="window.open('https://github.com/tomlin7/Browy', '_blank')">View GitHub</button>
        </div>
      </div>
    </div>

  </div>

  <div id="toast" class="toast">Action performed successfully</div>

<script>
  function switchSection(sectionId, element) {
    document.querySelectorAll('.content-section').forEach(el => el.classList.remove('active'));
    document.querySelectorAll('.nav-item').forEach(el => el.classList.remove('active'));
    document.getElementById(sectionId).classList.add('active');
    element.classList.add('active');
  }

  function showToast(message) {
    const toast = document.getElementById('toast');
    toast.textContent = message;
    toast.style.display = 'flex';
    setTimeout(() => { toast.style.display = 'none'; }, 2600);
  }

  function postAction(action, data = {}) {
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage(JSON.stringify({ action: action, ...data }));
    }
    if (action === 'clear_history') showToast('Browsing history cleared.');
    if (action === 'clear_cookies') showToast('Cache and cookies cleared.');
    if (action === 'open_downloads_folder') showToast('Opening Downloads folder...');
  }

  function onSearchEngineChange(engine) {
    let url = 'https://www.google.com/search?q=';
    if (engine === 'DuckDuckGo') url = 'https://duckduckgo.com/?q=';
    if (engine === 'Bing') url = 'https://www.bing.com/search?q=';
    if (engine === 'Brave') url = 'https://search.brave.com/search?q=';
    
    postAction('set_search_engine', { name: engine, url: url });
    showToast('Default search engine updated to ' + engine + '.');
  }
</script>
</body>
</html>
""";
        }

        public static string GetErrorPageHtml(string failedUrl, string errorStatus, string details = "")
        {
            return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>Cannot Reach Site - Browy</title>
<style>
  body {
    background: #0f0f14;
    color: #e2e8f0;
    font-family: 'Segoe UI Variable Text', 'Segoe UI', system-ui, sans-serif;
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100vh;
    margin: 0;
    user-select: none;
  }
  .card {
    background: #171720;
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 14px;
    padding: 36px 42px;
    max-width: 520px;
    text-align: center;
    box-shadow: 0 20px 40px rgba(0,0,0,0.5);
  }
  .icon {
    font-size: 38px;
    margin-bottom: 16px;
    color: #ef4444;
  }
  h1 {
    font-size: 20px;
    font-weight: 600;
    margin-bottom: 8px;
  }
  .url {
    color: #94a3b8;
    font-size: 13px;
    word-break: break-all;
    margin-bottom: 14px;
  }
  .status {
    display: inline-block;
    background: rgba(239, 68, 68, 0.15);
    color: #fca5a5;
    font-size: 11px;
    padding: 3px 8px;
    border-radius: 5px;
    margin-bottom: 20px;
  }
  p {
    font-size: 12.5px;
    color: #cbd5e1;
    line-height: 1.6;
    margin-bottom: 24px;
  }
  .actions {
    display: flex;
    gap: 10px;
    justify-content: center;
  }
  button {
    background: #3b82f6;
    color: white;
    border: none;
    padding: 9px 18px;
    border-radius: 7px;
    font-size: 12.5px;
    font-weight: 500;
    cursor: pointer;
    transition: background 0.15s;
  }
  button:hover { background: #2563eb; }
  button.secondary {
    background: rgba(255,255,255,0.08);
    color: #e2e8f0;
  }
  button.secondary:hover { background: rgba(255,255,255,0.14); }
</style>
</head>
<body>
  <div class="card">
    <div class="icon">&#x26A0;</div>
    <h1>This site can’t be reached</h1>
    <div class="url">{{failedUrl}}</div>
    <div class="status">{{errorStatus}}</div>
    <p>Check your internet connection, verify the address, or try searching for the page.</p>
    <div class="actions">
      <button onclick="location.reload()">Try Again</button>
      <button class="secondary" onclick="window.location.href='https://www.google.com/search?q=' + encodeURIComponent('{{failedUrl}}')">Search Google</button>
    </div>
  </div>
</body>
</html>
""";
        }
    }
}
