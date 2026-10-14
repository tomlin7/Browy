# Contributing to Browy

Thank you for your interest in contributing to Browy! Browy is a modern, privacy-respecting browser built for Windows with .NET 9 and Microsoft Edge WebView2, featuring Arc/Zen Spaces, Split View, InPrivate mode, Shield Ad-Blocker, and an AI Copilot.

## Development Setup

### Prerequisites
- Windows 10 (version 1809+) or Windows 11
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (pre-installed on modern Windows 10/11)
- Visual Studio 2022, JetBrains Rider, or VS Code with C# DevKit

### Building & Running

1. Clone the repository:
   ```bash
   git clone https://github.com/tomlin7/Browy.git
   cd Browy
   ```

2. Restore and build:
   ```bash
   dotnet restore
   dotnet build -c Debug
   ```

3. Run the automated test suite:
   ```bash
   dotnet test
   ```

4. Launch Browy:
   ```bash
   dotnet run --project src/Browy/Browy.csproj
   # Or run the local launch script:
   .\run.cmd
   ```

## Code Guidelines

- **Clean Architecture**: Keep UI logic in XAML/Code-behind and business/data logic inside `Services` and `Models`.
- **Commit Conventions**: Use [Conventional Commits](https://www.conventionalcommits.org/) (e.g., `feat: ...`, `fix: ...`, `docs: ...`, `test: ...`, `ci: ...`).
- **GPG Signing**: If not using GPG signing locally, use `--no-gpg-sign`.
- **Unit Tests**: Whenever adding non-UI logic or bugfixes, add corresponding test coverage under `tests/Browy.Tests/`.
- **Documentation**: Update `README.md` and `ARCHITECTURE.md` when introducing new user-facing features or internal architectural patterns.

## Submitting Pull Requests

1. Create a descriptive branch: `feature/xyz` or `fix/issue-number`.
2. Ensure `dotnet test` and `dotnet build -c Release` pass cleanly with 0 errors and 0 warnings.
3. Open a Pull Request against `main`. Automated GitHub Actions will verify build and test passes.
