'use strict';
if(typeof importScripts==='function')importScripts('vendor/googlevideo.js');
const b64=s=>{let raw='';for(const c of new TextEncoder().encode(s))raw+=String.fromCharCode(c);return btoa(raw);};
function tell(tabId,videoId,message,progress,done){chrome.tabs.sendMessage(tabId,{type:'yt-win7-status',videoId,message,progress,done},()=>void chrome.runtime.lastError);}
function context(m,sender){const page=new URL(sender.tab&&sender.tab.url||'');if(page.origin!=='https://www.youtube.com'||page.pathname!=='/watch'||page.searchParams.get('v')!==m.videoId||!/^[\w-]{11}$/.test(m.videoId))throw Error('Ролик не совпадает.');const duration=Number(m.duration);if(!Number.isSafeInteger(duration)||duration<1000||duration>21600000)throw Error('Неизвестна длительность ролика.');return {...m,title:String(m.title||m.videoId).replace(/[\\/:*?"<>|\u0000-\u001f]/g,'_').replace(/[. ]+$/g,'').slice(0,75),duration,tabId:sender.tab.id};}
function nativeCall(m,onStatus=()=>{}){return new Promise((resolve,reject)=>{let finished=false;const port=chrome.runtime.connectNative('com.ytwin7.sabr');port.onMessage.addListener(r=>{if(r.action==='status')onStatus(r);if(r.action==='result'){finished=true;port.disconnect();r.ok?resolve(r):reject(Error(r.error||'Ошибка локального модуля'));}});port.onDisconnect.addListener(()=>{const e=chrome.runtime.lastError;if(!finished)reject(Error(e?e.message:'Локальный модуль отключён'));});port.postMessage(m);});}
const identity=a=>({videoId:a.videoId,title:a.title,duration:a.duration});
let mediaBusy=false;
async function reuse(a){const lookup=await nativeCall({action:'lookup',...identity(a)});if(lookup.cached&&(!a.aac||lookup.audioCodec==='aac'))return true;const candidates=await new Promise(resolve=>chrome.downloads.search({state:'complete',exists:true},items=>resolve(items||[])));const matching=candidates.filter(item=>{const name=item.filename.split(/[\\/]/).pop();return name===a.title+' [360p].mp4'||name===a.title+' ['+a.videoId+'] [360p].mp4';});for(const item of matching){try{await nativeCall({action:'import',...identity(a),input:b64(item.filename)});const imported=await nativeCall({action:'lookup',...identity(a)});if(!a.aac||imported.audioCodec==='aac')return true;}catch(_){}}return false;}
chrome.runtime.onMessage.addListener((m,sender,reply)=>{
 if(!m||!['download360','downloadMP3','cachedMP3','downloadAAC','cachedAAC','downloadProgress'].includes(m.action))return;
 let a;try{a=context(m,sender);}catch(e){reply({ok:false,error:e.message});return;}
 if(m.action==='downloadProgress'){chrome.downloads.search({id:m.downloadId},items=>{const item=items&&items[0];reply(item?{ok:true,state:item.state,progress:item.totalBytes>0?Math.floor(item.bytesReceived/item.totalBytes*95):0}:{ok:false});});return true;}
 if(m.action==='download360'){
  let url;try{url=new URL(m.url);if(url.protocol!=='https:'||!/(^|\.)googlevideo\.com$/i.test(url.hostname))throw Error();}catch(_){reply({ok:false,error:'Неверная ссылка Googlevideo.'});return;}
  chrome.downloads.download({url:url.href,filename:a.title+' ['+a.videoId+'] [360p].mp4',conflictAction:'uniquify',saveAs:false},downloadId=>{const error=chrome.runtime.lastError;if(error||typeof downloadId!=='number'){reply({ok:false,error:error?error.message:'Загрузка не началась.'});return;}chrome.storage.local.set({['ytw7_360_'+downloadId]:identity(a)},()=>{reply({ok:true,downloadId});});});return true;
 }
 if(mediaBusy||globalThis.__ytw7BackgroundTransfer){reply({ok:false,error:'Дождись завершения текущей загрузки звука.'});return;}
 a.aac=['downloadAAC','cachedAAC'].includes(m.action);mediaBusy=true;let replied=false;
 (async()=>{
  try{if(['cachedMP3','cachedAAC'].includes(m.action)&&!await reuse(a)){mediaBusy=false;reply({ok:true,needsUrl:true});return;}
   reply({ok:true});replied=true;
   if(m.url)await browserMP3(a);else await nativeCall({action:a.aac?'aac':'mp3',...identity(a)},r=>tell(a.tabId,a.videoId,r.message,r.progress,false));
   tell(a.tabId,a.videoId,a.aac?'Готово! Исходный AAC сохранён на рабочий стол в M4A.':'Готово! MP3 320 кбит/с сохранён на рабочий стол.',100,true);
  }catch(e){tell(a.tabId,a.videoId,'Ошибка: '+e.message,0,true);if(!replied)reply({ok:false,error:e.message});}finally{mediaBusy=false;}
 })();return true;
});
chrome.downloads.onChanged.addListener(change=>{if(!change.state||change.state.current!=='complete')return;const key='ytw7_360_'+change.id;chrome.storage.local.get(key,store=>{const a=store[key];if(!a)return;chrome.downloads.search({id:change.id},items=>{if(!items||!items[0])return;nativeCall({action:'import',...a,input:b64(items[0].filename)}).then(()=>chrome.storage.local.remove(key)).catch(()=>{});});});});
// Chrome 102/109: register MAIN-world scripts through scripting, before reload.
const pageScripts={id:'ytw7-main-20',matches:['https://www.youtube.com/*'],js:['background_probe.js','sabr_capture.js','page_probe.js','formats_probe.js'],runAt:'document_start',world:'MAIN',persistAcrossSessions:true};
let registration;
function register(){if(!registration)registration=chrome.scripting.getRegisteredContentScripts({ids:[pageScripts.id]}).then(scripts=>scripts.length?chrome.scripting.updateContentScripts([pageScripts]):chrome.scripting.registerContentScripts([pageScripts])).catch(e=>{registration=null;throw e;});return registration;}
chrome.runtime.onInstalled.addListener(()=>register().catch(()=>{}));
chrome.runtime.onStartup.addListener(()=>register().catch(()=>{}));
chrome.runtime.onMessage.addListener((m,sender,reply)=>{if(m&&m.action==='preparePage'){register().then(()=>chrome.scripting.executeScript({target:{tabId:sender.tab.id},files:['background_probe.js','sabr_capture.js','page_probe.js','formats_probe.js'],world:'MAIN'})).then(()=>reply({ok:true}),e=>reply({ok:false,error:e.message}));return true;}});

// Experimental SABR capture: persistent native host across one tab reload.
const qualityCaptures=new Map();
chrome.runtime.onConnect.addListener(port=>{
  if(port.name!=='ytwin7-quality11')return;
  const tab=port.sender&&port.sender.tab;
  if(!tab||!/^https:\/\/www\.youtube\.com\/watch\?/.test(tab.url||'')){
    try{port.disconnect();}catch(_){}return;
  }
  const tabId=tab.id;
  function reply(data){try{port.postMessage(data);}catch(_){}}
  port.onMessage.addListener(async m=>{
    if(!m||typeof m.action!=='string')return;
    if(m.action==='start'){
      try{await register();}catch(e){reply({action:'fatal',error:e.message});return;}
      const session=String(m.session||'');
      if(qualityCaptures.has(tabId)||globalThis.__ytw7BackgroundTransfer){reply({action:'fatal',error:'В этой вкладке уже идёт захват.'});return;}
      if(!/^[\w-]{15,90}$/.test(session)||!['144','240','360','480','720','1080'].includes(String(m.height))||
         !/^[\w-]{11}$/.test(String(m.videoId)) || new URL(tab.url).searchParams.get('v')!==m.videoId){reply({action:'fatal',error:'Некорректные параметры.'});return;}
      let nativePort;
      try{nativePort=chrome.runtime.connectNative('com.ytwin7.sabr');}catch(e){reply({action:'fatal',error:e.message});return;}
      const entry={session,tabId,nativePort,clientPort:port,finished:false};qualityCaptures.set(tabId,entry);
      nativePort.onMessage.addListener(v=>{
        if(v.action==='ready')entry.audioCached=!!v.audioCached;
        if(v.action==='result')entry.finished=true;
        if(entry.clientPort){try{entry.clientPort.postMessage(v);}catch(_){}}
        if(v.action==='result')qualityCaptures.delete(tabId);
      });
      nativePort.onDisconnect.addListener(()=>{
        if(qualityCaptures.get(tabId)===entry)qualityCaptures.delete(tabId);
        if(!entry.finished&&entry.clientPort){
          const error=chrome.runtime.lastError;
          try{entry.clientPort.postMessage({action:'fatal',error:'Конвертер отключён: '+(error?error.message:'проверь установку 2.0')});}catch(_){}
        }
      });
      try{nativePort.postMessage(m);}catch(e){reply({action:'fatal',error:e.message});}
    }else if(m.action==='resume'){
      const entry=qualityCaptures.get(tabId);
      if(!entry||entry.session!==m.session){reply({action:'fatal',error:'Сессия потеряна после обновления страницы. Выбери качество ещё раз.'});return;}
      entry.clientPort=port;
      reply({action:'ready',ok:true,resumed:true,audioCached:entry.audioCached});
    }else{
      const entry=qualityCaptures.get(tabId);
      if(!entry||entry.session!==m.session)return;
      try{entry.nativePort.postMessage(m);}catch(e){reply({action:'fatal',error:e.message});}
      if(m.action==='cancel'){entry.finished=true;qualityCaptures.delete(tabId);try{entry.nativePort.disconnect();}catch(_){}}
    }
  });
  port.onDisconnect.addListener(()=>{
    const entry=qualityCaptures.get(tabId);
    if(entry&&entry.clientPort===port)entry.clientPort=null;
    // Deliberately retain nativePort through the automated video-page reload.
  });
});

chrome.tabs.onRemoved.addListener(tabId=>{const e=qualityCaptures.get(tabId);if(e){try{e.nativePort.postMessage({action:'cancel',session:e.session});e.nativePort.disconnect();}catch(_){}qualityCaptures.delete(tabId);}});

// Use the browser's HTTPS stack; the Win7 .NET host only writes local media.
async function browserMP3(a){
 const url=new URL(a.url);if(url.protocol!=='https:'||!/(^|\.)googlevideo\.com$/i.test(url.hostname))throw Error('Неверная ссылка Googlevideo.');
 return new Promise((resolve,reject)=>{
  const port=chrome.runtime.connectNative('com.ytwin7.sabr'),abort=new AbortController();let finished=false,waiter=null,started=false;
  const fail=e=>{if(finished)return;finished=true;abort.abort();if(waiter){clearTimeout(waiter.timer);waiter.reject(e);waiter=null;}try{port.disconnect();}catch(_){}reject(e);};
  function ack(){return new Promise((resolveAck,rejectAck)=>{waiter={resolve:resolveAck,reject:rejectAck,timer:setTimeout(()=>fail(Error('Локальный приёмник не подтвердил запись фрагмента.')),30000)};});}
  async function transfer(){let reader;try{const response=await fetch(url.href,{signal:abort.signal});if(!response.ok)throw Error('YouTube отклонил загрузку: HTTP '+response.status);if(!response.body)throw Error('Браузер не предоставил поток загрузки.');reader=response.body.getReader();const total=Number(response.headers.get('Content-Length'))||0;let count=0;for(;;){const chunk=await reader.read();if(chunk.done)break;for(let offset=0;offset<chunk.value.length;offset+=12288){const part=chunk.value.subarray(offset,offset+12288);let raw='';for(const b of part)raw+=String.fromCharCode(b);const confirmed=ack();port.postMessage({action:'mediaChunk',data:btoa(raw)});await confirmed;count+=part.length;}tell(a.tabId,a.videoId,'Получаю исходник '+(a.aac?'AAC':'MP3')+': '+Math.round(count/1024)+' КБ',total>0?Math.min(80,Math.floor(count/total*80)):0,false);}port.postMessage({action:'mediaFinish'});}catch(e){fail(e);}finally{if(reader)try{await reader.cancel();}catch(_){}}}
  port.onMessage.addListener(m=>{if(m.action==='ready'&&!started){started=true;transfer();}else if(m.action==='mediaAck'&&waiter){const w=waiter;waiter=null;clearTimeout(w.timer);w.resolve();}else if(m.action==='status')tell(a.tabId,a.videoId,m.message,m.progress,false);else if(m.action==='result'){if(!m.ok){fail(Error(m.error||'Ошибка обработки звука'));return;}finished=true;port.disconnect();resolve(m);}});
  port.onDisconnect.addListener(()=>{const error=chrome.runtime.lastError;if(!finished)fail(Error(error?error.message:'Локальный приёмник отключён'));});
  port.postMessage({action:a.aac?'aacstream':'mp3stream',...identity(a)});
 });
}

if(typeof importScripts==='function')importScripts('background_download.js');
