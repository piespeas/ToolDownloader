# acousticvoid downloader

A one-click Windows downloader for a curated DFIR and security tool set. Built with .NET 10 WinForms and published as a self-contained single-file executable.

![acousticvoid downloader](docs/preview.png)

## Download

- **Recommended:** [latest GitHub release](../../releases/latest)
- **From this repo:** [`dist/AcousticVoidDownloader.exe`](dist/AcousticVoidDownloader.exe) — version 1.3.0, win-x64
- Verify the download against [`dist/SHA256SUMS.txt`](dist/SHA256SUMS.txt).

The app is self-contained: no .NET runtime installation is required on the target machine.

## Requirements

- Windows 10 or 11, x64
- Administrator approval (UAC). The app requests elevation for the optional Windows Defender exclusion and for writing to `C:\ssN`.

## Usage

1. Launch `AcousticVoidDownloader.exe` and approve the UAC prompt.
2. Check the destination under **OUTPUT**. The app suggests the next free `C:\ssN`; **change** opens a folder picker.
3. Leave the Defender exclusion on unless you do not want it.
4. Press **DOWNLOAD ALL TOOLS**. The button shows live progress. Closing the window during a run asks whether to stop it.

Files are organised into one folder per catalog group. Existing files are never overwritten; duplicates are saved as `name_2.ext`.

## Catalog

90 tools across seven groups:

| Group | Tools |
|---|---:|
| Orbdiff | 15 |
| Tonynoh | 3 |
| Spokwn | 11 |
| Nirsoft | 24 |
| Generic Tools | 22 |
| Eric Zimmerman | 14 |
| Detect | 1 |

The catalog lives in [`Catalog.cs`](Catalog.cs). Add or remove URLs there and rebuild.

## Features

- Single **DOWNLOAD ALL TOOLS** action; no group picker.
- Groups run in order, files inside a group download six at a time.
- Files ≥ 8 MB with `Accept-Ranges: bytes` are pulled as four ranged requests into one preallocated file.
- Three attempts per file, with rollback of partial files between attempts.
- Automatic `.zip` extraction into a folder named after the archive, then the archive is deleted.
- Unique name reservation (`name_2.ext`) so nothing is silently overwritten.
- Optional Windows Defender exclusion for the destination folder.
- Output defaults to the next free `C:\ss1`, `C:\ss2`, …; a custom folder is remembered.
- Settings live in `%LOCALAPPDATA%\acousticvoid\downloader\settings.json`.
- Minimal center-stage window with live progress in the button label.

## Build

Requires the .NET SDK 10 (or newer) on Windows.

```powershell
.\build.ps1
```

Output: `dist\AcousticVoidDownloader.exe` — self-contained, single file, win-x64. The build also writes `dist\SHA256SUMS.txt`.

Dev loop:

```powershell
dotnet run --project .\AcousticVoidDownloader.csproj
```

## Publishing a release

The included GitHub Actions workflows build the app on every push and attach the executable to a release when a tag is pushed. To publish version 1.3.0:

```powershell
git tag v1.3.0
git push origin v1.3.0
```

The release workflow creates the GitHub Release and uploads `AcousticVoidDownloader.exe` and `SHA256SUMS.txt`. You can also upload the local files from `dist\` manually.

## Project layout

| File | Role |
|---|---|
| `Catalog.cs` | Tool groups and URLs — edit here to add or remove tools |
| `DownloadEngine.cs` | HTTP engine, range segments, retries, zip extraction |
| `MainForm.cs` | UI, one-click orchestration, settings |
| `Controls.cs` | Magnifier mark, textured hero panel, and checkbox |
| `Theme.cs` | Monochrome palette, fonts, button factory |
| `app.manifest` | `requireAdministrator`, PerMonitorV2 DPI |
| `build.ps1` | Single-file publish to `dist\` |
| `.github/workflows/` | CI build and tagged release automation |

## Notes

- The manifest requests elevation. `Add-MpPreference` and writes to `C:\ssN` need it; the Defender exclusion is skipped if the process is not elevated.
- Group downloads are sequential; files inside a group run six at a time. Each segmented file opens up to four additional connections.
- The engine resolves the final filename from `Content-Disposition`, then the redirect target, then the URL path. Archives keep their `.zip` name only until extraction finishes.
- Failures are counted and named in the completion dialog at the end of a run.
- This project does not host or bundle third-party tools. It downloads them from their official project URLs. All tools remain the property of their respective authors.
- Some downloaded tools may trigger antivirus alerts. Review each tool and use the collection only on systems you own or are authorised to examine.

## License

[MIT](LICENSE) © 2026 acousticvoid
