# Bundled SABR dependencies

Runtime imports only extension/vendor/googlevideo.js. No npm, Node, Python,
esbuild, or remote JavaScript is needed on the Windows 7 installation.

Pinned npm archives contain the distributed library code, source maps/types,
and generated protocol definitions. SOURCE/vendor/bridge.js contains our adapter
and compatibility fixes; upstream archives are unchanged. Integrity/provenance is
in PROVENANCE.json. MIT, Apache and BSD notices are included in this folder.

To rebuild, run BUILD_VENDOR.py with an esbuild executable (0.25.12 used here).
This is an optional developer step requiring a suitable development computer.
The script extracts the pinned archives to a build-tree and emits the Chrome109
IIFE bundle. Do not add build-tree to a distributed runtime kit.
