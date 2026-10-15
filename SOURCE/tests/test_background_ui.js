const fs=require('fs'),path=require('path'),vm=require('vm'),assert=require('assert');
const events={},documentEvents={},intervals=new Map(),sent=[],progress=[];let pauses=0,port;
const event=()=>{const f=[];return {addListener:x=>f.push(x),emit:m=>f.forEach(x=>x(m))};};
const video={duration:6,paused:false,pause(){this.paused=true;pauses++;},matches:s=>s==='video.html5-main-video'};
const document={title:'Fixture - YouTube',querySelector:()=>video,getElementById:()=>({classList:{contains:()=>false}}),addEventListener:(n,f)=>documentEvents[n]=f,removeEventListener:n=>delete documentEvents[n]};
const window={YTW7:{},addEventListener:(n,f)=>events[n]=f,dispatchEvent:e=>progress.push(e),postMessage:m=>sent.push(m)};
const ctx={window,document,location:{href:'https://www.youtube.com/watch?v=testVideo01',origin:'https://www.youtube.com'},URL,Number,Date,CustomEvent:class{constructor(type,{detail}){this.type=type;this.detail=detail;}},setTimeout:()=>1,clearTimeout:()=>{},setInterval:f=>{intervals.set(1,f);return 1;},clearInterval:id=>intervals.delete(id),chrome:{runtime:{sendMessage:(m,cb)=>cb({ok:true}),connect:()=>port={onMessage:event(),onDisconnect:event(),postMessage:m=>sent.push(m),disconnect:()=>{}}}}};
vm.createContext(ctx);vm.runInContext(fs.readFileSync(path.join(__dirname,'../../extension/background_content.js'),'utf8'),ctx);
window.YTW7.backgroundDownload({height:480,duration:268000});assert(video.paused&&window.__ytw7BackgroundBusy);const request=sent.at(-1);assert(request.height===480);
video.paused=false;documentEvents.play({target:video});assert(video.paused);
events.message({source:window,origin:ctx.location.origin,data:{tag:'YTWIN7_BACKGROUND24_RESPONSE',id:request.id,videoId:'testVideo01',ok:true,source:{kind:'direct'}}});assert(sent.at(-1).duration===268000,'ad duration overrode clip metadata');
port.onMessage.emit({action:'result',ok:true,codec:'h264',message:'done'});assert(!window.__ytw7BackgroundBusy&&!intervals.size&&!documentEvents.play);assert(progress.some(e=>e.type==='ytw7-background-result'&&e.detail.codec==='h264'));
window.YTW7.backgroundDownload({height:720,duration:268000});window.YTW7.backgroundCancel();assert(!window.__ytw7BackgroundBusy&&!intervals.size&&!documentEvents.play);
console.log('PASS: background UI pauses without reload, prevents autoplay during the job, uses movie duration instead of stale ad metadata, and releases pause guards on success/cancel.');

// A server format refusal retries a different source without playback/reload.
const timeouts=new Map();let timeoutId=10;ctx.setTimeout=(f,ms)=>{const i=++timeoutId;timeouts.set(i,{f,ms});return i;};ctx.clearTimeout=i=>timeouts.delete(i);
window.YTW7.backgroundDownload({height:720,duration:268000});let req=sent.at(-1);
function source(req,itag){events.message({source:window,origin:ctx.location.origin,data:{tag:'YTWIN7_BACKGROUND24_RESPONSE',id:req.id,videoId:'testVideo01',ok:true,source:{kind:'sabr',video:{itag}}}});}
source(req,136);let oldPort=port;oldPort.onMessage.emit({action:'result',ok:false,retryableFormat:true,rejectedItag:136,error:'sabr.no_video_selected'});assert(window.__ytw7BackgroundBusy&&video.paused);
oldPort.onDisconnect.emit();assert(window.__ytw7BackgroundBusy,'old port ended retry');
let retry=[...timeouts.values()].find(x=>x.ms===750);assert(retry);retry.f();req=sent.at(-1);assert(req.height===720&&req.rejectedItags.includes(136));source(req,247);assert(sent.at(-1).source.video.itag===247);
port.onMessage.emit({action:'result',ok:false,retryableFormat:true,rejectedItag:247,error:'sabr.no_video_selected'});retry=[...timeouts.values()].filter(x=>x.ms===750).at(-1);retry.f();req=sent.at(-1);assert(req.rejectedItags.includes(136)&&req.rejectedItags.includes(247));source(req,398);
port.onMessage.emit({action:'result',ok:false,retryableFormat:true,rejectedItag:398,error:'sabr.no_video_selected'});assert(!window.__ytw7BackgroundBusy,'retry limit exceeded');
window.YTW7.backgroundDownload({height:480,duration:268000});req=sent.at(-1);source(req,135);port.onMessage.emit({action:'result',ok:false,retryableFormat:true,rejectedItag:135,error:'sabr.no_video_selected'});retry=[...timeouts.values()].filter(x=>x.ms===750).at(-1);window.YTW7.backgroundCancel();const before=sent.length;retry.f();assert(sent.length===before&&!window.__ytw7BackgroundBusy,'cancel allowed delayed restart');
console.log('PASS: same-height codec retry, old-port disconnect ignored, three-attempt bound, pause preserved, and cancel invalidates pending retry.');
