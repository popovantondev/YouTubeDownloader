const fs=require('fs'),vm=require('vm'),assert=require('assert');
function event(){const handlers=[];return {addListener:f=>handlers.push(f),emit:(...args)=>handlers.map(f=>f(...args)),handlers};}
const nativeCalls=[],registrations=[],status=[];
const runtime={onMessage:event(),onConnect:event(),onInstalled:event(),onStartup:event(),lastError:null,connectNative:()=>{const p={onMessage:event(),onDisconnect:event(),disconnect:()=>{},postMessage:m=>{nativeCalls.push(m);if(m.action==='start')queueMicrotask(()=>p.onMessage.emit({action:'ready',audioCached:true,ok:true}));if(m.action==='lookup')queueMicrotask(()=>p.onMessage.emit({action:'result',ok:true,cached:true}));if(m.action==='mp3')queueMicrotask(()=>p.onMessage.emit({action:'result',ok:true,path:'cached.mp3'}));}};return p;}};
const chrome={runtime,tabs:{onRemoved:event(),sendMessage:(id,m,cb)=>{status.push(m);cb();}},scripting:{getRegisteredContentScripts:async()=>[],registerContentScripts:async s=>registrations.push(...s),updateContentScripts:async()=>{},executeScript:async()=>{}},downloads:{onChanged:event(),search:()=>{throw Error('Unexpected download lookup');},download:()=>{throw Error('Unexpected network download');}},storage:{local:{get:()=>{},set:()=>{},remove:()=>{}}}};
const ctx={chrome,URL,TextEncoder,Map,Promise,Number,String,Error,btoa:s=>Buffer.from(s,'binary').toString('base64')};vm.createContext(ctx);vm.runInContext(fs.readFileSync(require('path').join(__dirname,'../../extension/background.js'),'utf8'),ctx);
function port(videoId){return {name:'ytwin7-quality11',sender:{tab:{id:10,url:'https://www.youtube.com/watch?v='+videoId}},onMessage:event(),onDisconnect:event(),messages:[],postMessage(m){this.messages.push(m);},disconnect(){}};}
(async()=>{
 const first=port('testVideo01');runtime.onConnect.emit(first);
 await Promise.all(first.onMessage.emit({action:'start',session:'testVideo01-test-session',videoId:'testVideo01',height:720,duration:6000,title:'Test'}));await new Promise(r=>setImmediate(r));
 assert(registrations.some(r=>r.world==='MAIN'&&r.runAt==='document_start'));
 assert(first.messages[0].action==='ready');first.onDisconnect.emit();
 const resumed=port('testVideo01');runtime.onConnect.emit(resumed);await Promise.all(resumed.onMessage.emit({action:'resume',session:'testVideo01-test-session'}));assert(resumed.messages[0].audioCached===true);assert.strictEqual(nativeCalls.filter(m=>m.action==='start').length,1);
 console.log('PASS: Chrome 109 dynamic MAIN registration; native capture session and cache flag survive page reload.');
 await Promise.all(resumed.onMessage.emit({action:'cancel',session:'testVideo01-test-session'}));assert(nativeCalls.some(m=>m.action==='cancel'));
 const wrong=port('otherVideo1');runtime.onConnect.emit(wrong);await Promise.all(wrong.onMessage.emit({action:'start',session:'testVideo01-test-session',videoId:'testVideo01',height:720}));assert(wrong.messages.some(m=>m.action==='fatal'));
 console.log('PASS: capture rejects video ID different from the sender tab.');
 const replies=[];runtime.onMessage.emit({action:'cachedMP3',videoId:'testVideo01',title:'Test',duration:6000},{tab:{id:10,url:'https://www.youtube.com/watch?v=testVideo01'}},r=>replies.push(r));
 await new Promise(r=>setImmediate(r));await new Promise(r=>setImmediate(r));
 assert(replies.length===1&&replies[0].ok);assert(nativeCalls.some(m=>m.action==='mp3'&&!m.url));assert(status.some(m=>m.done&&m.progress===100));console.log('PASS: MP3 with cached audio starts without a URL, browser download or duplicate response.');
})().catch(e=>{console.error(e);process.exitCode=1;});
