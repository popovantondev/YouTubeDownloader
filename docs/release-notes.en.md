# 2.5.0 Preview

## Changes

New name and green icon; toolbar-only settings with per-format switches; RU/DE/EN and light/dark themes. Explicit Edge Native Messaging registration; x86 CLR2 host and existing paths preserved. Minimal FFmpeg/FFprobe 5.1.6 with pinned corresponding sources and licenses. Installer backup, automatic recovery and manual restore. The default remains without playback; no automatic playback fallback.

## Compatibility and checks
2.5.0 on Windows 11: Chrome 155.0.8059.80 and Edge 155.0.4283.45 passed menu/settings tests and actual Native Messaging registration/host communication in isolated browser profiles. A staged current-user install, upgrade from 2.4.2, restore and integration removal passed. Local media tests cover 144–1080p, AAC, Opus and MP3 with packet preservation checks. The live Edge 480p attempt stopped on YouTube session verification; a complete live download is not confirmed in that test. Fresh Windows 7, Windows 10 and current Yandex have not been independently tested with 2.5.0. The earlier 2.4.2 was reported working on reset Windows 7 with Yandex 24.10.4.850 and on Windows 11.

## Installation
Extract the entire release ZIP to a writable folder. Check SHA256SUMS.txt. Browser, Windows and the system .NET runtime are prerequisites.
Close YouTube tabs and run Install.cmd normally, without administrator rights. Optional language: Install.cmd en, ru or de.
Open edge://extensions, chrome://extensions or browser://extensions. Enable Developer mode and choose Load unpacked.
Select %LOCALAPPDATA%\YTWin7_360_MP3\extension. For an existing installation, reload the extension and YouTube page.
Pin the green arrow icon to the toolbar. Click it to choose language, theme and visible formats. Start with 480p.

## Limitations
Preview: YouTube can change delivery protocols or reject a stream. Supported choices are 144–1080p; only available qualities appear. Live streams, playlists, DRM, Vimeo and other sites are not supported. Signed URLs may expire; autonomous token refresh is not implemented. A 320 kbit/s MP3 is not higher-quality than its source. The release is unsigned.

## SHA-256
SHA256SUMS.txt
