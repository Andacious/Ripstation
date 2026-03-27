# Ripstation

Ripstation is a PowerShell automation tool for ripping optical discs with [MakeMKV](https://www.makemkv.com/), transcoding with [HandBrakeCLI](https://handbrake.fr/docs/en/latest/cli/cli-options.html), and organizing the output for [Plex](https://www.plex.tv/). It supports title scanning, interactive title selection, movie and TV episode naming, progress reporting, intermediate file cleanup, and optional disc ejection.

## Prerequisites

- **Windows** (x64) — required for disc ejection via `WMPlayer.OCX.7`
- **[PowerShell 7.1+](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows)** (Core edition, x64)
- **[MakeMKV](https://www.makemkv.com/)** — installed to the default location (`${env:ProgramFiles(x86)}\MakeMKV\makemkvcon64.exe`) or provide a custom path
- **[HandBrakeCLI](https://handbrake.fr/downloads2.php)** — installed to the default location (`$env:ProgramFiles\HandBrakeCLI\HandBrakeCLI.exe`) or provide a custom path

## Getting Started

Clone the repository and run `rip.ps1` from the repo root:

```powershell
# Rip a movie
.\rip.ps1 -MediaName "My Movie" -OutputPath "S:\Plex\Media\Movies"

# Rip TV episodes (season 2, episodes 1-4)
.\rip.ps1 -MediaName "My Show" -OutputPath "S:\Plex\Media\TV" -Season 2 -EpisodeStart 1 -EpisodeEnd 4

# Eject the disc when finished
.\rip.ps1 -MediaName "My Movie" -OutputPath "S:\Plex\Media\Movies" -Eject
```

### Parameters

| Parameter           | Default              | Description                                      |
|---------------------|----------------------|--------------------------------------------------|
| `-MediaName`        | *(from disc/title)*  | Name used for output file/folder naming           |
| `-DiskNumber`       | `0`                  | MakeMKV disc index (for multi-drive systems)      |
| `-IntermediatePath`  | `S:\MKV`             | Temporary directory for intermediate MKV files    |
| `-OutputPath`       | `S:\Plex\Media\Movies` | Final output directory                          |
| `-Season`           | `0`                  | Season number (enables TV episode naming)         |
| `-EpisodeStart`     | `0`                  | First episode number                              |
| `-EpisodeEnd`       | `0`                  | Last episode number                               |
| `-Eject`            | *off*                | Eject the disc drive when finished                |
| `-Debug`            | *off*                | Re-import modules and enable debug output         |

## How It Works

1. **Scan** — MakeMKV scans the disc in robot mode and returns title metadata (name, duration, size, chapters). Titles shorter than 10 minutes are ignored.
2. **Select** — If one title is found it is auto-selected; otherwise you are prompted to enter comma-separated title IDs.
3. **Rip** — Each selected title is ripped to an intermediate MKV file via MakeMKV.
4. **Encode** — HandBrakeCLI transcodes the MKV to M4V using the included Plex preset (H.264 NVENC, MP4 container, chapter markers, audio passthrough with AAC fallback).
5. **Organize** — Output files are named automatically:
   - **Movies:** `OutputPath\Movie Name.m4v`
   - **TV:** `OutputPath\Show Name\Season 01\Show Name - s01e05.m4v`
6. **Cleanup** — Intermediate MKV files are deleted after a successful encode.
7. **Eject** *(optional)* — The disc drive is ejected via the `-Eject` switch.

## Project Structure

```
ripstation-1/
├── rip.ps1                        # Main entry point
├── presets/
│   └── Plex.json                  # HandBrake preset (H.264 NVENC, Plex-optimized)
├── src/
│   ├── Andacious.Ripstation.psd1  # Module manifest
│   └── modules/
│       ├── Andacious.Ripstation.psm1            # Core orchestration & classes
│       ├── Andacious.Ripstation.MakeMkv.psm1    # MakeMKV integration
│       ├── Andacious.Ripstation.HandBrake.psm1  # HandBrake integration
│       └── Andacious.Ripstation.Utility.psm1    # File naming & helpers
├── tests/                         # Pester unit tests
│   ├── Andacious.Ripstation.Tests.ps1
│   ├── Andacious.Ripstation.MakeMkv.Tests.ps1
│   ├── Andacious.Ripstation.HandBrake.Tests.ps1
│   └── Andacious.Ripstation.Utility.Tests.ps1
└── docs/
    └── makemkvcon.md              # MakeMKV CLI reference
```

## Exported Functions

| Function                | Description                                           |
|-------------------------|-------------------------------------------------------|
| `Get-DiskAndTitleInfo`  | Scan a disc and return disc/title metadata             |
| `Backup-Title`          | Rip a single title to MKV via MakeMKV                 |
| `Convert-Video`         | Transcode a video file via HandBrakeCLI               |
| `Backup-DiskMedia`      | Full rip-and-encode pipeline for a single title        |
| `Get-TitleFileName`     | Sanitize and title-case a media name                   |
| `Get-MediaFilePath`     | Build the final output path (movie or TV format)       |
| `Open-DiskDrive`        | Eject the optical drive (alias: `eject`)               |

## HandBrake Preset

The included `presets/Plex.json` is tuned for Plex playback:

- **Video:** H.264 via NVENC, quality 20, slow preset, peak-limited 30 fps
- **Audio:** AC3 passthrough primary, AAC stereo secondary; copies common formats (DTS, TrueHD, EAC3, FLAC)
- **Subtitles:** Foreign audio scan with burn-in
- **Container:** MP4 with web optimization and chapter markers
- **Picture:** Auto crop, decomb deinterlace

## Running Tests

Tests use [Pester](https://pester.dev/). Run them from the repo root:

```powershell
Invoke-Pester .\tests\
```