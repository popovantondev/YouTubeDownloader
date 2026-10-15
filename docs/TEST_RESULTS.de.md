# Prüfprotokoll — 2.5.0 Preview

Tests unter Windows 11; Stand 15.10.2026.

- Chrome/Edge: echtes Menü RU/DE/EN, helle/dunkle Darstellung, Tab-Taste, Standardwerte, schnelle Schalter, Speicherung und Aktualisierung offener Seiten. Videometadaten im UI-Test waren künstlich.

- Echte HKCU-Registrierung: Beide Browser starteten die unveränderte Release-EXE und erhielten eine gültige lookup-Antwort. Erweiterungs-ID und allowed_origins stimmten überein.

- WSH-Installation mit getrenntem LocalAppData: Upgrade von 2.4.2, vollständige Sicherung der Medienwerkzeuge, Wiederherstellung, Neuinstallation und Entfernen nur der Integration. Cache-Kontrolldatei blieb erhalten; ursprüngliche Registrierung wurde danach zurückgesetzt.

- Lokale Medienmuster: 144/240/360/480/720/1080p H.264, unveränderte VP9-/AV1-Pakete; AAC-/Opus-Paketidentität; AAC aus neuem Stream und Cache, MP3 320; Dauer, Höhe, Codec, fehlende/abgebrochene Segmente und Bereinigung nach Abbruch. Separate vollständige FFmpeg-Werkzeuge erzeugten und dekodierten die Muster; die geprüfte Host-Komponente verwendete die neuen minimalen Release-Werkzeuge.

- SABR-Muster: geteilte UMP-Präfixe, mehrbyte IDs, wiederholte Initialisierung, Tick-Zeiten, erhaltener Kontext, begrenzter Puffer und identische Trackbytes. Maximal drei Versuche gleicher Auflösung; Abbruch verhindert verzögerten Neustart. Der Langsamstarttest wartet 150 Sekunden bis zur Bereitschaft.

- Echter Edge-Versuch mit 480p (lyuFLU2Zqz0): Einige Daten kamen an, dann verlangte der Server Attestation. Kein vollständiger Download. Video blieb davor und danach bei exakt 5,51346 Sekunden pausiert. Dies ist ein protokollierter Fehler, kein erfolgreicher Medientest.

- Nicht geprüft: frische Windows-7-Installation mit 2.5.0, Windows 10, aktuelles Yandex und vollständige echte Downloads aller sechs Formate. Berichte zu 2.4.2 sind nur historische Nachweise.

- Website: 24 Kombinationen aus Sprache, Startseite/Anleitung, Darstellung und Bildschirmgröße bestanden Bildlade- und Breitenprüfungen; repräsentative Screenshots wurden visuell geprüft.
- Während einer Aufnahme können alle Originalformate ausgeblendet werden; Aufgabe und Abbruchbedienung bleiben erhalten.
- Alle 17 erzeugten Mediendateien wurden unabhängig vollständig fehlerfrei dekodiert.
