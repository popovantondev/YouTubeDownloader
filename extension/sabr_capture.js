// Experimental SABR/UMP capture. Runs in the MAIN world before the YouTube player.
// Does not request external scripts or transmit tokens, cookies, or media URLs.
(() => {
 'use strict';
 if (window.__YTWIN7_SABR_CAPTURE_11__) return;
 window.__YTWIN7_SABR_CAPTURE_11__ = true;
 const TAG='YTWIN7_SABR_CAPTURE_11', MAX_PART=16*1024*1024, CHUNK=12288;
 let active=false,draining=false,audioCached=false, session='', requestNo=0, sentBytes=0,inflight=0,targetTag=0,targetTags=new Set();
 const wantedVideo=new URL(location.href).searchParams.get('v');
 function drained(){if(draining&&inflight===0){draining=false;window.postMessage({tag:TAG,type:'drained',session},location.origin);active=false;}}
 const limits = {requests:100000, bytes:16*1024*1024*1024};
 let bridgeReady=false,pending=[],pendingBytes=0;const STARTUP_LIMIT=8*1024*1024;
 function flush(){const queue=pending;pending=[];pendingBytes=0;for(const item of queue)emit(item.data,item.buf);}
 function emit(data,buf){
   if(!active)return;
   if(!bridgeReady){if(pendingBytes+(buf?buf.byteLength:0)>STARTUP_LIMIT){pending=[];pendingBytes=0;active=false;window.postMessage({tag:TAG,type:'error',session,error:'Приёмник долго не готов: превышен буфер запуска. Повтори захват.'},location.origin);return;}pending.push({data,buf});pendingBytes+=buf?buf.byteLength:0;return;}
   if(buf){
     if(sentBytes+buf.byteLength>limits.bytes){window.postMessage({tag:TAG,type:'error',session,error:'Достигнут лимит памяти/данных 16 ГБ'},location.origin);return;}
     sentBytes+=buf.byteLength;
     window.postMessage({tag:TAG,session,...data,bytes:buf.buffer},location.origin,[buf.buffer]);
   }else window.postMessage({tag:TAG,session,...data},location.origin);
 }
 function uvar(b,p){
   if(p>=b.length)return null;
   const c=b[p],n=c<128?1:c<192?2:c<224?3:c<240?4:5;
   if(p+n>b.length)return null;
   let v=0;
   if(n===1)v=c;
   if(n===2)v=(c&63)|(b[p+1]<<6);
   if(n===3)v=(c&31)|(b[p+1]<<5)|(b[p+2]<<13);
   if(n===4)v=(c&15)|(b[p+1]<<4)|(b[p+2]<<12)|(b[p+3]<<20);
   if(n===5)v=((b[p+1])|(b[p+2]<<8)|(b[p+3]<<16)|(b[p+4]<<24))>>>0;
   return {v,n};
 }
 function protoVar(b,p){
   let v=0,s=0;
   for(let i=0;i<9&&p+i<b.length;i++){
     const c=b[p+i];v+=(c&127)*Math.pow(2,s);s+=7;
     if(!(c&128))return {v,n:i+1};
   }return null;
 }
 function scalarFields(b){const fields={};let p=0;while(p<b.length){const k=protoVar(b,p);if(!k)return fields;p+=k.n;const wire=k.v%8,no=Math.floor(k.v/8);if(wire===0){const v=protoVar(b,p);if(!v)return fields;fields[no]=v.v;p+=v.n;}else if(wire===2){const n=protoVar(b,p);if(!n)return fields;p+=n.n+n.v;}else if(wire===1)p+=8;else if(wire===5)p+=4;else return fields;}return fields;}
 function mediaHeader(b){
   const r={};let p=0;
   for(let i=0;i<70&&p<b.length;i++){
     const k=protoVar(b,p);if(!k||!k.v)return null;p+=k.n;
     const no=Math.floor(k.v/8),wire=k.v%8;
     if(wire===0){const v=protoVar(b,p);if(!v)return null;p+=v.n;
       if(no===1)r.hid=v.v;if(no===3)r.itag=v.v;if(no===8)r.init=!!v.v;if(no===9)r.seg=v.v;if(no===7)r.compression=v.v;if(no===11)r.start=v.v;if(no===12)r.duration=v.v;if(no===14)r.length=v.v;if(no===17)r.offset=v.v;
     }else if(wire===2){const len=protoVar(b,p);if(!len)return null;p+=len.n;if(p+len.v>b.length)return null;if(no===2)r.videoId=new TextDecoder().decode(b.subarray(p,p+len.v));if(no===13){const fields=scalarFields(b.subarray(p,p+len.v));if(!r.itag)r.itag=fields[1];}if(no===15){const fields=scalarFields(b.subarray(p,p+len.v));if(fields[3]>0){r.start=Math.round((fields[1]||0)*1000/fields[3]);r.duration=Math.round((fields[2]||0)*1000/fields[3]);}}p+=len.v;}
     else if(wire===1)p+=8;else if(wire===5)p+=4;else return null;
   }
   if(!Number.isInteger(r.hid)||!Number.isInteger(r.itag)||r.itag<1||r.itag>10000)return null;
   return r;
 }
 // Re-arm before YouTube initializes its player when the experiment reloads the tab.
 try{
   const saved=JSON.parse(sessionStorage.getItem('ytw7_quality_capture11')||'null');
   const videoId=new URL(location.href).searchParams.get('v');
   if(saved&&saved.videoId===videoId&&/^[A-Za-z0-9_-]{15,90}$/.test(saved.session||'')){
     session=saved.session;active=true;audioCached=!!saved.audioCached;
   }
 }catch(_){}
 const videoItags = new Set([133,134,135,136,137,160,242,243,244,247,248,278,298,299,302,303,330,331,332,333,334,335,394,395,396,397,398,399]);
 const audioItags = new Set([139,140,249,250,251]);
 const keepItag = i=>i===targetTag||targetTags.has(i)||(!targetTag&&!targetTags.size&&videoItags.has(i))||(!audioCached&&audioItags.has(i));
 class Parser {
   constructor(req){this.req=req;this.buf=new Uint8Array(0);this.kind=-1;this.remain=0;this.headerMap={};this.media=null;this.skipPrefix=true;this.ok=true;}
   feed(bytes){
     const b=new Uint8Array(this.buf.length+bytes.length);b.set(this.buf);b.set(bytes,this.buf.length);let p=0;
     while(p<b.length){
       if(this.kind<0){
         const t=uvar(b,p);if(!t)break;const s=uvar(b,p+t.n);if(!s)break;
         if(s.v>MAX_PART){this.ok=false;emit({type:'error',error:'Слишком большой UMP-фрагмент'});break;}
         this.kind=t.v;this.remain=s.v;this.payload=[];this.media=null;this.skipPrefix=true;
         p+=t.n+s.n;
       }
       if(this.remain===0){this.finish();continue;}
       const take=Math.min(this.remain,b.length-p);if(!take)break;
       if(this.kind===20||this.kind===22){this.payload.push(b.subarray(p,p+take).slice());}
       else if(this.kind===21){
         let start=p;
         if(this.skipPrefix){
           const id=uvar(b,p);
           if(!id || id.n>take){break;}
           this.media=this.headerMap[String(id.v)]||null;
           start+=id.n;
           this.skipPrefix=false;
         }
         if(this.media&&keepItag(this.media.itag)&&(!this.media.videoId||this.media.videoId===wantedVideo)&&!this.media.compression){
           for(let a=start;a<p+take;a+=CHUNK){
             const z=b.subarray(a,Math.min(p+take,a+CHUNK)).slice();
             emit({type:'chunk',request:this.req,itag:this.media.itag,
               hid:this.media.hid,segment:Number.isInteger(this.media.seg)?this.media.seg:-1,init:!!this.media.init},z);
           }
         }
       }
       p+=take;this.remain-=take;
       if(this.remain===0)this.finish();
     }
     this.buf=b.subarray(p).slice();
     if(this.buf.length>65536||!this.ok)this.buf=new Uint8Array(0);
   }
   finish(){
     if(this.kind===20){
       let size=0;for(const v of this.payload)size+=v.length;
       if(size<8192){const b=new Uint8Array(size);let p=0;for(const v of this.payload){b.set(v,p);p+=v.length;}
         const h=mediaHeader(b);if(h){if(!Number.isInteger(h.seg))h.seg=-1;if(h.videoId&&h.videoId!==wantedVideo){this.kind=-1;this.remain=0;return;}if(h.compression){emit({type:'error',error:'Сжатый сегмент пока не поддерживается'});this.kind=-1;this.remain=0;return;}this.headerMap[String(h.hid)]=h;if(keepItag(h.itag))emit({type:'header',request:this.req,...h});}
       }
     }
     if(this.kind===22){const bytes=this.payload.flatMap(v=>Array.from(v));const id=uvar(new Uint8Array(bytes),0);const h=id&&this.headerMap[String(id.v)];if(h&&keepItag(h.itag)&&(!h.videoId||h.videoId===wantedVideo))emit({type:'end',request:this.req,...h});}
     this.kind=-1;this.remain=0;this.media=null;this.payload=[];
   }
 }
 function wanted(url){return typeof url==='string'&&/^https:\/\/[^/]+\.googlevideo\.com\/videoplayback\?/i.test(url);}
 window.addEventListener('message',e=>{
   if(e.source!==window||e.origin!==location.origin||!e.data||e.data.tag!==TAG+'_CONTROL')return;
   const d=e.data;if(d.type==='idleSnapshot'){playerSnapshot(true);return;}if(!/^[a-zA-Z0-9_-]{15,90}$/.test(String(d.session||'')))return;
   if(d.type==='quality'){targetHeight=Number(d.height)||targetHeight;applyQuality(targetHeight);return;}
   if(d.type==='snapshot'){bindPlayback();playerSnapshot();return;}
   if(d.type==='drain'&&d.session===session){draining=true;setTimeout(drained,300);}
   if(d.type==='start'){active=true;bridgeReady=true;flush();draining=false;audioCached=!!d.audioCached;session=d.session;targetTag=Number(d.itag)||targetTag;targetHeight=Number(d.height)||targetHeight;targetRate=[1,2,4].includes(Number(d.rate))?Number(d.rate):2;targetTags=new Set(String(d.tagList||'').split(',').map(Number).filter(n=>Number.isInteger(n)&&n>0));bindPlayback();applySpeed();emit({type:'state',message:'SABR-перехват включён'});}
   if(d.type==='stop'&&d.session===session){restoreSpeed();active=false;pending=[];pendingBytes=0;bridgeReady=false;}
 });
 if(typeof window.fetch==='function'){
   const old=window.fetch;
   window.fetch=function(resource,options){
     let url='';try{url=typeof resource==='string'?resource:(resource&&resource.url)||'';}catch(_){ }
     const capture=active&&!draining&&wanted(url)&&requestNo<limits.requests;
     if(capture)inflight++;
     return old.apply(this,arguments).then(response=>{
       if(capture&&active&&response&&response.ok&&(/yt-ump/i.test(response.headers.get('Content-Type')||'')||/[?&](sabr|ump)=1(?:&|$)/.test(url))){
         const req=++requestNo;
         try{const clone=response.clone();
           (async()=>{const parser=new Parser(req);let reader;
             try{reader=clone.body.getReader();while(active){const r=await reader.read();if(r.done){if(parser.remain||parser.buf.length)emit({type:'warning',error:'Ответ SABR оборвался внутри сегмента'});else emit({type:'requestEnd',request:req});break;}if(r.value)parser.feed(r.value);}}
             catch(e){emit({type:'warning',error:'Ошибка чтения SABR: '+String(e.message).slice(0,80)});}
             finally{try{if(reader)await reader.cancel();}catch(_){}inflight--;drained();}
           })();
         }catch(e){inflight--;drained();emit({type:'warning',error:'Не удалось открыть SABR-копию'});}
       }else if(capture){inflight--;drained();}
       return response;
     },error=>{if(capture){inflight--;drained();emit({type:'warning',error:'Ошибка сети при захвате'});}throw error;});
   };
 }
 // Snapshot the MAIN-world YouTube player. The HTML video element can be replaced.
 const qualityNames={144:'tiny',240:'small',360:'medium',480:'large',720:'hd720',1080:'hd1080'};
 let targetHeight=0,boundVideo=null,targetRate=2,originalRate=null;
 function applySpeed(){const v=document.querySelector?document.querySelector('video.html5-main-video'):null;if(!v)return;if(originalRate===null)originalRate=Number.isFinite(v.playbackRate)?v.playbackRate:1;try{v.playbackRate=targetRate;}catch(_){} }
 function restoreSpeed(){const v=document.querySelector?document.querySelector('video.html5-main-video'):null;if(v&&originalRate!==null)try{v.playbackRate=originalRate;}catch(_){}originalRate=null;}
 function applyQuality(height){const p=document.getElementById('movie_player'),q=qualityNames[height];if(!p||!q)return;try{if(p.getPlaybackQuality&&p.getPlaybackQuality()===q)return;if(p.setPlaybackQualityRange)p.setPlaybackQualityRange(q,q);if(p.setPlaybackQuality)p.setPlaybackQuality(q);}catch(_){} }
 function playerSnapshot(idle=false){const p=document.getElementById('movie_player');const v=document.querySelector?document.querySelector('video.html5-main-video'):null;const finite=(x,fallback)=>x!==null&&x!==undefined&&x!==''&&Number.isFinite(Number(x))?Number(x):fallback;let time=finite(v&&v.currentTime,-1),duration=finite(v&&v.duration,0),state=v?(v.ended?0:v.paused?2:1):-1,videoId=new URL(location.href).searchParams.get('v'),ad=!!(p&&p.classList&&p.classList.contains('ad-showing'));
   try{const data=p&&p.getVideoData&&p.getVideoData();if(data&&data.video_id)videoId=data.video_id;time=finite(p&&p.getCurrentTime&&p.getCurrentTime(),time);duration=finite(p&&p.getDuration&&p.getDuration(),duration);state=finite(p&&p.getPlayerState&&p.getPlayerState(),state);}catch(_){}
   const payload={type:idle?'idlePlayback':'playback',videoId,time,duration,state,ad,ended:!ad&&!!(v&&v.ended),hidden:!!document.hidden,requests:requestNo,bytes:sentBytes};
   if(idle)window.postMessage({tag:TAG,...payload},location.origin);else emit(payload);
   return payload;
 }
 function bindPlayback(){const v=document.querySelector?document.querySelector('video.html5-main-video'):null;if(v===boundVideo)return;if(boundVideo)for(const name of ['ended','timeupdate','loadedmetadata','durationchange','play','pause','volumechange'])boundVideo.removeEventListener(name,onPlayback);boundVideo=v;if(v)for(const name of ['ended','timeupdate','loadedmetadata','durationchange','play','pause','volumechange'])v.addEventListener(name,onPlayback);}
 function onPlayback(){if(active){bindPlayback();playerSnapshot();}}
 try{const s=JSON.parse(sessionStorage.getItem('ytw7_quality_capture11')||'null');if(s){targetHeight=Number(s.height)||0;targetTag=Number(s.itag)||0;targetTags=new Set(String(s.tagList||'').split(',').map(Number).filter(n=>Number.isInteger(n)&&n>0));targetRate=[1,2,4].includes(Number(s.rate))?Number(s.rate):2;}}catch(_){}
 if(typeof setInterval==='function')setInterval(()=>{if(active){bindPlayback();if(!draining){applyQuality(targetHeight);applySpeed();}playerSnapshot();}},1000);
 if(document.addEventListener){document.addEventListener('visibilitychange',onPlayback);document.addEventListener('yt-navigate-finish',onPlayback);}
 if(typeof XMLHttpRequest==='function'){
   const open=XMLHttpRequest.prototype.open,send=XMLHttpRequest.prototype.send;
   XMLHttpRequest.prototype.open=function(method,url){this.__ytw7Url=String(url||'');return open.apply(this,arguments);};
   XMLHttpRequest.prototype.send=function(){const xhr=this,url=xhr.__ytw7Url,capture=active&&!draining&&wanted(url);if(capture){inflight++;const req=++requestNo;let settled=false;const done=()=>{if(!settled){settled=true;inflight--;drained();}};xhr.addEventListener('load',async()=>{try{if(!active||xhr.status<200||xhr.status>=300)return;const type=xhr.getResponseHeader('Content-Type')||'';if(!/yt-ump/i.test(type)&&!/[?&](sabr|ump)=1(?:&|$)/.test(url))return;let bytes;if(xhr.response instanceof ArrayBuffer)bytes=new Uint8Array(xhr.response);else if(typeof Blob==='function'&&xhr.response instanceof Blob)bytes=new Uint8Array(await xhr.response.arrayBuffer());else {emit({type:'error',error:'Плеер получил UMP через XHR в неподдерживаемом текстовом виде'});return;}const parser=new Parser(req);parser.feed(bytes);if(parser.remain||parser.buf.length)emit({type:'warning',error:'XHR-ответ оборвался внутри сегмента'});else emit({type:'requestEnd',request:req});}catch(e){emit({type:'error',error:'Ошибка разбора XHR: '+e.message});}finally{done();}});xhr.addEventListener('error',()=>{emit({type:'warning',error:'Сетевой запрос XHR завершился ошибкой'});done();});xhr.addEventListener('abort',done);xhr.addEventListener('timeout',done);}try{return send.apply(this,arguments);}catch(e){if(capture){inflight--;drained();}throw e;}};
 }
})();
