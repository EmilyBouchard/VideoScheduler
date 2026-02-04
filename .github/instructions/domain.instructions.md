---
applyTo: "VideoScheduler.Domain/**/*.cs"
---

# Domain Layer Instructions

## Purpose
The Domain layer contains pure business logic and entities with **no external dependencies**.

## Rules
- **No dependencies** on other layers or frameworks
- **No I/O operations** (no file system, no database, no network)
- **No WPF or UI concerns**
- Domain entities are **sealed classes** by convention
- Properties should be **immutable** where possible (init-only setters preferred)
- **Validation in constructors** - fail fast with ArgumentException/ArgumentNullException
- **Explicit null checks** using ArgumentNullException.ThrowIfNull or guard clauses

## Patterns
- Use **value objects** for domain concepts (e.g., duration, file size)
- Keep classes **small and focused** on single responsibility
- Business rules belong **in the entity**, not in services
- Use **explicit rather than implicit** conversions

## Example
```csharp
public sealed class VideoAsset
{
    public string FileName { get; }
    public long SizeInBytes { get; }
    
    public VideoAsset(string fileName, long sizeInBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeInBytes);
        
        FileName = fileName;
        SizeInBytes = sizeInBytes;
    }
}
```

## What NOT to do
- ❌ Don't add dependencies to Application, Infrastructure, or Presentation
- ❌ Don't use async/await (Domain should be synchronous)
- ❌ Don't add attributes from other frameworks (except for serialization if needed)
- ❌ Don't put data access or UI logic in domain classes
