# 2.6.0 Preview / PeakMP3 0.16.0 — actual checks

Local candidate, 2026-10-19. Not published to GitHub Releases or Pages.
Host: Microsoft Windows 11 Enterprise, 10.0.26200, 64 bit.

## Passed locally

- Four managed executables compile against genuine .NET Framework 3.5 reference
  assemblies, C# 3, x86. Metadata says CLR v2.0.50727. Both media tools are x86.
  Host and PeakMP3 start through their configured runtime on this Windows 11 host.
- Real native AAC receive/remux and original AAC output preserve every AAC packet
  SHA-256. Plain MP3 packets match an unfiltered libmp3lame reference encode;
  the complete MP3 decodes without error.
- Native peakstream accepts AAC, creates one private job, launches a test entry point
  using the actual desktop window/import code and receives its acceptance marker.
  Full display title/language survive. It reports handoff, not a finished processed
  MP3. This check makes no live YouTube request.
- Actual desktop window queues without autoplay, rejects outside-inbox and traversal
  paths, deduplicates repeated delivery. User-only named-pipe delivery to an existing
  window acknowledges import and keeps one queue entry.
- Existing ProcessingEngine exports densities 0, 5 and 10 from synthetic five-second
  AAC, then fully decodes/verifies outputs. This is not a music listening comparison
  or an AIMP calibration certification. Maximum DSP parameters are retained;
  only process/thread scheduling changed in the audio engine.
- Actual float PCM preview pipe, PCM waveform pipe and FLAC decoding work in the new
  minimal FFmpeg 5.1.6 + LAME 3.100. Matching source archives/configuration and binary
  hashes are under encoder; third-party licenses are separate.
- Simulated browser API tests exercise processed/plain separation, AAC versus Opus
  cache handling, title/language transfer, popup launch access, defaults and new
  RU/DE/EN labels. Browser stream test verifies acknowledged chunks of at most
  12288 bytes, exact byte transfer and rejection of other source hosts.
- Receive cancellation exits without a successful new export. All cancellation
  timings and every SABR retry scenario have not been rerun.
- Installer engine installs into an isolated Cyrillic path, preserves extension ID
  and five user-level browser registrations, rejects a modified payload before
  mutation, restores prior files/registration after forced post-copy failure,
  upgrades a real 2.5.0 host and restores its exact prior bytes.
- Compiled Inno Setup TEST variant (separate AppId, no user shortcuts) installs and
  uninstalls on this Windows 11 host. Forced failure returns failure and restores
  old files. Unknown user file/cache survive uninstall. Delivery installer uses the
  same engine/manifest, with normal shortcuts and AppId.
- RU/DE/EN connection-wizard layouts rendered from WinForms and visually inspected.
  This does not exercise browser connection steps in a real profile.

## Still requires a real environment

- Fresh Windows 7 SP1 **32 bit**, .NET 3.5.1, low-powered hardware. This is the mandatory
  target; compilation/PE checks do not establish runtime compatibility.
- Windows 10, a separate standard account, real old/current browser profiles, clean
  extension connection, profile reload and live downloads.
- Complete live 360/480/720/1080/AAC/MP3 matrix, long downloads, all original codec
  retries, cross-tab/browser termination timing and music listening comparisons.
- Prior YouTube attestation-required refusal is unresolved. No automatic playback
  fallback or guaranteed successful original-stream download is claimed.

## Русский

Локально проверены сборка x86/CLR2, сохранность AAC, обычный MP3, передача AAC в окно
PeakMP3, очередь и канал уже открытого окна, экспорт плотностей 0/5/10, аудиовыходы
FFmpeg, установка в папку с кириллицей, отказ при подмене файла, откат, обновление 2.5.0
и удаление с сохранением пользовательских данных. Проверки выполнялись на Windows 11.
Настоящая чистая Windows 7 SP1 **32 бита и слабый ПК ещё не проверены**. Windows 10,
отдельная обычная учётная запись и живые браузерные загрузки также ожидают прогона.
Отказ YouTube attestation required не объявляется исправленным. Синтетический образец
не подтверждает звучание музыки или совпадение VU с AIMP. Публикация не выполнялась.

## Deutsch

Lokal geprüft: x86/CLR2-Build, unverändertes AAC, einfaches MP3, Übergabe ans PeakMP3-
Fenster, Warteschlange/IPC, Export mit Dichte 0/5/10, Audioausgaben, Installation im
kyrillischen Pfad, Erkennung veränderter Dateien, Rollback, Update von 2.5.0 und
Entfernung unter Erhalt eigener Dateien. Ausgeführt auf Windows 11.
Echtes sauberes Windows 7 SP1 **32 Bit und schwache Hardware sind noch ungeprüft**.
Windows 10, separates Standardkonto und Live-Browserdownloads stehen ebenfalls aus.
YouTube attestation required bleibt eine bekannte Grenze. Synthetische Prüftöne sind
kein Musik-Hörvergleich oder Nachweis identischer AIMP-Anzeige. Nicht veröffentlicht.

## User-reported environments — 2026-10-20

The user reports successful operation on Windows 11 with current Google Chrome,
and Windows 7 with its last compatible Yandex and Chrome (approximately 109).
Exact builds and a complete installation/plain MP3/AAC handoff/processed export
matrix were not recorded. These reports do not replace the local checks above;
no Windows 10 result or agent-run Windows 7 test is claimed.

Website: 12 localized pages returned HTTP 200; local assets resolved. RU/DE
navigation, mobile menu at 390px, silent demo play/stop, density and A/B were checked
in a browser. Final design approved by the user; external platter lip cleanup preserves the internal strobe dots. The final installer payload passed isolated install, tamper rejection, rollback, actual 2.5.0 upgrade/restore and compiled Setup/uninstall checks on 2026-10-20.
