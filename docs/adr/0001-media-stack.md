# ADR 0001: Media Metadata & Thumbnail Extraction

## Status
Accepted

## Context
The application needs to:
- Read video duration
- Identify audio/video codecs
- Extract a preview frame for thumbnails

This must work asynchronously and integrate cleanly with WPF.

## Decision
Use **Microsoft Media Foundation** as the media stack.

Media Foundation will be:
- Encapsulated in the Infrastructure layer
- Accessed via interfaces defined in the Application layer
- Started once per process (MFStartup)
- Shut down on application exit (MFShutdown)

## Alternatives Considered
### ffmpeg / ffprobe
- Pros: extremely broad format support, predictable output
- Cons:
  - External dependency
  - Licensing considerations
  - Requires bundling binaries
  - Less “Windows-native”

### Windows Shell thumbnails only
- Pros: trivial implementation
- Cons:
  - Unreliable metadata
  - No codec details
  - Inconsistent behavior across systems

## Consequences
### Positive
- Native Windows integration
- No third-party binaries
- Leverages system-installed codecs
- Clean alignment with WPF / Windows ecosystem

### Negative
- Format support depends on OS codecs
- More complex interop code
- Requires careful threading and lifetime management

## Notes
Unsupported formats must fail gracefully and display placeholders.
