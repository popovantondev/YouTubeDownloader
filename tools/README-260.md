# Build and checks — combined 2.6.0 Preview

The sibling runtime package contains FFmpeg, FFprobe, matching component sources,
licenses, assets and configuration files. Copy those runtime files into this source
kit before compiling, retaining its SOURCE, Shared, PeakMP3/SOURCE and installer folders.
Use BUILD_WIN7.cmd with the Windows .NET 3.5 compiler, or BUILD_ALL.ps1 with genuine
.NET Framework 3.5 reference assemblies and the .NET 4 compiler. Both paths target
x86/CLR2 and C# 3. The reference assemblies are development inputs, not runtime files.
No NuGet libraries are needed for the managed programs.

Rebuild FFmpeg using encoder/build-minimal.sh and the matching archives/provenance
from the runtime package. The build script pins an i686 MSVCRT toolchain and requires
MSYS make/pkg-config and shell utilities. Set YTD_TOOLCHAIN_PATH if the toolchain is
not beside the script. Third-party source archives are unmodified.

Checks: tests/test_desktop_media.py requires Python 3 and a separate full FFmpeg
fixture generator in YTD_FIXTURE_FFMPEG. It executes the bundled host and media tools.
After that, tests/RUN_DESKTOP_CHECKS.ps1 builds an isolated GUI/IPC harness against
.NET 3.5. node tests/test_extension260.js checks routing with simulated browser APIs.
The tests create .test-artifacts under this source tree and do not prove live
YouTube or Windows 7 compatibility. Installer tests use a separate registry prefix.

Installer source: installer/YouTubeDownloader.iss, Inno Setup 6.7.3. Runtime staging
is .payload with install-files.txt and SHA256SUMS.txt. Use the explicit runtime
manifest; do not include arbitrary work folders. Do not change browser policies or
preferences to force installation. See the offline guide for manual browser setup.

To rebuild the installer after compiling, run tools/BUILD_INSTALLER.ps1 -Compiler
with the full path to Inno Setup 6.7.3 ISCC.exe. It stages only the runtime manifest
and regenerates its checksums. The runtime package must be the parent of SOURCE-KIT.
