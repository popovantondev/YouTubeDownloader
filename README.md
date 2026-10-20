# YouTube Downloader

PeakMP3 now has its own [repository](https://github.com/popovantondev/PeakMP3) and [website](https://popovantondev.github.io/PeakMP3/index-en.html). The reviewed shared installer 2.6.0 still includes PeakMP3 0.16.0; existing release archives are unchanged.

<!-- public-release:start -->
**YouTube Downloader 2.6.0 Preview**

[Download Preview](https://github.com/popovantondev/YouTubeDownloader/releases/tag/v2.6.0) · [User guide](https://popovantondev.github.io/YouTubeDownloader/Guide-en.html) · [Report a problem](https://github.com/popovantondev/YouTubeDownloader/issues/new/choose)
<!-- public-release:end -->

Preview 2.6.0 + PeakMP3 0.16.0

## Install without administrator rights

Run YouTubeDownloader-PeakMP3-2.6.0-Preview-Setup.exe. The installer puts the desktop program, native host, extension files, media tools and offline guides into your user profile. Close PeakMP3 and ongoing downloads before upgrading. Browser connection remains a separate guided action in each browser profile.

## Connect a browser

In the connection wizard choose Chrome, Edge or Yandex and open its extension management page. Enable Developer mode, choose Load unpacked and select the extension folder shown by the wizard. Pin the green arrow. Refresh existing YouTube tabs. The popup should show PeakMP3 connected. Managed browser policies may prohibit unpacked extensions; the installer does not change them.

## Choose how to get MP3

The green MP3 button keeps its normal, unprocessed MP3 action. Its arrow opens two choices: download MP3 without processing, or download AAC and open PeakMP3. The second path copies original AAC to the private inbox, then opens it in PeakMP3 without autoplay. Listen, adjust density and export there. It does not silently produce a finished processed MP3. The save dialog starts at Desktop and lets you choose another location.

## Formats and language

The green browser arrow opens settings. Default visible formats are green quick 360p and MP3, brown original 480p and 720p. Other qualities and original AAC can be enabled there. Language RU/DE/EN and visibility are saved per browser profile. PeakMP3 has its own language selection; an incoming job sets the initial language only on first launch. Downloads default to without playback.

## Windows 7 and weak computers

Windows 7 SP1 32-bit with SSE2 remains a required target. Both managed applications target x86 .NET Framework 3.5 / CLR2; configurations allow CLR4 on newer Windows. FFmpeg is a static x86 MSVCRT build. No .NET 4.8, WebView2 or x64-only runtime is required. System Windows, a compatible browser and a usable .NET runtime are prerequisites. The installer does not enable Windows features. The user reports successful use on Windows 11 with current Chrome and on Windows 7 with the last compatible Yandex and Chrome (reported approximately as 109). Exact versions and complete installation/download/handoff/export scenarios were not recorded. This is a user report, separate from the local automated checks. Windows 10 has not been separately tested. See the checks file.

## Upgrade, recovery and uninstall

The existing extension ID, native host, install path, settings key and audio cache are retained. Setup snapshots the prior files and registrations before replacing them. Restore_previous.cmd restores BACKUP_PRE_260 when present. Uninstall through Windows Apps removes owned program files and registration, retaining settings, audio cache, inbox audio and downloads. Remove the extension separately from each browser profile. Do not manually delete user folders.

## Known limits and checks

YouTube can refuse original streams with attestation required. This candidate does not claim to fix that refusal and never switches to playback automatically. AAC handoff requires an accessible AAC source; Opus is not relabelled AAC. Test results are in docs/CHECKS-260.md. Synthetic media and simulated browser tests do not establish successful live downloads or every-browser compatibility.

## Sources and rights

SOURCE-KIT contains application sources, extension sources, build scripts and checks. Runtime FFmpeg and FFprobe are included. Matching FFmpeg/LAME source archives and build configuration are in encoder, with licenses in licenses. Own code may be inspected and the unmodified release used personally; third-party terms apply separately. See RIGHTS.md and THIRD_PARTY_NOTICES.md.
