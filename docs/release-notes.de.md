# 2.6.0 Preview + PeakMP3 0.16.0

## Änderungen

Gemeinsames Setup für YouTube Downloader 2.6.0 Preview und PeakMP3 0.16.0. Einfaches MP3 laden oder Original-AAC ohne Autoplay an die PeakMP3-Warteschlange übergeben. RU/DE/EN-Webseite in Creme, Holz und Metall mit separater lautloser Bedienungsdemo.

## Kompatibilität

Verbindliches Ziel: Windows 7 SP1, 32 Bit, SSE2, .NET 3.5 / CLR2; CLR4-Fallback auf neueren Windows-Versionen. Lokale Prüfungen liefen unter Windows 11. Nutzerbericht: aktuelles Chrome auf Windows 11 sowie kompatibles Yandex/Chrome (ungefähr 109) auf Windows 7. Exakte Builds und vollständige Szenarien sind nicht dokumentiert. Windows 10 wurde nicht separat geprüft. Siehe CHECKS-260.md.

## Installation

Gemeinsames Setup.exe ohne Administratorrechte starten oder vollständiges ZIP entpacken. PeakMP3 und aktive Downloads vor Updates schließen. Die entpackte Erweiterung mit dem Assistenten in jedem Chrome-, Edge- oder Yandex-Profil verbinden. Bestehende ID, Einstellungen und Audiocache bleiben erhalten.

## Einschränkungen

Preview: attestation required bleibt ungelöst. Wiedergabe startet nicht automatisch. Vollständige Live-Browser-/Formatmatrix, Windows 10 und ein eigener Windows-7-Test sind ungeprüft. Synthetische Tests bestätigen weder Musik-Klangqualität noch exakte AIMP-VU-Übereinstimmung. DRM, Vimeo, andere Webseiten, Livestreams und Playlists sind nicht enthalten. EXE-Dateien sind unsigniert.

## Prüfsummen

Exakte Größen und SHA-256 stehen in public-release.json und SHA256SUMS.txt dieses Releases. Das vollständige ZIP enthält Programme, Anwendungsquellen, FFmpeg/FFprobe, passende Komponentenquellen, Lizenzen und Offline-Anleitungen.
