# Browy - Native Windows 11 Acrylic Browser

**Browy** is a modern, lightweight native Windows 11 browser built with .NET 9, WPF, and Windows Evergreen **WebView2**, featuring a semi-transparent Windows 11 Desktop Acrylic / Mica backdrop, Arc/Zen-inspired vertical sidebar tabs, floating rounded web cards, and a smart omnibox.

---

## ✨ Features

- **Windows 11 DWM Acrylic & Mica Backdrop**:
  - Native hardware-accelerated frosted glass blur using `dwmapi.dll` (`DwmSetWindowAttribute` with `DWMSBT_TRANSIENTWINDOW` and `DWMSBT_TABBEDWINDOW`).
  - Interactive backdrop switcher (Acrylic 💧 / Mica Alt 💎 / Mica 🌌).
  - Immersive Dark Mode.
- **Arc / Zen Inspired Vertical Sidebar Tabs**:
  - Sleek collapsible translucent sidebar.
  - Multi-tab management with individual persistent web instances.
  - Live tab titles, favicons, active indicator cards, and quick close buttons.
  - New tab creation (`+`) and compact mode toggle.
- **Floating Rounded Web View Card**:
  - The web content floats in an elegant card with `CornerRadius="14"`, subtle glass border, and soft drop shadow.
  - Native Windows Evergreen WebView2 engine (no standalone bloated Chromium/Firefox download needed).
  - Fast, secure, full HTML5, WebGL, WebGPU, and modern web API support.
- **Smart Omnibox & Navigation**:
  - Navigation controls: Back (`◀`), Forward (`▶`), Reload/Stop (`↻`/`✕`), Home (`⌂`).
  - Omnibox with SSL security indicator (`🔒`), smart URL vs. Google search auto-detection.
  - Pin / Bookmark bar with quick-launch shortcuts (X, YouTube, Vercel, GitHub, Cloudflare, Linear, MDN).
  - Integrated Developer Tools (`F12` / `⚡`).

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `Ctrl + T` | Open new tab |
| `Ctrl + W` | Close active tab |
| `Ctrl + Tab` | Switch to next tab |
| `Ctrl + L` / `Alt + D` | Focus Omnibox address bar |
| `Ctrl + R` / `F5` | Reload page |
| `F12` | Toggle Developer Tools (Inspect Elements, Console, Network) |
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
