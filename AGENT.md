# AGENT.md – VideoScheduler (WPF / MVVM)

## Project Goal

Build a **professional, portfolio-grade WPF application** for scheduling video playback:

- Playback at specific times
- Playback in response to system or external events
- Strong emphasis on **expert-level WPF architecture**

This project prioritizes:

- Clean separation of concerns
- Long-term maintainability
- MVVM best practices
- Testability
- Performance with large media libraries

This is NOT a quick demo app. Avoid shortcuts.

---

## Technology Stack

- **.NET**: Modern .NET (target .NET 8 WPF unless stated otherwise)
- **UI**: WPF
- **MVVM**: `CommunityToolkit.Mvvm`
- **DI / Lifetime**: `Microsoft.Extensions.Hosting` + `Microsoft.Extensions.DependencyInjection`
- **Navigation**: ViewModel-first navigation via `ContentControl` + DataTemplates
- **Media stack**: Microsoft Media Foundation (not ffmpeg)

---

## Solution Structure

```
VideoScheduler.sln
│
├── VideoScheduler.Domain
│ └── Pure domain models (no WPF, no IO)
│
├── VideoScheduler.Application
│ └── Use cases, interfaces, orchestration
│
├── VideoScheduler.Infrastructure
│ └── IO, Media Foundation, filesystem, OS integrations
│
└── VideoScheduler.Presentation.Wpf
├── Views
├── ViewModels
├── Navigation
├── Services
└── Resources
```

### Dependency Rules

- Domain has no dependencies
- Application depends on Domain
- Infrastructure depends on Application + Domain
- Presentation depends on all others
- **No cross-layer back-references**

---

## Architectural Principles (IMPORTANT)

### MVVM

- Views contain **no business logic**
- ViewModels:
  - Inherit from `ObservableObject`
  - Use `[ObservableProperty]`, `[RelayCommand]`
  - Never reference Views
- Navigation is ViewModel-driven

### Navigation

- `ShellViewModel` hosts `CurrentViewModel`
- `INavigationService` swaps ViewModels
- Views are resolved via **DataTemplates**
- Navigation supports lifecycle:
  - `INavigationAware.OnNavigatedToAsync`
  - `INavigationAware.OnNavigatedFromAsync`
- Navigation service is DI-resolved and testable

### WPF Best Practices

- ResourceDictionaries split by concern:
  - Colors
  - Typography
  - Controls
  - Templates
- DataTemplates keyed by **ViewModel type**
- Virtualization enabled for lists
- Async work never blocks UI thread
- UI updates batched where possible

---

## Current State (Baseline Implemented)

### ✔ Implemented

- Generic Host + DI startup
- ShellView / ShellViewModel
- NavigationService with lifecycle + cancellation
- ViewModel-first navigation using `ContentControl`
- Toolkit MVVM plumbing
- Resource dictionary structure scaffolded
- Placeholder `VideoLibraryViewModel` + View

### ✔ Naming

- `ShellView` / `ShellViewModel` are intentional
- “Shell” = application container, not a feature

---

## Media Handling (DO NOT CHANGE)

### Media Metadata & Thumbnails

- Use **Microsoft Media Foundation**
- Do NOT use ffmpeg / ffprobe
- Abstract MF behind interfaces in Application layer

Expected capabilities:

- Read duration
- Read video/audio codecs
- Extract a video frame for thumbnails
- Gracefully handle unsupported formats

Media Foundation must:

- Be started once per process (`MFStartup`)
- Be shut down on app exit (`MFShutdown`)
- Be encapsulated in Infrastructure layer

---

## Next Planned Features (Priority Order)

1. **Video Library Module**
   
   - Folder picker
   - TreeView (folders)
   - ListView (videos)
   - Thumbnails
   - Duration / codec metadata
   - Async scanning + cancellation
   - Virtualized UI

2. **Scheduler Module**
   
   - Schedule playback at times
   - Bind schedules to video assets

3. **Trigger Module**
   
   - External/system triggers
   - Event-driven playback

4. **Playback Monitor**
   
   - Current/queued playback
   - Diagnostics

---

## What NOT to Do

- ❌ No code-behind logic (except trivial view wiring)
- ❌ No ViewModel → View references
- ❌ No service locators or static singletons
- ❌ No UI logic in Infrastructure
- ❌ No blocking calls on UI thread
- ❌ No hard-coding of Views in navigation

---

## Coding Style Expectations

- Favor clarity over cleverness
- Small, composable classes
- Explicit interfaces for services
- Async/await everywhere IO is involved
- Cancellation tokens supported
- Fail gracefully (especially for media decoding)

---

## Assumptions

- Windows 10 / 11
- Desktop WPF application
- Local video library (filesystem)
- Performance matters with large libraries

---

## Guiding Question for All Changes

> “Would this still make sense if the app doubled in features?”

If the answer is “no”, rethink the approach.

---

## Status

This project is intentionally scaffold-heavy early on.  
Do not rush feature work at the expense of architecture.

When in doubt:

- Preserve MVVM purity
- Preserve testability
- Preserve separation of concerns
