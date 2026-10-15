"""Build the reviewed runtime/source ZIPs without copying private workspace files."""
from pathlib import Path
import argparse,hashlib,json,zipfile
ROOT=Path(__file__).resolve().parents[1]
FOLDERS=('extension','SOURCE','encoder','assets','docs','licenses','tools','.github')
FILES=('README.md','RIGHTS.md','THIRD_PARTY_NOTICES.md','public-release.json','BUILD_HOST.cmd','Install.cmd','Uninstall.cmd','Restore_previous.cmd','CHECK_ENVIRONMENT.cmd','CHECK_VIDEO.cmd','VERIFY_PACKAGE.cmd','VERIFY_PACKAGE.ps1','setup.js','diagnose.js','register_sabr_host.js','YTWin7SabrHost.exe.config','index.html','index-en.html','index-ru.html','index-de.html','Guide-en.html','Guide-ru.html','Guide-de.html','site.css','site.js','.nojekyll','.gitignore','.gitattributes')
RUNTIME=('YTWin7SabrHost.exe','YTWin7SabrHost.exe.config','ffmpeg.exe','ffprobe.exe','Install.cmd','Uninstall.cmd','Restore_previous.cmd','CHECK_ENVIRONMENT.cmd','CHECK_VIDEO.cmd','VERIFY_PACKAGE.cmd','VERIFY_PACKAGE.ps1','setup.js','diagnose.js','register_sabr_host.js','README.md','RIGHTS.md','THIRD_PARTY_NOTICES.md','public-release.json','Guide-en.html','Guide-ru.html','Guide-de.html','site.css','site.js')
def digest(b):return hashlib.sha256(b).hexdigest()
def files_under(folder):
 for p in sorted((ROOT/folder).rglob('*')):
  if not p.is_file() or any(n in p.parts for n in ('__pycache__','node_modules','build-tree','.git')) or p.suffix=='.pyc':continue
  yield p.relative_to(ROOT).as_posix(),p.read_bytes()
def write_zip(dest,top,items):
 values=dict(items);values['SHA256SUMS.txt']=(''.join(digest(b)+'  '+n+'\n' for n,b in sorted(values.items()))).encode()
 with zipfile.ZipFile(dest,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as archive:
  for n,b in sorted(values.items()):
   info=zipfile.ZipInfo(top+'/'+n,date_time=(2026,10,15,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED;info.external_attr=0o644<<16;archive.writestr(info,b)
 with zipfile.ZipFile(dest) as archive:assert archive.testzip() is None
 return dest.read_bytes()
def main():
 ap=argparse.ArgumentParser();ap.add_argument('output',type=Path);args=ap.parse_args();out=args.output.resolve();out.mkdir(parents=True,exist_ok=True)
 meta=json.loads((ROOT/'public-release.json').read_text(encoding='utf-8'));version=meta['version'];stem='YouTubeDownloader-'+version
 names=[stem+'-windows-full.zip',stem+'-source.zip',stem+'-corresponding-sources.zip']
 declaration=dict(meta);declaration['assets']=[{'name':n,'url':meta['source_url']+'/releases/download/'+meta['tag']+'/'+n} for n in names+['SHA256SUMS.txt']];declaration['archive_note']='Archive hashes and sizes are published in the repository release declaration and external SHA256SUMS.txt.'
 declaration_bytes=(json.dumps(declaration,ensure_ascii=False,indent=2)+'\n').encode()
 provenance=json.loads((ROOT/'encoder/PROVENANCE.json').read_text())
 for name,expected in provenance['binaries'].items():
  if digest((ROOT/name).read_bytes())!=expected:raise ValueError('Media-tool binary mismatch: '+name)
 for entry in provenance['sources'][:2]:
  if digest((ROOT/'encoder/sources'/entry['name']).read_bytes())!=entry['sha256']:raise ValueError('Component source mismatch')
 corresponding=[]
 for folder in ('encoder','licenses'):
  corresponding+=list(files_under(folder))
 corresponding+=list(files_under('SOURCE/vendor'))
 corresponding+=[('THIRD_PARTY_NOTICES.md',(ROOT/'THIRD_PARTY_NOTICES.md').read_bytes()),('RIGHTS.md',(ROOT/'RIGHTS.md').read_bytes())]
 component_bytes=write_zip(out/names[2],stem+'-corresponding-sources',corresponding)
 source=[]
 for name in FILES:source.append((name,declaration_bytes if name=='public-release.json' else (ROOT/name).read_bytes()))
 for folder in FOLDERS:
  source.extend((n,b) for n,b in files_under(folder) if not n.startswith('encoder/sources/'))
 write_zip(out/names[1],stem+'-source',source)
 runtime=[(n,declaration_bytes if n=='public-release.json' else (ROOT/n).read_bytes()) for n in RUNTIME]
 for folder in ('extension','assets','docs','licenses'):runtime+=list(files_under(folder))
 runtime.append(('tools/corresponding-sources.zip',component_bytes));write_zip(out/names[0],stem,runtime)
 sums=''.join(digest((out/n).read_bytes())+'  '+n+'\n' for n in names);(out/'SHA256SUMS.txt').write_text(sums,encoding='ascii')
 meta['assets']=[{'name':n,'size':(out/n).stat().st_size,'sha256':digest((out/n).read_bytes()),'url':meta['source_url']+'/releases/download/'+meta['tag']+'/'+n} for n in names+['SHA256SUMS.txt']]
 (ROOT/'public-release.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');(ROOT/'SHA256SUMS.txt').write_text(sums,encoding='ascii')
 print(json.dumps([{'name':a['name'],'size':a['size'],'sha256':a['sha256']} for a in meta['assets']],indent=2))
if __name__=='__main__':main()
