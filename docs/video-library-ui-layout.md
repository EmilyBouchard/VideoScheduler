# Video Library UI Layout

## Screen Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ [Browse...] [C:\Videos\MyLibrary                            ] [Scan]         │
├───────────────────┬─────────────────────────────────────────────────────────┤
│ Folders           │ Videos                                                  │
│ ┌───────────────┐ │ ┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐    │
│ │ MyVideos      │ │ │  🎬   │ │  🎬   │ │  🎬   │ │  🎬   │ │  🎬   │    │
│ │ > Folder1     │ │ │       │ │       │ │       │ │       │ │       │    │
│ │ > Folder2     │ │ │video1 │ │video2 │ │video3 │ │video4 │ │video5 │    │
│ │ > Folder3     │ │ │       │ │       │ │       │ │       │ │       │    │
│ │ > Archives    │ │ │00:05:30│ │00:12:45│ │00:08:15│ │00:03:20│ │00:15:00│   │
│ │               │ │ │125.4 MB│ │358.2 MB│ │201.8 MB│ │89.5 MB │ │412.6 MB│   │
│ │               │ │ │2/3/2026│ │2/2/2026│ │2/1/2026│ │1/30/26 │ │1/29/26 │   │
│ │               │ │ └───────┘ └───────┘ └───────┘ └───────┘ └───────┘    │
│ │               │ │                                                         │
│ │               │ │ ┌───────┐ ┌───────┐ ┌───────┐                          │
│ │               │ │ │  🎬   │ │  🎬   │ │  🎬   │                          │
│ │               │ │ │       │ │       │ │       │                          │
│ │               │ │ │video6 │ │video7 │ │video8 │                          │
│ │               │ │ │       │ │       │ │       │                          │
│ │               │ │ │00:07:45│ │00:20:10│ │00:04:55│                          │
│ │               │ │ │189.3 MB│ │567.1 MB│ │102.4 MB│                          │
│ │               │ │ │1/28/26 │ │1/27/26 │ │1/26/26 │                          │
│ │               │ │ └───────┘ └───────┘ └───────┘                          │
│ └───────────────┘ │                                                         │
├───────────────────┴─────────────────────────────────────────────────────────┤
│ Found 8 videos. | Loading: False                                            │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Video Card Detail

```
┌──────────────────────┐
│                      │
│       🎬            │  ← Thumbnail placeholder (or actual thumbnail)
│                      │
├──────────────────────┤
│ video_name.mp4       │  ← File name (truncated with ellipsis)
│                      │
│ Duration: 00:05:30   │  ← Video duration from metadata
│ Size: 125.4 MB       │  ← Human-readable file size
│ Modified: 2/3/2026   │  ← Last modified date
└──────────────────────┘
```

## Features

### Top Bar
- **Browse Button**: Opens folder picker dialog
- **Path TextBox**: Shows selected root folder (read-only)
- **Scan Button**: Triggers async scan of the selected folder

### Left Panel (Folders)
- Lists all subdirectories found during scan
- Virtualized for performance
- Clicking a folder updates root path and rescans

### Right Panel (Videos)
- Card-based layout with wrap panel
- Each card shows:
  - Thumbnail area (with placeholder emoji when no thumbnail)
  - File name with tooltip
  - Duration (from metadata service)
  - File size (formatted)
  - Last modified date
- Cards arranged in rows, wrapping as needed
- Virtualized for performance

### Status Bar
- Left: Status messages (scanning, errors, results)
- Right: Loading indicator

## Interaction Flow

1. User clicks "Browse..." button
2. Folder picker dialog appears
3. User selects a folder
4. Path appears in textbox
5. User clicks "Scan" (or it auto-scans)
6. Status shows "Scanning..."
7. Folders populate on the left as they're found
8. Video cards appear on the right as videos are found
9. Thumbnails load asynchronously in background
10. Status shows "Found X videos."

## Responsive Behavior

- Window can be resized
- Cards wrap to fit available width
- Scroll bars appear when content exceeds viewport
- Folders panel has fixed width (280px)
- Videos panel takes remaining space

## Error Handling

- Unsupported video formats: show card with placeholder
- Missing thumbnails: show placeholder emoji
- Missing duration: show "Unknown"
- Folder access errors: silent failure, empty results
- Cancellation: "Scan canceled." message in status bar
