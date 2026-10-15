# Building and checking a release

The runtime ZIP is the ready-to-install package. The source ZIP requires compilation;
FFmpeg/FFprobe are supplied by the corresponding-sources build or the reviewed runtime.

1. Build the x86 host with BUILD_HOST.cmd (.NET 3.5 compiler required for this build path).
2. Build the media tools as described in encoder/README.md, using the pinned inputs.
3. Rebuild the bundled browser dependency with SOURCE/vendor/BUILD_VENDOR.py if it changed.
4. Run Python/JavaScript tests from SOURCE/tests. Media fixtures require a separate full
   FFmpeg with the test encoders and lavfi, supplied through YTD_FIXTURE_FFMPEG. That
   fixture generator is not the executable used by the native component under test.
5. Run `python tools/check_public_docs.py --write`, review the output, then run it without
   --write. Tests do not establish fresh-machine or every-browser compatibility.
6. Run `python tools/build_release.py path/to/output`. Only the explicit application
   file/folder lists are packaged. Every ZIP has an internal SHA256SUMS.txt. The full
   ZIP contains tools/corresponding-sources.zip as well as all runtime binaries.
7. Publish the three ZIPs and external SHA256SUMS.txt. The repository public-release.json
   stores their final sizes/hashes. ZIP declarations list names/URLs before hashes are
   computed to avoid a self-referential archive hash.
8. Download published files and run `python tools/check_public_docs.py --online --published`.

The minimal media tools can copy AV1 but do not decode it; use a full independent
FFmpeg for complete AV1 decoding during verification. Do not add runtime prerequisites
from unrelated programs. The native component targets .NET 3.5 / CLR2 with CLR4 fallback.
