# Browy - Native Browser

**Browy** is a modern, lightweight native Windows 11 browser built with .NET 9, WPF, and Windows Evergreen **WebView2**, featuring acrylic panels, Arc/Zen-inspired vertical sidebar tabs, floating rounded web cards, and a smart omnibox.

## Keyboard Shortcuts

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
