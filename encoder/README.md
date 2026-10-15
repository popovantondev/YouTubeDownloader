# Minimal Windows x86 media tools

FFmpeg/FFprobe 5.1.6 and LAME 3.100, static, MSVCRT, Windows 7 baseline and SSE2.
GPL, version3 and nonfree are not enabled. FFmpeg reports LGPL 2.1 or later.
No network protocols; no video encoding. Audio conversion is MP3 only.
H.264/VP9 decoding is present for checking files; AV1 is parsed and copied without
decoding. Full AV1 decode verification requires a separate development decoder.

## Rebuild

Use an isolated MSYS2 build shell with make, pkg-config, diffutils/cmp, tar, sed,
awk and coreutils. Extract the pinned llvm-mingw MSVCRT host package named in
PROVENANCE.json beside build-minimal.sh. The i686 cross compiler is selected.
Run `bash build-minimal.sh`. No administrator rights are needed for the program.
Compiler and build tools are development prerequisites, not application runtime.
The source archives are unmodified and verified by SHA-256 in the build script.
The script records the compiler, configuration and binary version. Its own narrow
license is LGPL-2.1-or-later; the application rights do not change.
