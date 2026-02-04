# GitHub Copilot Instructions — VideoScheduler (WPF)

You are assisting on a **portfolio-grade WPF application**. Prioritize maintainability, testability, and WPF best practices over shortcuts.

## Source of truth
- Read and follow **AGENT.md** at repo root.
- Read and follow all **ADRs** in `docs/adr/*.md`.
- If any instruction conflicts, follow ADRs first, then AGENT.md.

## Core architecture (do not change)
- Solution layering:
  - `VideoScheduler.Domain` = pure domain models (no WPF, no IO)
  - `VideoScheduler.Application` = interfaces + use cases (depends on Domain)
  - `VideoScheduler.Infrastructure` = filesystem/OS/media implementations (depends on Application + Domain)
  - `VideoScheduler.Presentation.Wpf` = WPF UI (depends on others)
- No cross-layer back-references.

## MVVM rules (strict)
- Use `CommunityToolkit.Mvvm`:
  - ViewModels inherit `ObservableObject`
  - Prefer `[ObservableProperty]` and `[RelayCommand]`
- Views contain **no business logic**.
- ViewModels must **never** reference Views or UI types.
- Avoid code-behind except trivial wiring (InitializeComponent / DI constructor assignment).

## Navigation rules
- App uses a **Shell** pattern:
  - `ShellViewModel` exposes `CurrentViewModel`
  - `ShellView` hosts a `ContentControl` bound to `CurrentViewModel`
- Navigation is via `INavigationService` which swaps ViewModels.
- Views are resolved using **WPF DataTemplates** (VM → View). No view locators.
- Support lifecycle via `INavigationAware` (`OnNavigatedToAsync` / `OnNavigatedFromAsync`) and cancellation.

## Media rules (critical)
- Use **Microsoft Media Foundation** for media metadata and thumbnails.
- Do NOT introduce ffmpeg/ffprobe or other external media stacks unless an ADR explicitly changes this.
- Encapsulate Media Foundation behind interfaces in the Application layer; implementation lives in Infrastructure.
- Ensure MF lifetime is handled correctly (startup once / shutdown on exit).

## UI & performance expectations
- Split styles and templates into ResourceDictionaries (Colors, Typography, Controls, Templates).
- Prefer DataTemplates, converters only when needed, minimal code-behind.
- Enable virtualization for large lists (`VirtualizingStackPanel`, recycling).
- Never block the UI thread: use async/await, cancellation tokens, and throttling (SemaphoreSlim) for IO-heavy tasks.
- WPF images assigned from background work must be `Freeze()`’d before cross-thread use.

## Coding style
- Keep classes small and single-purpose.
- Prefer explicit interfaces and DI registration over statics/singletons.
- Include cancellation tokens for long-running work.
- Fail gracefully for unsupported media formats (placeholders, error states).

## When you respond
- If a change affects architecture, mention which ADR/AGENT rule it satisfies.
- If you propose something new, also propose an ADR entry (title + decision + rationale).

## Building and testing

### Local development setup
- **Prerequisites**: .NET 9.0 SDK, Windows OS (for WPF)
- **Restore**: `dotnet restore VideoScheduler.sln`
- **Build**: `dotnet build VideoScheduler.sln --configuration Debug`
- **Run WPF app**: `dotnet run --project VideoScheduler.Presentation.WPF`

### Running tests
- **All tests**: `dotnet test VideoScheduler.sln --verbosity normal`
- **With coverage**: `dotnet test VideoScheduler.sln --collect:"XPlat Code Coverage"`
- **Domain only**: `dotnet test VideoScheduler.Domain.Tests/VideoScheduler.Domain.Tests.csproj`
- **Application only**: `dotnet test VideoScheduler.Application.Tests/VideoScheduler.Application.Tests.csproj`

### Testing framework
- **xUnit** for test framework
- **FluentAssertions** for assertions
- **NSubstitute** for mocking
- Tests must follow AAA pattern (Arrange, Act, Assert)
- See `docs/CI.md` for detailed testing and CI/CD information

### Before committing
1. Run `dotnet build VideoScheduler.sln` to ensure no build errors
2. Run `dotnet test VideoScheduler.sln` to ensure all tests pass
3. Review changes to ensure minimal, focused modifications
