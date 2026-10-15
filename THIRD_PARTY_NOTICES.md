# Components / Компоненты / Komponenten

| Component | Version | Terms |
|---|---|---|
| FFmpeg / FFprobe | 5.1.6 minimal x86 | LGPL 2.1 or later |
| LAME | 3.100 | LGPL; separate process via FFmpeg |
| googlevideo | 4.1.1 | MIT |
| @bufbuild/protobuf | 2.12.0 | Apache 2.0 and BSD 3-Clause notices |
| MinGW-w64 / winpthreads / LLVM runtime | llvm-mingw 20240619 | Bundled notices in licenses/toolchain |

FFmpeg runs as a separate process. No FFmpeg library is linked into the native
application. Corresponding unmodified sources, exact configuration, hashes and
build script are included under tools in the full release and available separately
as YouTubeDownloader-2.5.0-corresponding-sources.zip on the same release page.
No GPL/version3/nonfree configuration is enabled in the media tools. Libraries
retain their own terms, independently of the original application restrictions.

Русский: соответствующие исходники и лицензии вложены; ограничения собственного
кода не применяются к сторонним компонентам. Немецкий: Passende Quellen und
Lizenztexte sind enthalten; eigene Quelltextbedingungen gelten nicht für Drittcode.

Only encoder/build-minimal.sh has the additional LGPL-2.1-or-later permission.
This narrow exception does not relicense the application.

[FFmpeg terms](https://ffmpeg.org/legal.html) · [Release sources](https://github.com/popovantondev/YouTubeDownloader/releases/tag/v2.5.0)
