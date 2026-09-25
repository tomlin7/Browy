# Browy - Native Windows 11 Acrylic Browser

**Browy** is a modern, lightweight native Windows 11 browser built with .NET 9, WPF, and Windows Evergreen **WebView2**, featuring seamless DWM acrylic panels, Arc/Zen-inspired vertical sidebar tabs, floating rounded web cards, a smart omnibox, and an integrated right-side AI Agents panel.

---

## ✨ Features

- **Windows 11 DWM Desktop Acrylic Backdrop**:
  - Native hardware-accelerated frosted glass blur using `dwmapi.dll` (`DwmSetWindowAttribute`).
  - Seamless, unified transparent styling with zero split divider lines.
  - Immersive Dark Mode.
- **Arc / Zen Inspired Vertical Sidebar Tabs**:
  - Sleek collapsible translucent sidebar with snappy slide animations.
  - Multi-tab management with individual persistent web instances.
  - Live tab titles, favicons, active indicator cards, and quick close buttons.
  - Snappy collapse to horizontal tabs with `Ctrl + S`.
- **AI Agents Panel (Right Sidebar)**:
  - Dedicated wider assistant panel (320px) on the right with seamless acrylic material.
  - Page-aware Copilot: instant summaries, explanations, and insights based on the active tab.
  - Capability suggestion chips, conversation bubble feed, and bottom prompt composer.
  - Snappy slide animations toggleable with `Ctrl + J` or top-nav icon.
- **Floating Rounded Web View Card**:
  - The web content floats in an elegant card with `CornerRadius="14"`, subtle glass border, and soft drop shadow.
  - Native Windows Evergreen WebView2 engine (no standalone bloated Chromium/Firefox download needed).
  - Fast, secure, full HTML5, WebGL, WebGPU, and modern web API support.
- **Smart Omnibox & Navigation**:
  - Navigation controls: Back (`◀`), Forward (`▶`), Reload/Stop (`↻`/`✕`).
  - Omnibox with SSL security indicator (`🔒`), smart URL vs. Google search auto-detection.
  - Bookmark bar with quick-launch shortcuts (X, YouTube, Vercel, GitHub, Cloudflare, Linear, MDN).
  - Top-level floating History flyout (`Ctrl + H`) floating over web content without clipping.
  - Integrated Developer Tools (`F12`).

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `Ctrl + T` | Open new tab |
| `Ctrl + W` | Close active tab |
| `Ctrl + Tab` | Switch to next tab |
| `Ctrl + S` | Toggle Vertical / Horizontal Tab strip |
| `Ctrl + J` | Toggle Right AI Agents Panel |
| `Ctrl + H` | Toggle History Flyout |
| `Ctrl + L` / `Alt + D` | Focus Omnibox address bar |
| `Ctrl + R` / `F5` | Reload page |
| `F12` | Toggle Developer Tools |
| `Alt + Left` | Back in history |
| `Alt + Right` | Forward in history |

---

## 🚀 Building & Running

### Requirements
- Windows 10/11 (Windows 11 recommended for full Acrylic blur effect)
- .NET 9 SDK
- Microsoft Edge WebView2 Evergreen Runtime (pre-installed on Windows 10/11)

### Quick Run
```powershell
dotnet run --project src\Browy\Browy.csproj
```

### Build Solution
```powershell
dotnet build Browy.sln
```
Output binaries are generated in `src\Browy\bin\Debug\net9.0-windows\Browy.exe`.
