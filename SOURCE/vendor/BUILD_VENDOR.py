"""Optional developer build. Runtime needs neither Python nor esbuild."""
from pathlib import Path
import sys,tarfile,subprocess,shutil
if len(sys.argv)!=2:raise SystemExit('Usage: python BUILD_VENDOR.py path/to/esbuild.exe')
root=Path(__file__).resolve().parent;build=root/'build-tree';build.mkdir(exist_ok=True)
for name,target in [('googlevideo-4.1.1.tgz','googlevideo'),('bufbuild_protobuf-2.12.0.tgz','@bufbuild/protobuf')]:
 destination=build/'node_modules'/target
 with tarfile.open(root/name,'r:gz') as archive:
  for member in archive:
   if not member.isfile():continue
   relative=Path(*Path(member.name).parts[1:])
   if relative.is_absolute() or '..' in relative.parts:raise ValueError('Unsafe archive path')
   path=destination/relative;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(archive.extractfile(member).read())
shutil.copyfile(root/'bridge.js',build/'bridge.js')
output=root.parents[1]/'extension/vendor/googlevideo.js'
subprocess.run([str(Path(sys.argv[1]).resolve()),str(build/'bridge.js'),'--bundle','--format=iife','--global-name=YTWin7Sabr','--target=chrome109','--minify','--legal-comments=eof','--outfile='+str(output)],check=True)
print(output)
