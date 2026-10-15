# 2.5.0 Preview

## Änderungen

Neuer Name und grünes Symbol; Einstellungen nur über das Browsersymbol mit Formatschaltern; RU/DE/EN sowie helle/dunkle Darstellung. Eigene Edge-Native-Messaging-Registrierung; x86-CLR2-Komponente und bestehende Pfade erhalten. Minimales FFmpeg/FFprobe 5.1.6 mit passenden Quellarchiven und Lizenzen. Installationssicherung, automatische Fehlerwiederherstellung und manuelle Rückkehr. Standard bleibt ohne Wiedergabe; kein automatischer Wiedergabewechsel.

## Kompatibilität und Tests
2.4.2: Nutzer bestätigte Funktion auf zurückgesetztem Windows 7 mit Yandex 24.10.4.850 sowie Windows 11; Dateien wurden unabhängig dekodiert. 2.5.0: Chrome 155.0.8059.80 und Edge 155.0.4283.45 bestanden automatisierte Erweiterungs-/Einstellungstests mit künstlichen Videometadaten. Native Tests: docs/TEST_RESULTS.md. Saubere 2.5.0-Installation, Windows 10 und aktuelles Yandex sind noch nicht unabhängig geprüft.

## Installation
Gesamtes Release-ZIP in einen beschreibbaren Ordner entpacken. SHA256SUMS.txt prüfen. Browser, Windows und System-.NET werden separat vorausgesetzt.
YouTube-Tabs schließen und Install.cmd normal ohne Administratorrechte starten. Optionale Sprache: Install.cmd de, ru oder en.
edge://extensions, chrome://extensions oder browser://extensions öffnen. Entwicklermodus aktivieren und „Entpackte Erweiterung laden“ wählen.
%LOCALAPPDATA%\YTWin7_360_MP3\extension auswählen. Bei Updates Erweiterung und YouTube-Seite neu laden.
Grünen Pfeil in der Browserleiste anheften. Darüber Sprache, Darstellung und sichtbare Formate wählen. Zuerst 480p versuchen.

## Einschränkungen
Preview: YouTube kann Protokolle ändern oder Streams ablehnen. Auswahl 144–1080p; nur verfügbare Qualitäten erscheinen. Livestreams, Playlists, DRM, Vimeo und andere Seiten werden nicht unterstützt. Signierte Adressen können ablaufen; eigenständige Token-Erneuerung fehlt noch. MP3 320 verbessert die Quelle nicht. EXE unsigniert.

## SHA-256
SHA256SUMS.txt
