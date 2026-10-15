const fs=require('fs'),vm=require('vm'),assert=require('assert'),path=require('path');
const root=process.argv[2]||path.join(__dirname,'../../extension');
const script=n=>fs.readFileSync(path.join(root,n),'utf8');
function event(){const handlers=[];return {addListener:f=>handlers.push(f),emit:m=>handlers.forEach(f=>f(m))};}
class Element {constructor(tag,ns){this.tagName=tag;this.namespaceURI=ns;this.children=[];this.attributes={};this.style={};this.listeners={};this.classList={contains:()=>false};}setAttribute(k,v){this.attributes[k]=String(v);}appendChild(n){n.parent=this;this.children.push(n);return n;}append(...n){n.forEach(x=>this.appendChild(x));}insertBefore(n,b){n.parent=this;const i=this.children.indexOf(b);this.children.splice(i<0?this.children.length:i,0,n);}remove(){if(this.parent)this.parent.children=this.parent.children.filter(n=>n!==this);}addEventListener(k,f){(this.listeners[k]||(this.listeners[k]=[])).push(f);}removeEventListener(k,f){this.listeners[k]=(this.listeners[k]||[]).filter(x=>x!==f);}emit(k){for(const f of this.listeners[k]||[])f();}set textContent(t){this.text=t;this.children=[];}get textContent(){return this.text||'';}}
const base=new Element('root'),panel=new Element('div'),bar=new Element('progress');panel.id='ytw7-panel';bar.id='ytw7-progress';base.appendChild(panel);panel.appendChild(bar);
function all(e=base){return [e,...e.children.flatMap(x=>all(x))];}
let video=new Element('video');Object.assign(video,{currentTime:0,duration:6,paused:true,ended:false,play:()=>{video.paused=false;return Promise.resolve();}});
const player=new Element('player');player.id='movie_player';base.appendChild(player);
const document={title:'Test - YouTube',getElementById:id=>all().find(n=>n.id===id),querySelector:s=>s==='video.html5-main-video'?video:null,querySelectorAll:()=>all().filter(n=>'data-ytw7-format' in n.attributes),createElement:n=>new Element(n),createElementNS:(ns,n)=>new Element(n,ns),addEventListener:()=>{}};
const handlers={},progress=[],controls=[],native=[];let port;
const window={addEventListener:(name,fn)=>(handlers[name]||(handlers[name]=[])).push(fn),dispatchEvent:e=>{if(e.type==='ytw7-progress')progress.push(e.detail);(handlers[e.type]||[]).forEach(f=>f(e));},postMessage:m=>controls.push(m)};
const intervals=[],timeouts=[];let clock=0;const preference=new Map([['ytw7_capture_rate23','4']]);class FakeDate extends Date{static now(){return clock;}}
const saved={session:'testVideo01-test-session',videoId:'testVideo01',height:720,sourceHeight:720,itag:136,codec:'h264',rate:4,duration:6000,title:'Test'};
const ctx={window,document,location:{href:'https://www.youtube.com/watch?v=testVideo01',pathname:'/watch',search:'?v=testVideo01',origin:'https://www.youtube.com',reload:()=>{}},Date:FakeDate,localStorage:{getItem:k=>preference.get(k)||null,setItem:(k,v)=>preference.set(k,v)},sessionStorage:{getItem:()=>JSON.stringify(saved),removeItem:()=>{},setItem:()=>{}},URLSearchParams,URL,Set,Number,Math,JSON,ArrayBuffer,Uint8Array,btoa:s=>Buffer.from(s,'binary').toString('base64'),CustomEvent:class {constructor(type,options){this.type=type;this.detail=options.detail;}},setInterval:f=>(intervals.push(f),intervals.length),setTimeout:(fn,delay)=>(timeouts.push({fn,delay}),timeouts.length),clearInterval:()=>{},clearTimeout:()=>{},chrome:{runtime:{connect:()=>port={onMessage:event(),onDisconnect:event(),postMessage:m=>native.push(m),disconnect:()=>{}},sendMessage:(m,cb)=>cb({ok:true})}}};
window.YTD={settings:{mode:'capture',captureRate:4,visible:{original720:true}},translate:s=>s,ready:{then:f=>f()}};
vm.createContext(ctx);vm.runInContext(script('shared.js'),ctx);vm.runInContext(script('quality_content.js'),ctx);
const send=data=>(handlers.message||[]).forEach(f=>f({source:window,origin:ctx.location.origin,data}));

video=null;port.onMessage.emit({action:'ready',audioCached:false});
send({tag:'YTWIN7_SABR_CAPTURE_11',session:saved.session,type:'state'});
for(let i=0;i<150;i++){clock+=1000;timeouts.pop().fn();}
assert(!native.some(m=>m.action==='cancel'),'slow loading was cancelled');
assert(!controls.some(m=>m.type==='quality'),'capture started before a usable player');
assert(progress.at(-1).text.includes('150 с'));
video=new Element('video');Object.assign(video,{duration:6,currentTime:6,paused:true,ended:true,play:()=>{video.ended=false;video.paused=false;return Promise.resolve();}});
clock+=1000;timeouts.pop().fn();assert(controls.some(m=>m.type==='quality'));assert(video.currentTime===0);assert(!controls.some(m=>m.type==='drain'),'stale end before seek triggered premature finalization');
send({tag:'YTWIN7_SABR_CAPTURE_11',session:saved.session,type:'warning',error:'request cancelled'});
assert(!native.some(m=>m.action==='cancel'),'transient request error stopped capture');
window.YTD.settings.visible={original720:false};handlers['ytd-settings-change'].forEach(f=>f());
assert(!native.some(m=>m.action==='cancel'),'preference change cancelled the active task');assert(document.getElementById('ytw7-quality-cancel').style.display==='inline-block','hiding formats hid cancellation');
window.YTD.settings.visible.original720=true;handlers['ytd-settings-change'].forEach(f=>f());
console.log('PASS: changing visibility during a capture preserves the task and its cancel control.');
port.onMessage.emit({action:'result',ok:true,codec:'av1',message:'done'});
assert(document.getElementById('ytw7-speed').value==='4');
window.YTD.settings.captureRate=1;handlers['ytd-settings-change'].forEach(f=>f());assert(preference.get('ytw7_capture_rate23')==='1');
console.log('PASS: 150-second page load waits without reload/cancel; capture starts at zero only after MAIN acknowledgement and usable video.');
console.log('PASS: network cancellation leaves capture active; 4x survives completion; changing to 1x persists independently of capture state.');
// A lost native session recovers once with a new session and the same rate.
document.querySelectorAll()[0];
send({tag:'YTWIN7_FORMATS21',videoId:saved.videoId,formats:[{height:720,sourceHeight:720,itag:398,codec:'av1',duration:6000}]});
document.querySelectorAll()[0].onclick();const original=port;original.onMessage.emit({action:'fatal',error:'Сессия потеряна после обновления страницы.'});
assert(progress.at(-1).text.includes('2/3'));timeouts.pop().fn();
const retry=native.at(-1);assert(retry.action==='start'&&retry.rate===1&&retry.attempt===2&&retry.session!==saved.session);
console.log('PASS: lost session creates a bounded fresh attempt with a new session and preserved speed.');
port.onMessage.emit({action:'fatal',error:'Сессия потеряна'});assert(progress.at(-1).text.includes('3/3'));timeouts.pop().fn();
assert(native.at(-1).attempt===3);port.onMessage.emit({action:'fatal',error:'Сессия потеряна'});
assert(progress.at(-1).text.includes('После 3 попыток'));assert(native.at(-1).action==='cancel');
console.log('PASS: third failed attempt stops; there is no infinite page-reload loop.');
