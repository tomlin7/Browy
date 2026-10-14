# Browy - Native Browser

**Browy** is a modern, lightweight native Windows 11 browser built with .NET 9, WPF, and Microsoft Edge Evergreen **WebView2**, featuring Arc/Zen-inspired spaces, dual-pane split view, inprivate stealth browsing, an integrated live AI Copilot, Fluent dark hub pages, and a distraction-free user experience.

## Key Features

- **Arc/Zen Spaces & Workspaces**:
  - Multi-space organization (Personal, Work, Research, etc.) with custom accent colors.
  - Quick-switch space pills at the bottom of the sidebar (`Alt+1..9`, `Alt+Left/Right`).
  - Persistent per-space tab sets and state restored seamlessly on startup.
- **Split View / Dual-Pane Browsing (`Ctrl+\`)**:
  - Compare or browse two web tabs side-by-side.
  - Interactive, draggable center divider with fluid column resizing.
  - Synchronized focus and individual navigation controls.
- **InPrivate / Incognito Stealth Window (`Ctrl+Shift+N`)**:
  - Fully isolated ephemeral WebView2 user data profile.
  - Zero disk trace: temporary cache directory automatically purged on exit.
  - Completely disables history tracking, download logging, and session recording.
  - Discreet InPrivate badge in the Omnibox.
- **Live AI Copilot & Page Intelligence (`Ctrl+J`)**:
  - Extracts live DOM structure, page headings, and readable text directly from the active tab.
  - Contextual page summarization, takeaway extraction, concept explanation, and question answering.
- **Dedicated Internal Hub Pages**:
  - `browy://newtab`: Fluent start page with clock, search bar, and speed dials.
  - `browy://history`: Searchable browsing history with time grouping and deletion.
  - `browy://bookmarks`: Responsive grid of bookmarks with instant search.
  - `browy://downloads`: Download management center with open and show-in-folder actions.
  - `browy://settings`: Browser preferences (startup behavior, default search provider, shield toggles).
- **Browy Shield (Ad & Tracker Blocker)**:
  - Low-latency network filtering blocking intrusive ad networks, telemetry trackers, and analytics.
  - Live blocked counter badge and one-click pause toggle.
- **Distraction-Free Reader Mode (`Ctrl+Shift+R` / `F9`)**:
  - Clean article extraction stripping ads, banners, and clutter.
  - Customizable typography and Dark/Sepia/Light reading themes.
- **Command Palette (`Ctrl+K` / `Ctrl+P`)**:
  - Unified switcher for commands, active open tabs (`/tab`), and bookmarks (`/bm`).

## Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `Ctrl + K` / `Ctrl + Shift + P` | Open Command Palette |
| `Ctrl + \` | Toggle Split View (dual-pane side-by-side) |
| `Ctrl + Shift + N` | Open InPrivate / Incognito Window |
| `Ctrl + J` | Open AI Copilot Sidebar |
| `Alt + 1..9` | Switch to Space 1 through 9 |
| `Alt + Left` / `Alt + Right` | Previous / Next Space |
| `Ctrl + T` | Open new tab |
| `Ctrl + W` | Close active tab |
| `Ctrl + Shift + T` | Reopen recently closed tab |
| `Ctrl + Tab` / `Ctrl + Shift + Tab` | Next / Previous tab |
| `Ctrl + Shift + R` / `F9` | Toggle Distraction-Free Reader Mode |
| `Ctrl + ,` | Open Settings (`browy://settings`) |
| `Ctrl + H` | Toggle History Flyout |
| `Ctrl + Shift + J` | Toggle Downloads Manager |
| `Ctrl + F` | Find in page |
| `Ctrl + L` / `Alt + D` | Focus Omnibox address bar |
| `Ctrl + M` | Toggle mute on active tab |
| `Ctrl + D` | Bookmark active page |
| `Ctrl + +` / `Ctrl + -` / `Ctrl + 0` | Zoom in / Zoom out / Reset zoom (100%) |
| `Ctrl + R` / `F5` | Reload page |
| `Ctrl + F5` | Hard reload (bypass cache) |
| `F11` | Toggle Full Screen mode |
| `F12` | Toggle Developer Tools |

## Building & Testing

### Requirements
- Windows 10 (1809+) or Windows 11
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Microsoft Edge WebView2 Evergreen Runtime

### Run Application
```powershell
dotnet run --project src\Browy\Browy.csproj
# Or run the local launch script:
.\run.cmd
```

### Run Tests
```powershell
dotnet test
```

## Documentation
- [Architecture & Design Guide](ARCHITECTURE.md)
- [Contributing Guidelines](CONTRIBUTING.md)
