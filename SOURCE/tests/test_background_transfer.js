const fs=require('fs'),path=require('path'),vm=require('vm'),assert=require('assert');
const code=fs.readFileSync(path.join(__dirname,'../../extension/background_download.js'),'utf8');
function event(){const listeners=[];return {addListener:fn=>listeners.push(fn),emit:value=>listeners.forEach(fn=>fn(value))};}
const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
async function scenario(options={}){
 const nativeMessages=[],clientMessages=[],requests=[],output={video:[],audio:[]};let native,client,connects=0,outstanding=0,peak=0;
 const connection=event(),identity={videoId:'testVideo01',title:'Fixture',duration:6000};
 const data={video:Buffer.alloc(100000,17),audio:Buffer.alloc(50000,31)};
 const ctx={chrome:{runtime:{onConnect:connection,connectNative:()=>{connects++;native={onMessage:event(),onDisconnect:event(),postMessage:m=>{nativeMessages.push(m);if(m.action==='backgroundStart')queueMicrotask(()=>native.onMessage.emit({action:'ready',audioCached:!!options.cached}));else if(m.action==='trackChunk'){outstanding++;peak=Math.max(peak,outstanding);output[m.track].push(Buffer.from(m.data,'base64'));setTimeout(()=>{outstanding--;native.onMessage.emit({action:'trackAck',part:m.part});},2);}else if(m.action==='backgroundFinish')queueMicrotask(()=>native.onMessage.emit({action:'result',ok:true,message:'complete',codec:'h264',height:720}));},disconnect:()=>{}};return native;}}},context:m=>({...identity,...m}),identity:a=>({videoId:a.videoId,title:a.title,duration:a.duration}),mediaBusy:false,qualityCaptures:new Map(),AbortController,URL,Map,Promise,Number,Date,Uint8Array,setTimeout,clearTimeout,btoa:s=>Buffer.from(s,'binary').toString('base64'),fetch:async(url,{signal})=>{
  requests.push(String(url));const kind=String(url).includes('audio')?'audio':'video';
  if(options.fetchPending){setTimeout(()=>native.onDisconnect.emit(),5);return new Promise(()=>{});}
  let offset=0;return {ok:true,status:200,body:{getReader:()=>({read:async()=>{if(options.cancel){return new Promise((_,reject)=>signal.addEventListener('abort',()=>reject(Error('aborted')),{once:true}));}if(offset===data[kind].length)return {done:true};const next=data[kind].subarray(offset,offset+70000);offset+=next.length;return {done:false,value:new Uint8Array(next)};},cancel:async()=>{}})}};
 }};
 vm.createContext(ctx);vm.runInContext(code,ctx);client={name:'ytwin7-background24',sender:{tab:{id:1,url:'https://www.youtube.com/watch?v=testVideo01'}},onMessage:event(),onDisconnect:event(),postMessage:m=>clientMessages.push(m)};connection.emit(client);
 const source={kind:'direct',codec:'h264',video:{itag:136,height:720,qualityLabel:'720p',mimeType:'video/mp4; codecs="avc1.4d401f"',contentLength:data.video.length,url:options.foreign?'https://example.com/video':'https://fixture.googlevideo.com/video'},audio:{itag:140,mimeType:'audio/mp4; codecs="mp4a.40.2"',contentLength:data.audio.length,url:'https://fixture.googlevideo.com/audio'}};
 client.onMessage.emit({action:'start',...identity,height:720,source});
 if(options.cancel){await delay(10);client.onMessage.emit({action:'cancel'});}
 for(let i=0;i<100&&ctx.__ytw7BackgroundTransfer;i++)await delay(5);
 assert(!ctx.__ytw7BackgroundTransfer,'background job remained locked');
 if(options.foreign){assert(connects===0&&requests.length===0);assert(clientMessages.at(-1).ok===false);return;}
 if(options.fetchPending){assert(clientMessages.at(-1).ok===false);assert(!nativeMessages.some(m=>m.action==='backgroundFinish'));return;}
 if(options.cancel){assert(nativeMessages.some(m=>m.action==='cancel'));assert(!nativeMessages.some(m=>m.action==='backgroundFinish'));return;}
 assert(clientMessages.at(-1).ok===true);assert(peak===1,'native writes were not serialized');assert(Buffer.concat(output.video).equals(data.video));
 assert(nativeMessages.filter(m=>m.action==='trackChunk').every(m=>Buffer.from(m.data,'base64').length<=32768));
 if(options.cached){assert(requests.length===1&&!output.audio.length);}else assert(Buffer.concat(output.audio).equals(data.audio));
 const chunks=nativeMessages.filter(m=>m.action==='trackChunk');assert(chunks.every((m,i)=>m.part===i+1));
}
(async()=>{await scenario();await scenario({cached:true});console.log('PASS: worker serializes 32KiB ACK writes, preserves both tracks and skips direct audio HTTP when cached.');await scenario({cancel:true});await scenario({fetchPending:true});console.log('PASS: cancel and native disconnect stop a pending network job promptly, without finish or a stuck lock.');await scenario({foreign:true});console.log('PASS: foreign download host is rejected before native startup or network I/O.');})().catch(e=>{console.error(e);process.exitCode=1;});
