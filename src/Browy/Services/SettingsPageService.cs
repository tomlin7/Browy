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
    --bg-main: #0b0c0e;
    --bg-sidebar: #0f1013;
    --bg-card: #141619;
    --bg-card-hover: #181b20;
    --border-color: rgba(255, 255, 255, 0.07);
    --border-subtle: rgba(255, 255, 255, 0.05);
    --border-hover: rgba(255, 255, 255, 0.16);
    --text-primary: #f4f4f5;
    --text-secondary: #a1a1aa;
    --text-muted: #71717a;
    --accent: {{settings.AccentColorHex}};
    --accent-hover: #ffffff;
    --danger: #ef4444;
    --danger-subtle: rgba(239, 68, 68, 0.12);
    --danger-border: rgba(239, 68, 68, 0.28);
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
    display: flex;
    height: 100vh;
    overflow: hidden;
  }

  /* Left Sidebar Navigation */
  .sidebar {
    width: 236px;
    background: var(--bg-sidebar);
    border-right: 1px solid var(--border-color);
    padding: 24px 12px;
    display: flex;
    flex-direction: column;
    gap: 4px;
    flex-shrink: 0;
    overscroll-behavior: contain;
  }

  .brand {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 0 10px 18px 10px;
    border-bottom: 1px solid var(--border-color);
    margin-bottom: 10px;
  }

  .brand-icon {
    width: 22px;
    height: 22px;
    border-radius: 6px;
    background: #f4f4f5;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    color: #0b0c0e;
  }

  .brand-icon svg {
    width: 14px;
    height: 14px;
    fill: currentColor;
  }

  .brand-title {
    font-size: 13.5px;
    font-weight: 600;
    letter-spacing: -0.01em;
    color: var(--text-primary);
  }

  .nav-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 7px 10px;
    border-radius: 6px;
    color: var(--text-secondary);
    font-size: 13px;
    font-weight: 400;
    cursor: pointer;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, color, transform;
    transition-duration: 120ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  .nav-item:not(:disabled):active {
    transform: scale(0.98);
  }

  @media (hover: hover) {
    .nav-item:hover {
      background: rgba(255, 255, 255, 0.04);
      color: var(--text-primary);
    }
  }

  .nav-item.active {
    background: rgba(255, 255, 255, 0.08);
    color: var(--text-primary);
    font-weight: 500;
  }

  .nav-icon {
    font-family: 'Segoe Fluent Icons', 'Segoe MDL2 Assets';
    font-size: 14px;
    width: 16px;
    text-align: center;
    color: var(--text-muted);
    line-height: 1;
    display: inline-flex;
    align-items: center;
    justify-content: center;
  }

  .nav-item.active .nav-icon {
    color: var(--text-primary);
  }

  /* Content Area */
  .content {
    flex: 1;
    overflow-y: auto;
    padding: 36px 48px;
    overscroll-behavior: contain;
  }

  .content-section {
    display: none;
    max-width: 800px;
  }

  .content-section.active {
    display: block;
    animation: fadeIn 0.15s cubic-bezier(0.2, 0, 0, 1);
  }

  @keyframes fadeIn {
    from { opacity: 0; transform: translateY(4px); }
    to { opacity: 1; transform: translateY(0); }
  }

  .section-title {
    font-size: 22px;
    font-weight: 600;
    letter-spacing: -0.02em;
    color: var(--text-primary);
    margin-bottom: 4px;
  }

  .section-subtitle {
    font-size: 13px;
    color: var(--text-muted);
    line-height: 1.5;
    margin-bottom: 24px;
  }

  /* Settings Card & Groups */
  .settings-group {
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    margin-bottom: 20px;
    overflow: hidden;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.02);
  }

  .settings-row {
    padding: 14px 18px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    border-bottom: 1px solid var(--border-subtle);
    gap: 16px;
  }

  .settings-row:last-child {
    border-bottom: none;
  }

  .row-info {
    flex: 1;
    min-width: 0;
  }

  .row-title {
    font-size: 13px;
    font-weight: 500;
    color: var(--text-primary);
    margin-bottom: 2px;
    letter-spacing: -0.01em;
  }

  .row-desc {
    font-size: 12px;
    color: var(--text-secondary);
    line-height: 1.45;
  }

  /* Form Controls */
  select {
    background-color: #101215;
    border: 1px solid var(--border-color);
    color: var(--text-primary);
    padding: 6px 12px;
    border-radius: 6px;
    font-size: 12.5px;
    font-family: inherit;
    outline: none;
    cursor: pointer;
    -webkit-tap-highlight-color: transparent;
    transition-property: border-color, background-color, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  @media (hover: hover) {
    select:hover {
      border-color: var(--border-hover);
      background-color: #141619;
    }
  }

  select:focus {
    border-color: rgba(255, 255, 255, 0.28);
    box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.15);
  }

  select option {
    background: #141619;
    color: #f4f4f5;
  }

  input[type="text"], input[type="password"] {
    background: #101215;
    border: 1px solid var(--border-color);
    color: var(--text-primary);
    padding: 6px 12px;
    border-radius: 6px;
    font-size: 12.5px;
    font-family: var(--font-mono);
    outline: none;
    -webkit-tap-highlight-color: transparent;
    transition-property: border-color, background-color, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  input[type="text"]:focus, input[type="password"]:focus {
    border-color: rgba(255, 255, 255, 0.28);
    box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.15);
    background: #121417;
  }

  /* Accent Swatches */
  .accent-swatch {
    width: 22px;
    height: 22px;
    border-radius: 50%;
    cursor: pointer;
    border: 2px solid transparent;
    -webkit-tap-highlight-color: transparent;
    transition-property: transform, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  @media (hover: hover) {
    .accent-swatch:hover {
      transform: scale(1.15);
    }
  }

  .accent-swatch:not(:disabled):active {
    transform: scale(0.96);
  }

  .accent-swatch.active {
    box-shadow: 0 0 0 2px var(--bg-card), 0 0 0 3.5px #f4f4f5;
    transform: scale(1.15);
  }

  input[type="color"] {
    -webkit-appearance: none;
    border: 1px solid var(--border-color);
    border-radius: 50%;
    width: 22px;
    height: 22px;
    padding: 0;
    background: transparent;
    cursor: pointer;
    vertical-align: middle;
    transition-property: transform, border-color;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  @media (hover: hover) {
    input[type="color"]:hover {
      transform: scale(1.15);
      border-color: var(--border-hover);
    }
  }

  input[type="color"]::-webkit-color-swatch-wrapper {
    padding: 0;
    border-radius: 50%;
  }

  input[type="color"]::-webkit-color-swatch {
    border: none;
    border-radius: 50%;
  }

  /* Buttons */
  .btn {
    padding: 6px 14px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    font-family: inherit;
    cursor: pointer;
    border: 1px solid transparent;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform, box-shadow;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  .btn:not(:disabled):active {
    transform: scale(0.96);
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

  .btn-secondary {
    background: transparent;
    border-color: var(--border-color);
    color: var(--text-primary);
  }

  @media (hover: hover) {
    .btn-secondary:hover {
      background: rgba(255, 255, 255, 0.05);
      border-color: var(--border-hover);
    }
  }

  .btn-danger {
    background: var(--danger-subtle);
    border-color: var(--danger-border);
    color: #fca5a5;
  }

  @media (hover: hover) {
    .btn-danger:hover {
      background: rgba(239, 68, 68, 0.2);
      border-color: rgba(239, 68, 68, 0.4);
      color: #ffffff;
    }
  }

  /* Toggle Switch */
  .toggle-switch {
    position: relative;
    width: 38px;
    height: 20px;
    display: inline-block;
    flex-shrink: 0;
    -webkit-tap-highlight-color: transparent;
  }

  .toggle-switch input {
    opacity: 0;
    width: 0;
    height: 0;
    position: absolute;
  }

  .toggle-slider {
    position: absolute;
    cursor: pointer;
    top: 0; left: 0; right: 0; bottom: 0;
    background-color: rgba(255, 255, 255, 0.12);
    border: 1px solid rgba(255, 255, 255, 0.06);
    border-radius: 20px;
    transition-property: background-color, border-color;
    transition-duration: 150ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  .toggle-slider:before {
    position: absolute;
    content: "";
    height: 14px;
    width: 14px;
    left: 2px;
    bottom: 2px;
    background-color: #f4f4f5;
    border-radius: 50%;
    transition-property: transform, background-color;
    transition-duration: 150ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  input:checked + .toggle-slider {
    background-color: #f4f4f5;
    border-color: #f4f4f5;
  }

  input:checked + .toggle-slider:before {
    transform: translateX(18px);
    background-color: #0b0c0e;
  }

  /* Shortcuts Table */
  .shortcuts-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 12.5px;
  }

  .shortcuts-table th {
    text-align: left;
    padding: 10px 18px;
    font-size: 11px;
    color: var(--text-muted);
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    border-bottom: 1px solid var(--border-color);
  }

  .shortcuts-table td {
    padding: 11px 18px;
    border-bottom: 1px solid var(--border-subtle);
  }

  .kbd {
    display: inline-block;
    padding: 2px 7px;
    font-size: 11px;
    font-family: var(--font-mono);
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid var(--border-color);
    border-radius: 4px;
    color: #e4e4e7;
  }

  .key-pill {
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid var(--border-color);
    border-radius: 5px;
    padding: 4px 8px;
    color: var(--text-primary);
    font-family: var(--font-mono);
    font-size: 11.5px;
    cursor: pointer;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 120ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
    display: inline-flex;
    align-items: center;
    gap: 4px;
    user-select: none;
  }

  .key-pill:not(:disabled):active {
    transform: scale(0.96);
  }

  @media (hover: hover) {
    .key-pill:hover {
      background: rgba(255, 255, 255, 0.09);
      border-color: var(--border-hover);
    }
  }

  .key-pill.recording {
    background: var(--danger-subtle);
    border-color: var(--danger-border);
    color: #fca5a5;
  }

  /* Toast Notification */
  .toast {
    position: fixed;
    bottom: 24px;
    right: 24px;
    background: #141619;
    border: 1px solid rgba(255, 255, 255, 0.12);
    color: #f4f4f5;
    padding: 10px 16px;
    border-radius: 6px;
    box-shadow: 0 12px 32px rgba(0, 0, 0, 0.6), inset 0 1px 0 rgba(255, 255, 255, 0.04);
    font-size: 12.5px;
    display: none;
    align-items: center;
    gap: 8px;
    z-index: 999;
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

  <!-- Navigation Sidebar -->
  <div class="sidebar">
    <div class="brand">
      <div class="brand-icon">
        <svg viewBox="0 0 24 24"><path d="M4 4h9a5 5 0 0 1 5 5 5 5 0 0 1-5 5H4V4zm0 10h10a5 5 0 0 1 5 5 5 5 0 0 1-5 5H4v-10z"></path></svg>
      </div>
      <div class="brand-title">Settings</div>
    </div>
    <div class="nav-item active" onclick="switchSection('general', this)">
      <span class="nav-icon">&#xE713;</span> General
    </div>
    <div class="nav-item" onclick="switchSection('appearance', this)">
      <span class="nav-icon">&#xE790;</span> Appearance
    </div>
    <div class="nav-item" onclick="switchSection('privacy', this)">
      <span class="nav-icon">&#xE72E;</span> Privacy & Security
    </div>
    <div class="nav-item" onclick="switchSection('downloads', this)">
      <span class="nav-icon">&#xE896;</span> Downloads
    </div>
    <div class="nav-item" onclick="switchSection('copilot', this)">
      <span class="nav-icon">&#xE99A;</span> AI Copilot
    </div>
    <div class="nav-item" onclick="switchSection('performance', this)">
      <span class="nav-icon">&#xE950;</span> Performance &amp; Gestures
    </div>
    <div class="nav-item" onclick="switchSection('shortcuts', this)">
      <span class="nav-icon">&#xE765;</span> Keyboard Shortcuts
    </div>
    <div class="nav-item" onclick="switchSection('about', this)">
      <span class="nav-icon">&#xE946;</span> About Browy
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
            <option value="Ecosia" {{(currentEngine == "Ecosia" ? "selected" : "")}}>Ecosia</option>
          </select>
        </div>

        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">On Startup</div>
            <div class="row-desc">Open the Browy start page or continue where you left off.</div>
          </div>
          <select id="startupBehaviorSelect" onchange="onStartupBehaviorChange(this.value)">
            <option value="RestoreSession" {{(settings.StartupBehavior == "RestoreSession" ? "selected" : "")}}>Restore previous session tabs</option>
            <option value="NewTab" {{(settings.StartupBehavior == "NewTab" ? "selected" : "")}}>Open the Browy Start Page (New Tab)</option>
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
            <div class="row-title">Windows 11 DWM Backdrop Material</div>
            <div class="row-desc">Select native Windows 11 desktop blur material or high-performance solid theme.</div>
          </div>
          <select id="backdropSelect" onchange="onBackdropChange(this.value)">
            <option value="Acrylic" {{(settings.BackdropMaterial == "Acrylic" ? "selected" : "")}}>Desktop Acrylic (Frosted Glass)</option>
            <option value="Mica" {{(settings.BackdropMaterial == "Mica" ? "selected" : "")}}>Mica (Desktop Wallpaper Tint)</option>
            <option value="MicaAlt" {{(settings.BackdropMaterial == "MicaAlt" ? "selected" : "")}}>Mica Alt (Tabbed Surface)</option>
            <option value="SolidDark" {{(settings.BackdropMaterial == "SolidDark" ? "selected" : "")}}>Solid Dark Theme (Windows 10 / High Performance)</option>
          </select>
        </div>

        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Theme Mode</div>
            <div class="row-desc">Browy is optimized with an immersive modern dark palette.</div>
          </div>
          <select id="themeModeSelect" onchange="onThemeModeChange(this.value)">
            <option value="Dark" {{(settings.ThemeMode == "Dark" ? "selected" : "")}}>Dark (Arc &amp; Zen Fluent)</option>
            <option value="Light" {{(settings.ThemeMode == "Light" ? "selected" : "")}}>Light Mode</option>
            <option value="System" {{(settings.ThemeMode == "System" ? "selected" : "")}}>Follow Windows System</option>
          </select>
        </div>

        <div class="settings-row" style="align-items: flex-start;">
          <div class="row-info">
            <div class="row-title">Accent Color &amp; Space Palette</div>
            <div class="row-desc">Choose a signature Arc &amp; Zen accent tint for borders, active tabs, buttons, and highlights.</div>
          </div>
          <div style="display: flex; flex-direction: column; align-items: flex-end; gap: 8px;">
            <div class="color-palette-row" style="display: flex; gap: 8px; align-items: center;">
              <div class="accent-swatch {{(settings.AccentColorHex == "#3B82F6" ? "active" : "")}}" style="background: #3B82F6;" onclick="onSelectAccentColor('#3B82F6')" title="Electric Blue"></div>
              <div class="accent-swatch {{(settings.AccentColorHex == "#8B5CF6" ? "active" : "")}}" style="background: #8B5CF6;" onclick="onSelectAccentColor('#8B5CF6')" title="Arc Purple"></div>
              <div class="accent-swatch {{(settings.AccentColorHex == "#10B981" ? "active" : "")}}" style="background: #10B981;" onclick="onSelectAccentColor('#10B981')" title="Emerald Green"></div>
              <div class="accent-swatch {{(settings.AccentColorHex == "#F59E0B" ? "active" : "")}}" style="background: #F59E0B;" onclick="onSelectAccentColor('#F59E0B')" title="Sunset Amber"></div>
              <div class="accent-swatch {{(settings.AccentColorHex == "#EC4899" ? "active" : "")}}" style="background: #EC4899;" onclick="onSelectAccentColor('#EC4899')" title="Rose Quartz"></div>
              <div class="accent-swatch {{(settings.AccentColorHex == "#06B6D4" ? "active" : "")}}" style="background: #06B6D4;" onclick="onSelectAccentColor('#06B6D4')" title="Cyber Cyan"></div>
              <div class="accent-swatch {{(settings.AccentColorHex == "#64748B" ? "active" : "")}}" style="background: #64748B;" onclick="onSelectAccentColor('#64748B')" title="Midnight Slate"></div>
              <input type="color" id="customAccentPicker" value="{{settings.AccentColorHex}}" onchange="onSelectAccentColor(this.value)" title="Custom Color Picker" style="width: 24px; height: 24px; border: 1px solid var(--border-color); border-radius: 50%; cursor: pointer; background: transparent; padding: 0;">
            </div>
            <div style="font-size: 11px; color: var(--text-muted); font-family: var(--font-mono);">Active Tint: <span id="activeAccentText" style="color: {{settings.AccentColorHex}}; font-weight: 600;">{{settings.AccentColorHex}}</span></div>
          </div>
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
            <div class="row-title">Browy Shield (Ad & Tracker Blocker)</div>
            <div class="row-desc">Proactively intercept and block intrusive ads, trackers, and analytical telemetry across all tabs.</div>
          </div>
          <label class="toggle-switch">
            <input type="checkbox" id="shieldToggle" {{(settings.IsShieldEnabled ? "checked" : "")}} onchange="onShieldToggleChange(this.checked)">
            <span class="toggle-slider"></span>
          </label>
        </div>

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

    <!-- AI COPILOT SECTION -->
    <div id="copilot" class="content-section">
      <h1 class="section-title">AI Copilot (BYOK)</h1>
      <p class="section-subtitle">Configure your own API keys for OpenAI, Anthropic Claude, or local Ollama LLMs.</p>

      <div class="settings-group">
        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Active AI Provider</div>
            <div class="row-desc">Choose between built-in heuristics or your own AI model provider.</div>
          </div>
          <select id="aiProviderSelect" onchange="onAiProviderChange(this.value)">
            <option value="BrowyDefault" {{(settings.AiProvider == "BrowyDefault" ? "selected" : "")}}>Browy Built-in (Local Heuristics)</option>
            <option value="OpenAI" {{(settings.AiProvider == "OpenAI" ? "selected" : "")}}>OpenAI (GPT-4o, GPT-4o-mini)</option>
            <option value="Anthropic" {{(settings.AiProvider == "Anthropic" ? "selected" : "")}}>Anthropic (Claude 3.5 Sonnet)</option>
            <option value="Ollama" {{(settings.AiProvider == "Ollama" ? "selected" : "")}}>Local Ollama</option>
          </select>
        </div>

        <div class="settings-row" id="openAiKeyRow">
          <div class="row-info">
            <div class="row-title">OpenAI API Key</div>
            <div class="row-desc">Bearer token for OpenAI API requests (stored locally).</div>
          </div>
          <input type="password" id="openAiKeyInput" placeholder="sk-..." value="{{settings.OpenAiApiKey}}" style="width: 220px;" onchange="saveAiConfig()">
        </div>

        <div class="settings-row" id="anthropicKeyRow">
          <div class="row-info">
            <div class="row-title">Anthropic API Key</div>
            <div class="row-desc">API key for Claude models (stored locally).</div>
          </div>
          <input type="password" id="anthropicKeyInput" placeholder="sk-ant-..." value="{{settings.AnthropicApiKey}}" style="width: 220px;" onchange="saveAiConfig()">
        </div>

        <div class="settings-row" id="ollamaEndpointRow">
          <div class="row-info">
            <div class="row-title">Ollama Endpoint</div>
            <div class="row-desc">Local Ollama instance URL (default: http://localhost:11434).</div>
          </div>
          <input type="text" id="ollamaEndpointInput" placeholder="http://localhost:11434" value="{{settings.OllamaEndpoint}}" style="width: 220px;" onchange="saveAiConfig()">
        </div>

        <div class="settings-row" id="aiModelInputRow">
          <div class="row-info">
            <div class="row-title">Selected Model Name</div>
            <div class="row-desc">e.g. gpt-4o-mini, gpt-4o, claude-3-5-sonnet-20241022, llama3, etc.</div>
          </div>
          <input type="text" id="aiModelInput" placeholder="gpt-4o-mini" value="{{settings.SelectedAiModel}}" style="width: 220px;" onchange="saveAiConfig()">
        </div>
      </div>
    </div>

    <!-- PERFORMANCE & GESTURES SECTION -->
    <div id="performance" class="content-section">
      <h1 class="section-title">Performance &amp; Gestures</h1>
      <p class="section-subtitle">Fine-tune memory saving tab suspension, idle throttling, and navigation gestures.</p>

      <div class="settings-group">
        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Sleeping Tabs (Memory Saver)</div>
            <div class="row-desc">Automatically suspend background tab renderers when idle to reclaim memory and CPU.</div>
          </div>
          <label class="toggle-switch">
            <input type="checkbox" id="tabSuspensionToggle" {{(settings.EnableIdleTabSuspension ? "checked" : "")}} onchange="onPerformanceChange()">
            <span class="toggle-slider"></span>
          </label>
        </div>

        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Put Inactive Tabs to Sleep After</div>
            <div class="row-desc">Tabs playing audio or actively pinned in split view are never suspended.</div>
          </div>
          <select id="idleTimeoutSelect" onchange="onPerformanceChange()">
            <option value="5" {{(settings.IdleTabSuspensionMinutes == 5 ? "selected" : "")}}>5 minutes</option>
            <option value="15" {{(settings.IdleTabSuspensionMinutes == 15 ? "selected" : "")}}>15 minutes</option>
            <option value="30" {{(settings.IdleTabSuspensionMinutes == 30 ? "selected" : "")}}>30 minutes</option>
            <option value="60" {{(settings.IdleTabSuspensionMinutes == 60 ? "selected" : "")}}>1 hour</option>
          </select>
        </div>

        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Active Tab Memory Optimizer</div>
            <div class="row-desc">Immediately suspend idle tabs to free memory, or wake all background tabs.</div>
          </div>
          <div style="display: flex; gap: 8px;">
            <button class="btn btn-secondary" onclick="postAction('snooze_inactive_tabs'); showToast('Sleeping background tabs to free memory.');">Snooze Inactive Tabs</button>
            <button class="btn btn-secondary" onclick="postAction('wake_all_tabs'); showToast('Resumed all tabs.');">Wake All Tabs</button>
          </div>
        </div>

        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Right-Click Mouse Gestures</div>
            <div class="row-desc">Hold right mouse button and drag left to go Back, right for Forward, down for New Tab.</div>
          </div>
          <label class="toggle-switch">
            <input type="checkbox" id="mouseGesturesToggle" {{(settings.EnableMouseGestures ? "checked" : "")}} onchange="onPerformanceChange()">
            <span class="toggle-slider"></span>
          </label>
        </div>

        <div class="settings-row">
          <div class="row-info">
            <div class="row-title">Two-Finger Trackpad Horizontal Swipe</div>
            <div class="row-desc">Fluid page history navigation with two-finger horizontal trackpad gestures.</div>
          </div>
          <label class="toggle-switch">
            <input type="checkbox" id="trackpadSwipeToggle" {{(settings.EnableTrackpadSwipe ? "checked" : "")}} onchange="onPerformanceChange()">
            <span class="toggle-slider"></span>
          </label>
        </div>
      </div>
    </div>

    <!-- SHORTCUTS MANAGER -->
    <div id="shortcuts" class="content-section">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
        <h1 class="section-title" style="margin: 0;">Keyboard Shortcuts</h1>
        <button class="btn btn-secondary" onclick="resetAllShortcuts()">Reset to Defaults</button>
      </div>
      <p class="section-subtitle">Full searchable keyboard shortcuts manager. Click any pill to record a custom shortcut.</p>

      <div style="margin-bottom: 16px;">
        <input type="text" id="shortcutSearch" placeholder="Search shortcuts (e.g. Tab, Palette, DevTools, Back)..."
               style="width: 100%; padding: 8px 14px;"
               oninput="filterShortcuts(this.value)">
      </div>

      <div id="shortcutConflict" style="display:none; background: var(--danger-subtle); border: 1px solid var(--danger-border); border-radius: 6px; padding: 10px 14px; color: #fca5a5; font-size: 12.5px; margin-bottom: 16px;"></div>

      <div class="settings-group">
        <table class="shortcuts-table" id="shortcutsTable">
          <thead>
            <tr>
              <th>Action</th>
              <th>Category</th>
              <th>Shortcut Keybinding</th>
            </tr>
          </thead>
          <tbody id="shortcutsListBody">
            <tr data-action="palette" data-name="Open Command Palette" data-cat="Navigation">
              <td>Open Command Palette</td>
              <td><span class="kbd">Navigation</span></td>
              <td><button class="key-pill" onclick="recordShortcut('palette', this)">{{(settings.CustomKeyBindings.TryGetValue("palette", out var pVal) ? pVal : "Ctrl+K")}}</button></td>
            </tr>
            <tr data-action="settings" data-name="Open Settings" data-cat="General">
              <td>Open Settings</td>
              <td><span class="kbd">General</span></td>
              <td><button class="key-pill" onclick="recordShortcut('settings', this)">{{(settings.CustomKeyBindings.TryGetValue("settings", out var sVal) ? sVal : "Ctrl+,")}}</button></td>
            </tr>
            <tr data-action="new_tab" data-name="New Tab" data-cat="Tabs">
              <td>New Tab</td>
              <td><span class="kbd">Tabs</span></td>
              <td><button class="key-pill" onclick="recordShortcut('new_tab', this)">{{(settings.CustomKeyBindings.TryGetValue("new_tab", out var ntVal) ? ntVal : "Ctrl+T")}}</button></td>
            </tr>
            <tr data-action="close_tab" data-name="Close Active Tab" data-cat="Tabs">
              <td>Close Active Tab</td>
              <td><span class="kbd">Tabs</span></td>
              <td><button class="key-pill" onclick="recordShortcut('close_tab', this)">{{(settings.CustomKeyBindings.TryGetValue("close_tab", out var ctVal) ? ctVal : "Ctrl+W")}}</button></td>
            </tr>
            <tr data-action="reopen_tab" data-name="Reopen Closed Tab" data-cat="Tabs">
              <td>Reopen Closed Tab</td>
              <td><span class="kbd">Tabs</span></td>
              <td><button class="key-pill" onclick="recordShortcut('reopen_tab', this)">{{(settings.CustomKeyBindings.TryGetValue("reopen_tab", out var rtVal) ? rtVal : "Ctrl+Shift+T")}}</button></td>
            </tr>
            <tr data-action="omnibox" data-name="Focus Address Bar (Omnibox)" data-cat="Navigation">
              <td>Focus Address Bar (Omnibox)</td>
              <td><span class="kbd">Navigation</span></td>
              <td><button class="key-pill" onclick="recordShortcut('omnibox', this)">{{(settings.CustomKeyBindings.TryGetValue("omnibox", out var oVal) ? oVal : "Ctrl+L")}}</button></td>
            </tr>
            <tr data-action="find" data-name="Find in Page" data-cat="Navigation">
              <td>Find in Page</td>
              <td><span class="kbd">Navigation</span></td>
              <td><button class="key-pill" onclick="recordShortcut('find', this)">{{(settings.CustomKeyBindings.TryGetValue("find", out var fVal) ? fVal : "Ctrl+F")}}</button></td>
            </tr>
            <tr data-action="downloads" data-name="Downloads Drawer" data-cat="Tools">
              <td>Downloads Drawer</td>
              <td><span class="kbd">Tools</span></td>
              <td><button class="key-pill" onclick="recordShortcut('downloads', this)">{{(settings.CustomKeyBindings.TryGetValue("downloads", out var dVal) ? dVal : "Ctrl+Shift+J")}}</button></td>
            </tr>
            <tr data-action="history" data-name="Browsing History Hub" data-cat="Tools">
              <td>Browsing History Hub</td>
              <td><span class="kbd">Tools</span></td>
              <td><button class="key-pill" onclick="recordShortcut('history', this)">{{(settings.CustomKeyBindings.TryGetValue("history", out var hVal) ? hVal : "Ctrl+H")}}</button></td>
            </tr>
            <tr data-action="split_view" data-name="Toggle Split View" data-cat="View">
              <td>Toggle Split View</td>
              <td><span class="kbd">View</span></td>
              <td><button class="key-pill" onclick="recordShortcut('split_view', this)">{{(settings.CustomKeyBindings.TryGetValue("split_view", out var spVal) ? spVal : "Ctrl+\\")}}</button></td>
            </tr>
            <tr data-action="mute" data-name="Mute / Unmute Audio" data-cat="Tabs">
              <td>Mute / Unmute Audio</td>
              <td><span class="kbd">Tabs</span></td>
              <td><button class="key-pill" onclick="recordShortcut('mute', this)">{{(settings.CustomKeyBindings.TryGetValue("mute", out var mVal) ? mVal : "Ctrl+M")}}</button></td>
            </tr>
            <tr data-action="fullscreen" data-name="Toggle Full Screen" data-cat="View">
              <td>Toggle Full Screen</td>
              <td><span class="kbd">View</span></td>
              <td><button class="key-pill" onclick="recordShortcut('fullscreen', this)">{{(settings.CustomKeyBindings.TryGetValue("fullscreen", out var fsVal) ? fsVal : "F11")}}</button></td>
            </tr>
            <tr data-action="sidebar" data-name="Toggle Left Sidebar" data-cat="View">
              <td>Toggle Left Sidebar</td>
              <td><span class="kbd">View</span></td>
              <td><button class="key-pill" onclick="recordShortcut('sidebar', this)">{{(settings.CustomKeyBindings.TryGetValue("sidebar", out var sbVal) ? sbVal : "Ctrl+S")}}</button></td>
            </tr>
            <tr data-action="devtools" data-name="Developer Tools" data-cat="Tools">
              <td>Developer Tools</td>
              <td><span class="kbd">Tools</span></td>
              <td><button class="key-pill" onclick="recordShortcut('devtools', this)">{{(settings.CustomKeyBindings.TryGetValue("devtools", out var dtVal) ? dtVal : "F12")}}</button></td>
            </tr>
            <tr data-action="reader_mode" data-name="Reader Mode" data-cat="View">
              <td>Reader Mode</td>
              <td><span class="kbd">View</span></td>
              <td><button class="key-pill" onclick="recordShortcut('reader_mode', this)">{{(settings.CustomKeyBindings.TryGetValue("reader_mode", out var rmVal) ? rmVal : "F9")}}</button></td>
            </tr>
            <tr data-action="bookmarks_bar" data-name="Bookmarks Bar" data-cat="View">
              <td>Bookmarks Bar</td>
              <td><span class="kbd">View</span></td>
              <td><button class="key-pill" onclick="recordShortcut('bookmarks_bar', this)">{{(settings.CustomKeyBindings.TryGetValue("bookmarks_bar", out var bmVal) ? bmVal : "Ctrl+Shift+B")}}</button></td>
            </tr>
            <tr data-action="reload" data-name="Reload Page" data-cat="Navigation">
              <td>Reload Page</td>
              <td><span class="kbd">Navigation</span></td>
              <td><button class="key-pill" onclick="recordShortcut('reload', this)">{{(settings.CustomKeyBindings.TryGetValue("reload", out var rVal) ? rVal : "Ctrl+R")}}</button></td>
            </tr>
            <tr data-action="print" data-name="Print / Save PDF" data-cat="Tools">
              <td>Print / Save PDF</td>
              <td><span class="kbd">Tools</span></td>
              <td><button class="key-pill" onclick="recordShortcut('print', this)">{{(settings.CustomKeyBindings.TryGetValue("print", out var prVal) ? prVal : "Ctrl+P")}}</button></td>
            </tr>
            <tr data-action="pip" data-name="Picture-in-Picture" data-cat="Tools">
              <td>Picture-in-Picture</td>
              <td><span class="kbd">Tools</span></td>
              <td><button class="key-pill" onclick="recordShortcut('pip', this)">{{(settings.CustomKeyBindings.TryGetValue("pip", out var pipVal) ? pipVal : "Alt+P")}}</button></td>
            </tr>
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
            <div class="row-desc">0.3.0 (Windows 11 Fluent Edition with Shield &amp; Reader Mode)</div>
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
            <div class="row-title">Diagnostics &amp; System Hub</div>
            <div class="row-desc">View hardware specs, memory usage, runtime stats, and WebView2 diagnostics (<code style="color: var(--accent); font-family: var(--font-mono);">browy://version</code>).</div>
          </div>
          <button class="btn btn-secondary" onclick="window.location.href='browy://version'">Open browy://version</button>
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
    setTimeout(() => { toast.style.display = 'none'; }, 2400);
  }

  function postAction(action, data = {}) {
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage(JSON.stringify({ action: action, ...data }));
    }
    if (action === 'clear_history') showToast('Browsing history cleared.');
    if (action === 'clear_cookies') showToast('Cache and cookies cleared.');
    if (action === 'open_downloads_folder') showToast('Opening Downloads folder...');
  }

  function onStartupBehaviorChange(val) {
    postAction('set_startup_behavior', { behavior: val });
    showToast('Startup behavior updated.');
  }

  function onShieldToggleChange(enabled) {
    postAction('set_shield_enabled', { enabled: enabled });
    showToast('Browy Shield ' + (enabled ? 'enabled.' : 'paused.'));
  }

  function onSearchEngineChange(engine) {
    let url = 'https://www.google.com/search?q=';
    if (engine === 'DuckDuckGo') url = 'https://duckduckgo.com/?q=';
    if (engine === 'Bing') url = 'https://www.bing.com/search?q=';
    if (engine === 'Brave') url = 'https://search.brave.com/search?q=';
    if (engine === 'Ecosia') url = 'https://www.ecosia.org/search?q=';
    
    postAction('set_search_engine', { name: engine, url: url });
    showToast('Default search engine updated to ' + engine + '.');
  }

  function saveAiConfig() {
    const provider = document.getElementById('aiProviderSelect').value;
    const openAiKey = document.getElementById('openAiKeyInput').value;
    const anthropicKey = document.getElementById('anthropicKeyInput').value;
    const ollamaEndpoint = document.getElementById('ollamaEndpointInput').value;
    const selectedModel = document.getElementById('aiModelInput').value;

    postAction('set_ai_config', {
      provider: provider,
      openAiKey: openAiKey,
      anthropicKey: anthropicKey,
      ollamaEndpoint: ollamaEndpoint,
      selectedModel: selectedModel
    });
    showToast('AI Copilot BYOK settings saved.');
  }

  function onAiProviderChange(val) {
    saveAiConfig();
  }

  function onBackdropChange(val) {
    postAction('set_backdrop_material', { material: val });
    showToast('Backdrop material updated to ' + val);
  }

  function onThemeModeChange(val) {
    postAction('set_theme_mode', { mode: val });
    showToast('Theme mode updated to ' + val);
  }

  function onSelectAccentColor(color) {
    if (!color) return;
    postAction('set_accent_color', { color: color });
    document.documentElement.style.setProperty('--accent', color);
    const txt = document.getElementById('activeAccentText');
    if (txt) { txt.innerText = color; txt.style.color = color; }
    document.querySelectorAll('.accent-swatch').forEach(sw => {
      const onclickAttr = sw.getAttribute('onclick') || '';
      sw.classList.toggle('active', onclickAttr.includes(color));
    });
    showToast('Accent tint updated.');
  }

  function onPerformanceChange() {
    const tabSuspension = document.getElementById('tabSuspensionToggle').checked;
    const idleMinutes = parseInt(document.getElementById('idleTimeoutSelect').value, 10);
    const mouseGestures = document.getElementById('mouseGesturesToggle').checked;
    const trackpadSwipe = document.getElementById('trackpadSwipeToggle').checked;

    postAction('set_tab_suspension', { enabled: tabSuspension, minutes: idleMinutes });
    postAction('set_gestures', { mouse: mouseGestures, trackpad: trackpadSwipe });
    showToast('Performance & Gesture preferences saved.');
  }

  function filterShortcuts(query) {
    const q = query.toLowerCase();
    const rows = document.querySelectorAll('#shortcutsListBody tr');
    rows.forEach(r => {
      const text = r.innerText.toLowerCase();
      r.style.display = text.includes(q) ? '' : 'none';
    });
  }

  let activeRecordingPill = null;
  function recordShortcut(actionId, elem) {
    if (activeRecordingPill) {
      activeRecordingPill.classList.remove('recording');
      activeRecordingPill.innerText = activeRecordingPill.dataset.currentKey || activeRecordingPill.innerText;
    }

    activeRecordingPill = elem;
    elem.dataset.currentKey = elem.innerText;
    elem.innerText = 'Press keys...';
    elem.classList.add('recording');
    document.getElementById('shortcutConflict').style.display = 'none';

    function handleKey(e) {
      e.preventDefault();
      e.stopPropagation();

      if (['Control', 'Shift', 'Alt', 'Meta'].includes(e.key)) return;

      const parts = [];
      if (e.ctrlKey) parts.push('Ctrl');
      if (e.shiftKey) parts.push('Shift');
      if (e.altKey) parts.push('Alt');

      let k = e.key.toUpperCase();
      if (k === ' ') k = 'Space';
      parts.push(k);

      const combo = parts.join('+');

      // Collision detection
      let conflictAction = null;
      document.querySelectorAll('#shortcutsListBody tr').forEach(r => {
        if (r.dataset.action !== actionId) {
          const pill = r.querySelector('.key-pill');
          if (pill && pill.innerText.toLowerCase() === combo.toLowerCase()) {
            conflictAction = r.dataset.name;
          }
        }
      });

      if (conflictAction) {
        const conflictBox = document.getElementById('shortcutConflict');
        conflictBox.innerText = `Shortcut conflict: ${combo} is already assigned to "${conflictAction}".`;
        conflictBox.style.display = 'block';
        elem.classList.remove('recording');
        elem.innerText = elem.dataset.currentKey;
        window.removeEventListener('keydown', handleKey, true);
        activeRecordingPill = null;
        return;
      }

      elem.innerText = combo;
      elem.dataset.currentKey = combo;
      elem.classList.remove('recording');
      window.removeEventListener('keydown', handleKey, true);
      activeRecordingPill = null;

      postAction('set_custom_shortcut', { actionId: actionId, shortcut: combo });
      showToast(`Rebound ${actionId} to ${combo}`);
    }

    window.addEventListener('keydown', handleKey, { capture: true, once: true });
  }

  function resetAllShortcuts() {
    postAction('reset_custom_shortcuts');
    showToast('Reset all shortcuts to defaults.');
    setTimeout(() => location.reload(), 400);
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
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Cannot Reach Site - Browy</title>
<style>
  :root {
    --bg-main: #0b0c0e;
    --bg-card: #141619;
    --border-color: rgba(255, 255, 255, 0.07);
    --border-hover: rgba(255, 255, 255, 0.16);
    --text-primary: #f4f4f5;
    --text-secondary: #a1a1aa;
    --text-muted: #71717a;
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
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100vh;
    padding: 24px;
    margin: 0;
  }

  .card {
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 32px 36px;
    max-width: 480px;
    width: 100%;
    text-align: center;
    box-shadow: 0 16px 40px rgba(0, 0, 0, 0.5), inset 0 1px 0 rgba(255, 255, 255, 0.03);
  }

  .icon-wrap {
    width: 44px;
    height: 44px;
    border-radius: 8px;
    background: rgba(239, 68, 68, 0.1);
    border: 1px solid rgba(239, 68, 68, 0.2);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    margin-bottom: 16px;
    color: #ef4444;
  }

  .icon-wrap svg {
    width: 22px;
    height: 22px;
    stroke: currentColor;
    stroke-width: 1.75;
    fill: none;
  }

  h1 {
    font-size: 18px;
    font-weight: 600;
    letter-spacing: -0.01em;
    margin-bottom: 8px;
    color: var(--text-primary);
  }

  .url {
    color: var(--text-muted);
    font-size: 12.5px;
    font-family: var(--font-mono);
    word-break: break-all;
    margin-bottom: 12px;
    padding: 6px 10px;
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid var(--border-color);
    border-radius: 6px;
  }

  .status {
    display: inline-block;
    background: rgba(239, 68, 68, 0.1);
    color: #fca5a5;
    font-size: 11px;
    font-family: var(--font-mono);
    padding: 2px 8px;
    border-radius: 4px;
    border: 1px solid rgba(239, 68, 68, 0.2);
    margin-bottom: 18px;
  }

  p {
    font-size: 12.5px;
    color: var(--text-secondary);
    line-height: 1.5;
    margin-bottom: 24px;
  }

  .actions {
    display: flex;
    gap: 8px;
    justify-content: center;
  }

  .btn {
    padding: 7px 16px;
    border-radius: 6px;
    font-size: 12.5px;
    font-weight: 500;
    font-family: inherit;
    cursor: pointer;
    border: 1px solid transparent;
    -webkit-tap-highlight-color: transparent;
    transition-property: background-color, border-color, color, transform;
    transition-duration: 140ms;
    transition-timing-function: cubic-bezier(0.2, 0, 0, 1);
  }

  .btn:not(:disabled):active {
    transform: scale(0.96);
  }

  .btn-primary {
    background: #f4f4f5;
    color: #0b0c0e;
  }

  @media (hover: hover) {
    .btn-primary:hover {
      background: #ffffff;
    }
  }

  .btn-secondary {
    background: transparent;
    border-color: var(--border-color);
    color: var(--text-primary);
  }

  @media (hover: hover) {
    .btn-secondary:hover {
      background: rgba(255, 255, 255, 0.05);
      border-color: var(--border-hover);
    }
  }

  @media (prefers-reduced-motion: reduce) {
    *, *::before, *::after {
      transition-duration: 0.001ms !important;
      transform: none !important;
    }
  }
</style>
</head>
<body>
  <div class="card">
    <div class="icon-wrap">
      <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>
    </div>
    <h1>This site can’t be reached</h1>
    <div class="url">{{failedUrl}}</div>
    <div class="status">{{errorStatus}}</div>
    <p>Check your internet connection, verify the address, or try searching for the page.</p>
    <div class="actions">
      <button class="btn btn-primary" onclick="location.reload()">Try Again</button>
      <button class="btn btn-secondary" onclick="window.location.href='https://www.google.com/search?q=' + encodeURIComponent('{{failedUrl}}')">Search Google</button>
    </div>
  </div>
</body>
</html>
""";
        }
    }
}
