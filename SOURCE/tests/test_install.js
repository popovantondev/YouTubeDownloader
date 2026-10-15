const fs=require('fs'),path=require('path'),vm=require('vm'),assert=require('assert');
const root=path.join(__dirname,'../..'),code=fs.readFileSync(root+'/setup.js','utf8');
function run(mode='install',options={}){
 const local='C:\\Users\\Тест\\AppData\\Local',base=local+'\\YTWin7_360_MP3',data=local+'\\YTWin7_WatchCapture',backup=data+'\\BACKUP_PRE_250',files=new Map(),folders=new Set([local]),reg={},echo=[],copies=[],deletes=[];let exit=0,failed=false;
 const apps=['Microsoft\\Edge','Google\\Chrome','Chromium','Yandex\\YandexBrowser','Yandex\\YandexBrowserStable'],key=a=>'HKCU\\Software\\'+a+'\\NativeMessagingHosts\\com.ytwin7.sabr\\';
 for(const n of ['YTWin7SabrHost.exe','YTWin7SabrHost.exe.config','ffmpeg.exe','ffprobe.exe','register_sabr_host.js','extension\\manifest.json'])files.set('C:\\Package\\'+n,'new:'+n);
 if(options.missing)files.delete('C:\\Package\\ffmpeg.exe');
 if(options.upgrade||mode==='uninstall'){folders.add(base);folders.add(base+'\\extension');for(const n of ['YTWin7SabrHost.exe','YTWin7SabrHost.exe.config','ffmpeg.exe','ffprobe.exe','extension\\manifest.json','com.ytwin7.sabr.json'])files.set(base+'\\'+n,'old:'+n);for(const a of apps)reg[key(a)]=base+'\\com.ytwin7.sabr.json';files.set(data+'\\AUDIO_CACHE\\keep.m4a','audio');}
 if(options.foreign)reg[key(apps[0])]='C:\\Other\\host.json';
 const fso={GetParentFolderName:p=>path.win32.dirname(p),BuildPath:path.win32.join,GetTempName:()=> 'unique.tmp',FolderExists:p=>folders.has(p),CreateFolder:p=>folders.add(p),FileExists:p=>files.has(p),
 CopyFile:(a,b)=>{if(!files.has(a))throw Error('missing '+a);if(options.copyFail&&!failed&&a==='C:\\Package\\ffmpeg.exe'){failed=true;throw Error('locked');}files.set(b,files.get(a));copies.push([a,b]);},
 CopyFolder:(a,b)=>{folders.add(b);for(const [name,value] of [...files])if(name.startsWith(a+'\\'))files.set(b+name.slice(a.length),value);},
 DeleteFile:p=>files.delete(p),DeleteFolder:p=>{folders.delete(p);for(const n of [...files.keys()])if(n.startsWith(p+'\\'))files.delete(n);},
 CreateTextFile:p=>{let value='';return {WriteLine:s=>value+=s+'\n',Close:()=>files.set(p,value)};},OpenTextFile:p=>{let i=0;const lines=files.get(p).split('\n');return {ReadLine:()=>lines[i++],Close:()=>{}};}};
 const shell={ExpandEnvironmentStrings:()=>local,RegRead:k=>{if(k.includes('LocaleName'))return 'de-DE';if(!(k in reg))throw Error('missing');return reg[k];},RegWrite:(k,v)=>reg[k]=v,RegDelete:k=>{deletes.push(k);delete reg[k];},Run:command=>{if(options.runtimeFail)return 1;if(command.startsWith('cscript')){files.set(base+'\\com.ytwin7.sabr.json','new manifest');for(const a of apps)reg[key(a)]=base+'\\com.ytwin7.sabr.json';}return 0;}};
 const args=i=>[mode,'de'][i];Object.defineProperty(args,'length',{value:2});const ctx={WScript:{Arguments:args,ScriptFullName:'C:\\Package\\setup.js',CreateObject:n=>n==='WScript.Shell'?shell:fso,Echo:s=>echo.push(s),Quit:n=>{exit=n;throw {quit:true};}}};try{vm.runInNewContext(code,ctx);}catch(e){if(!e.quit)throw e;}return {exit,copies,deletes,reg,echo,base,data,backup,files};
}
let r=run();assert.equal(r.exit,0);assert.equal(Object.keys(r.reg).length,5);assert(r.echo.at(-1).includes('Installation abgeschlossen'));
r=run('install',{upgrade:true});assert.equal(r.exit,0);assert.equal(r.files.get(r.backup+'\\ffmpeg.exe'),'old:ffmpeg.exe');assert.equal(r.files.get(r.backup+'\\ffprobe.exe'),'old:ffprobe.exe');assert.equal(r.files.get(r.base+'\\ffmpeg.exe'),'new:ffmpeg.exe');assert.equal(r.files.get(r.data+'\\AUDIO_CACHE\\keep.m4a'),'audio');
r=run('install',{upgrade:true,copyFail:true});assert.equal(r.exit,1);assert.equal(r.files.get(r.base+'\\YTWin7SabrHost.exe'),'old:YTWin7SabrHost.exe');assert.equal(r.files.get(r.base+'\\ffmpeg.exe'),'old:ffmpeg.exe');assert.equal(Object.keys(r.reg).length,5);
for(const failure of ['missing','runtimeFail']){r=run('install',{[failure]:true});assert.equal(r.exit,1);assert(!r.copies.length);}
r=run('uninstall',{foreign:true});assert.equal(r.exit,0);assert.equal(r.deletes.length,4);assert.equal(Object.values(r.reg)[0],'C:\\Other\\host.json');assert.equal(r.files.get(r.data+'\\AUDIO_CACHE\\keep.m4a'),'audio');
console.log('PASS: per-user install; full media-tool backup on upgrade; automatic rollback on locked file; early failures; five browser registrations; foreign registration and user cache preserved on removal.');
