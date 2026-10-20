# 2.6.0 Preview + PeakMP3 0.16.0

## Changes

Combined installer for YouTube Downloader 2.6.0 Preview and PeakMP3 0.16.0. Download plain MP3 or transfer original AAC into the PeakMP3 queue without autoplay. RU/DE/EN website in cream, wood and metal with a separate silent control demonstration.

## Compatibility

Required target: Windows 7 SP1, 32 bit, SSE2, .NET 3.5 / CLR2; CLR4 fallback on newer Windows. Local checks ran on Windows 11. The user reports current Chrome on Windows 11 and compatible Yandex/Chrome (approximately 109) on Windows 7; exact builds and full scenarios were not recorded. Windows 10 is not separately tested. See CHECKS-260.md.

## Installation

Run the combined Setup.exe without administrator rights or extract the complete ZIP. Close PeakMP3 and active downloads before updating. Use the wizard to connect the unpacked extension separately in each Chrome, Edge or Yandex profile. Existing extension ID, settings and audio cache are retained.

## Limitations

Preview: attestation required refusals remain unresolved. Playback does not start automatically. The full live browser/format matrix, Windows 10 and an agent-run Windows 7 test remain unverified. Synthetic tests do not establish music sound quality or exact AIMP VU matching. DRM, Vimeo, other websites, livestreams and playlists are out of scope. Executables are unsigned.

## Checksums

Exact file sizes and SHA-256 values are in public-release.json and this release’s SHA256SUMS.txt. The full ZIP includes programs, application sources, FFmpeg/FFprobe, matching component sources, licenses and offline guides.
