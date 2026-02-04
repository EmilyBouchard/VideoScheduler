# ADR 0002: Navigation Architecture

## Status
Accepted

## Context
The application consists of multiple functional modules:
- Video Library
- Scheduler
- Triggers
- Playback Monitor

Navigation must be:
- Testable
- ViewModel-first
- Decoupled from Views
- Compatible with DI

## Decision
Use a **ShellViewModel** hosting a single `CurrentViewModel`.

Navigation is handled by:
- `INavigationService`
- ViewModel lifecycle hooks (`INavigationAware`)
- WPF DataTemplates to map ViewModels to Views

Views are never referenced directly by navigation code.

## Alternatives Considered
- Code-behind navigation
- View locators
- Frameworks (Prism, Caliburn.Micro)

## Consequences
- Slightly more scaffolding
- Strong modularity
- Easy unit testing
- Predictable navigation behavior
