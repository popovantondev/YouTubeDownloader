# Verification record — 2.5.0 Preview

Checks performed on Windows 11; dated 2026-10-15.

- Chrome/Edge: real extension popup in RU/DE/EN, light/dark, keyboard Tab, defaults, rapid checkbox changes, persistence and live page updates. Content-script metadata for UI tests was synthetic.

- Actual HKCU native registration: both browsers launched the unmodified release EXE and received a valid lookup response. The fixed extension ID and allowed_origins matched.

- Actual WSH installer in isolated LocalAppData: upgrade from 2.4.2, complete media-tool backup, restore, reinstall and integration-only uninstall; cache sentinel survived. Existing registry values were restored after this test.

- Local media fixtures: 144/240/360/480/720/1080p H.264, VP9 and AV1 packet-preserving copy; AAC/Opus packet identity; cold/cached AAC and MP3 320 conversion; duration, height, codec, missing/truncated segments and cancellation cleanup. Fixtures were generated and fully decoded with a separate full FFmpeg; the released minimal tools were used by the tested host.

- SABR fixtures: split UMP prefixes, multibyte IDs, repeated init, tick timing, preserved request context, bounded buffering and exact track bytes. Retry tests: same resolution only, at most three attempts; cancellation prevents a delayed restart. Slow-page test waits 150 seconds and starts only when usable.

- Live Edge 480p (lyuFLU2Zqz0): some media arrived, then the server required attestation. Download did not complete. The video remained paused at exactly 5.51346 seconds before and after. This is a recorded failure, not a successful media test.

- Not verified: a clean Windows 7 installation of 2.5.0, Windows 10, current Yandex, and complete live downloads of all six target formats. 2.4.2 reports are historical evidence only.

- Website: 24 combinations of RU/DE/EN, landing/guide, light/dark and desktop/mobile viewport passed resource loading and no-horizontal-overflow checks; representative screenshots were visually reviewed.
- Active capture preference test: hiding every original format leaves the task running and the cancel control available.
- All 17 produced fixture files were fully decoded independently with no errors.
