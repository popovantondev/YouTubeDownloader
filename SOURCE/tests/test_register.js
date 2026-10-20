const fs=require('fs'),vm=require('vm'),assert=require('assert'),path=require('path');
let output='',writes=[];const root='C:\\Users\\Пользователь 😀\\AppData\\Local';
const sh={Environment:()=>()=>'',ExpandEnvironmentStrings:()=>root,RegWrite:(...x)=>writes.push(x)};
const fso={CreateTextFile:()=>({Write:s=>output+=s,Close:()=>{}})};
const ctx={WScript:{CreateObject:name=>name==='WScript.Shell'?sh:fso,Echo:()=>{}}};
vm.createContext(ctx);vm.runInContext(fs.readFileSync(path.join(__dirname,'../../register_sabr_host.js'),'utf8'),ctx);
assert([...output].every(c=>c.charCodeAt(0)<128));const parsed=JSON.parse(output);assert(parsed.path===root+'\\YTWin7_360_MP3\\YTWin7SabrHost.exe');assert(writes.length===5);assert(writes.some(x=>x[0].includes('Microsoft\\Edge')));console.log('PASS: native manifest stays ASCII-valid JSON and preserves Cyrillic/emoji profile paths.');
