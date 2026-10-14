# Browy - Native Browser

**Browy** is a modern, lightweight native Windows 11 browser built with .NET 9, WPF, and Windows Evergreen **WebView2**, featuring acrylic panels, Arc/Zen-inspired vertical sidebar tabs, floating rounded web cards, and a smart omnibox.

## Features

- **Windows 11 Fluent Acrylic Design**: Native desktop acrylic blur, rounded web view cards, and seamless dark mode.
- **Arc/Zen-Inspired Tab Management**:
  - Vertical sidebar tabs and horizontal tab strip (`Ctrl+S`).
  - Drag-and-drop tab reordering.
  - Pinned tabs with compact icon states.
  - Tab audio indicator with one-click mute (`Ctrl+M`).
  - Recently closed tab restore (`Ctrl+Shift+T`).
- **Command Palette (`Ctrl+K` / `Ctrl+P`)**:
  - Unified switcher for commands, active open tabs (`/tab`), and bookmarks (`/bm`).
- **Distraction-Free Reader Mode (`Ctrl+Shift+R` / `F9`)**:
  - Automatic article detection.
  - Clean article extraction stripping ads, banners, and trackers.
  - Customizable typography (Sans, Serif, Monospace), font scaling, and Dark/Sepia/Light reading themes.
- **Browy Shield (Ad & Tracker Blocker)**:
  - Low-latency request filtering blocking intrusive ad networks, telemetry trackers, and analytics.
  - Live blocked counter badge and one-click pause toggle.
- **Fluent Start & New Tab Page (`browy://newtab`)**:
  - Offline-ready, zero latency start page.
  - Integrated search engine selector, digital clock, greeting, and customizable speed dial grid.
- **Persistent Sessions**:
  - Automatically restores open tabs, pinned tabs, and active selection across browser restarts.
- **Native Browser Tools**:
  - Floating Find in Page (`Ctrl+F`) with match counter.
  - Dedicated Downloads Manager (`Ctrl+Shift+J`).
  - Settings page (`browy://settings` or `Ctrl+,`).
  - Zoom indicator pill and site security permissions flyout.

## Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `Ctrl + K` / `Ctrl + Shift + P` | Open Command Palette |
| `Ctrl + ,` | Open Settings |
| `Ctrl + T` | Open new tab |
| `Ctrl + W` | Close active tab |
| `Ctrl + Shift + T` | Reopen recently closed tab |
| `Ctrl + Tab` | Switch to next tab |
| `Ctrl + Shift + Tab` | Switch to previous tab |
| `Ctrl + Shift + R` / `F9` | Toggle Distraction-Free Reader Mode |
| `Ctrl + F5` | Hard reload (bypass cache) |
| `Ctrl + R` / `F5` | Reload page |
| `Ctrl + S` | Toggle Vertical / Horizontal Tab strip |
| `Ctrl + J` | Toggle Right AI Agents Panel |
| `Ctrl + H` | Toggle History Flyout |
| `Ctrl + Shift + J` | Toggle Downloads Manager |
| `Ctrl + F` | Find in page |
| `Ctrl + L` / `Alt + D` | Focus Omnibox address bar |
| `Ctrl + M` | Toggle mute on active tab |
| `Ctrl + D` | Bookmark active page |
| `Ctrl + +` / `Ctrl + -` / `Ctrl + 0` | Zoom in / Zoom out / Reset zoom |
| `F11` | Toggle Full Screen mode |
| `F12` | Toggle Developer Tools |
| `Alt + Left` | Back in history |
| `Alt + Right` | Forward in history |
| `Alt + Home` | Open Start / New Tab Page |

## Building & Running

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
