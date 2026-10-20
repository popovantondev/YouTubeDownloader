// Routing tests use simulated browser APIs; they do not establish live YouTube access.
const fs=require('fs'),vm=require('vm'),assert=require('assert');
const root=require('path').resolve(__dirname,'..');
function event(){const callbacks=[];return {addListener:f=>callbacks.push(f),emit:v=>callbacks.forEach(f=>f(v))};}
const listeners=[],sent=[],statuses=[];let codec='aac';
const chrome={runtime:{id:'extension-test',getURL:p=>'chrome-extension://extension-test/'+p,
 onMessage:{addListener:f=>listeners.push(f)},onInstalled:event(),onStartup:event(),onConnect:event(),
 connectNative(){const message=event(),disconnect=event();let closed=false;return {onMessage:message,onDisconnect:disconnect,
  disconnect(){if(!closed){closed=true;disconnect.emit();}},postMessage(m){sent.push(m);queueMicrotask(()=>message.emit(m.action==='lookup'?{action:'result',ok:true,cached:true,audioCodec:codec}:{action:'result',ok:true,handedOff:m.action==='peak',installed:true}));}};}},
 downloads:{onChanged:event(),search:(q,cb)=>cb([])},storage:{local:{get:(k,cb)=>cb({}),set:(s,cb)=>cb?.(),remove:()=>{}}},
 tabs:{onRemoved:event(),sendMessage:(id,m,cb)=>{statuses.push(m);cb?.();}},scripting:{}};
const context=vm.createContext({chrome,console,URL,TextEncoder,btoa:s=>Buffer.from(s,'binary').toString('base64'),queueMicrotask,setTimeout,clearTimeout,AbortController});
vm.runInContext(fs.readFileSync(root+'/extension/background.js','utf8'),context);
const sender={id:'extension-test',tab:{id:5,url:'https://www.youtube.com/watch?v=YtdTest0001'}};
function request(action,extra={},from=sender){return new Promise((resolve,reject)=>{let accepted=false;const message={action,videoId:'YtdTest0001',duration:5000,title:'ABBA — A/B? Test',language:'de',operationId:'test-op',...extra};for(const f of listeners){if(f(message,from,resolve)===true)accepted=true;}if(!accepted&&action!=='openDesktop')reject(Error('No async listener accepted '+action));});}
const settle=()=>new Promise(r=>setTimeout(r,15));
(async()=>{
 assert((await request('cachedProcessedMP3')).ok);await settle();
 const peak=sent.find(m=>m.action==='peak');assert(peak);assert.equal(peak.language,'de');assert.equal(peak.displayTitle,'ABBA — A/B? Test');assert(!peak.title.includes('/'));
 assert(statuses.some(m=>m.done&&m.message.includes('AAC передан')&&m.operationId==='test-op'));
 sent.length=0;codec='opus';assert((await request('cachedProcessedMP3')).needsUrl);await settle();assert(!sent.some(m=>m.action==='peak'));
 sent.length=0;codec='aac';assert((await request('cachedMP3')).ok);await settle();assert(sent.some(m=>m.action==='mp3'));assert(!sent.some(m=>m.action==='peak'));
 assert.equal((await request('openDesktop')).ok,false);
 const popup={id:'extension-test',url:'chrome-extension://extension-test/popup.html'};
 assert((await request('openDesktop',{},popup)).ok);
 const c=vm.createContext({chrome:{i18n:{getUILanguage:()=> 'fr'},storage:{local:{get:(k,cb)=>cb({}),set:(v,cb)=>cb()},onChanged:event()}},navigator:{language:'fr'},dispatchEvent:()=>{},CustomEvent:function(){},console});
 for(const file of ['settings.js','i18n.js'])vm.runInContext(fs.readFileSync(root+'/extension/'+file,'utf8'),c);
 await c.YTD.ready;assert.equal(c.YTD.settings.language,'en');assert.equal(c.YTD.settings.mode,'background');
 assert.equal(Object.keys(c.YTD.settings.visible).filter(k=>c.YTD.settings.visible[k]).join(','),'quick360,mp3,original480,original720');
 for(const lang of ['ru','de','en']){c.YTD.settings.language=lang;for(const key of ['mp3Plain','mp3Peak','waitingAudio','waitExpired','desktopMissing','audioMenu'])assert.notEqual(c.YTD.text(key),key);}
 console.log('PASS: AAC/Opus route, plain MP3 separation, title/language handoff, popup access, defaults and three-language new UI keys');
})().catch(e=>{console.error(e);process.exitCode=1;});
