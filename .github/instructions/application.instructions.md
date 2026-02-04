---
applyTo: "VideoScheduler.Application/**/*.cs"
---

# Application Layer Instructions

## Purpose
The Application layer defines **interfaces** and **use cases** that orchestrate domain logic and external services.

## Rules
- **Define interfaces** for services implemented in Infrastructure
- Use **DTOs (Data Transfer Objects)** as records for data passed between layers
- All I/O operations must be **async with cancellation tokens**
- **No concrete implementations** of external services (those belong in Infrastructure)
- **No UI or WPF concerns**
- Use `IAsyncEnumerable<T>` for streaming large result sets

## Patterns
- Service interfaces define contracts: `IVideoMetadataService`, `IFileSystemService`
- DTOs use **records** for immutability and value semantics
- Use cases return **Result types or DTOs**, never domain entities directly
- Include **CancellationToken** parameters for async methods
- Prefer **IAsyncEnumerable** over loading entire lists into memory

## Example
```csharp
// Interface definition
public interface IVideoMetadataService
{
    Task<VideoMetadataDto?> GetMetadataAsync(string filePath, CancellationToken cancellationToken = default);
}

// DTO as record
public record VideoMetadataDto(
    TimeSpan Duration,
    string VideoCodec,
    string AudioCodec,
    int Width,
    int Height
);

// Use case returning IAsyncEnumerable
public interface IVideoLibraryService
{
    IAsyncEnumerable<VideoAsset> ScanFolderAsync(string folderPath, CancellationToken cancellationToken = default);
}
```

## What NOT to do
- ❌ Don't implement concrete I/O operations here (use Infrastructure)
- ❌ Don't add WPF dependencies (ObservableObject, commands, etc.)
- ❌ Don't return domain entities directly from use cases - use DTOs
- ❌ Don't forget CancellationToken parameters on async methods
- ❌ Don't block on async calls (.Result, .Wait())
