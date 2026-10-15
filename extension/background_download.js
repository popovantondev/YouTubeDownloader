// Browser worker only: no HTML video, no decoder, bounded native writes.
(() => {
 'use strict';let active=null;
 const valid=s=>{const u=new URL(s);if(u.protocol!=='https:'||!/(^|\.)googlevideo\.com$/i.test(u.hostname))throw Error('Неверный адрес Googlevideo.');return u;};
 chrome.runtime.onConnect.addListener(port=>{
  if(port.name!=='ytwin7-background24')return;let task=null;
  const tell=data=>{try{port.postMessage(data);}catch(_){}};
  const status=(value,text)=>tell({action:'status',progress:Math.max(0,Math.min(90,Math.floor(value))),message:text});
  const stop=()=>{if(!task)return;task.cancelled=true;task.fail?.(Error('Отменено'));task.abort.abort();try{task.stream?.abort();}catch(_){}try{task.native?.postMessage({action:'cancel'});}catch(_){}try{task.native?.disconnect();}catch(_){}for(const w of task.waiters.values()){clearTimeout(w.timer);w.reject(Error('Отменено'));}task.waiters.clear();if(active===task){active=null;globalThis.__ytw7BackgroundTransfer=false;}};
  port.onDisconnect.addListener(stop);
  port.onMessage.addListener(async message=>{
   if(message.action==='cancel'){stop();return;}if(message.action!=='start'||task)return;
   if(active||mediaBusy||qualityCaptures.size){tell({action:'result',ok:false,error:'Дождись завершения текущей загрузки.'});return;}
   let identityData;try{identityData=context(message,port.sender);if(![144,240,360,480,720,1080].includes(Number(message.height)))throw Error('Неверное качество.');}catch(e){tell({action:'result',ok:false,error:e.message});return;}
   task=active={...identityData,cancelled:false,abort:new AbortController(),waiters:new Map(),serial:Promise.resolve(),part:0,bytes:0,started:Date.now()};
   task.failure=new Promise((_,reject)=>{task.fail=error=>{if(task.error)return;task.error=error;task.abort.abort();try{task.stream?.abort();}catch(_){}for(const waiter of task.waiters.values()){clearTimeout(waiter.timer);waiter.reject(error);}task.waiters.clear();reject(error);};});task.failure.catch(()=>{});task.race=promise=>Promise.race([promise,task.failure]);
   globalThis.__ytw7BackgroundTransfer=true;try{
    const source=message.source;if(!source||!['direct','sabr'].includes(source.kind))throw Error('Неизвестный источник.');
    const video=source.video,audio=source.audio;if(!video||!audio||!['h264','vp9','av1'].includes(source.codec)||!Number.isInteger(video.itag)||!Number.isInteger(audio.itag)||!/^video\//.test(video.mimeType)||!/^audio\//.test(audio.mimeType))throw Error('Некорректные сведения о потоках.');
    const preciseDuration=Number(video.approxDurationMs);if(preciseDuration>=1000&&preciseDuration<=21600000){if(Math.abs(task.duration-preciseDuration)>2000)throw Error('Длительность источника не совпадает с роликом.');task.duration=Math.round(preciseDuration);}
    const labelled=Number(String(video.qualityLabel||video.height).match(/\d+/)?.[0]);const actualCodec=/avc1/i.test(video.mimeType)?'h264':/vp0?9/i.test(video.mimeType)?'vp9':/av01/i.test(video.mimeType)?'av1':'';if(labelled!==Number(task.height)||actualCodec!==source.codec||!Number.isInteger(Number(video.height))||video.height<1||video.height>8192)throw Error('Источник не соответствует выбранному качеству.');
    if(source.kind==='direct'){valid(video.url);valid(audio.url);}else{valid(source.url);if(!Array.isArray(source.template)||source.template.length>262144||!source.template.every(b=>Number.isInteger(b)&&b>=0&&b<=255)||typeof source.config!=='string'||source.config.length>262144)throw Error('Некорректная конфигурация SABR.');}
    const native=task.native=chrome.runtime.connectNative('com.ytwin7.sabr');let resultReceived=false;
    const ready=new Promise((resolve,reject)=>{task.readyResolve=resolve;task.readyReject=reject;task.readyTimer=setTimeout(()=>task.fail(Error('Локальный модуль не готов за 3 минуты.')),180000);});
    native.onMessage.addListener(m=>{if(m.action==='ready'){clearTimeout(task.readyTimer);task.cached=!!m.audioCached;task.readyResolve();}else if(m.action==='trackAck'){const w=task.waiters.get(m.part);if(w){task.waiters.delete(m.part);clearTimeout(w.timer);w.resolve();}}else if(m.action==='status')status(m.progress,m.message);else if(m.action==='result'){resultReceived=true;if(!m.ok){task.fail(Error(m.error||'Ошибка локального модуля'));}else task.resultResolve?.(m);}});
    native.onDisconnect.addListener(()=>{if(!resultReceived&&!task.cancelled){const error=Error(chrome.runtime.lastError?.message||'Локальный модуль отключён');task.fail(error);}});
    native.postMessage({action:'backgroundStart',...identity(task),height:task.height,sourceHeight:Number(video.height)||task.height,codec:source.codec});await task.race(ready);
    function write(track,bytes){task.serial=task.serial.then(async()=>{for(let offset=0;offset<bytes.length;offset+=32768){if(task.cancelled)throw Error('Отменено');const piece=bytes.subarray(offset,offset+32768),part=++task.part;let raw='';for(const byte of piece)raw+=String.fromCharCode(byte);const ack=new Promise((resolve,reject)=>task.waiters.set(part,{resolve,reject,timer:setTimeout(()=>task.fail(Error('Запись фрагмента не подтверждена за 2 минуты.')),120000)}));native.postMessage({action:'trackChunk',track,part,data:btoa(raw)});await task.race(ack);task.bytes+=piece.length;}});return task.serial;}
    async function consume(track,reader,discard=false){try{while(!task.cancelled){let timer;const chunk=await task.race(Promise.race([reader.read(),new Promise((_,reject)=>timer=setTimeout(()=>reject(Error('Сетевой поток не отвечает 3 минуты.')),180000))])).finally(()=>clearTimeout(timer));if(chunk.done)break;if(!discard)await write(track,chunk.value);if(source.kind==='direct'){const total=Number(video.contentLength)+(task.cached?0:Number(audio.contentLength));status(total>0?task.bytes/total*89:0,'Фоновая загрузка '+task.height+'p: '+(task.bytes/1048576).toFixed(1)+' МБ · видео на паузе');}}}finally{try{await reader.cancel();}catch(_){}}}
    status(0,'Фоновая загрузка '+task.height+'p · '+source.codec.toUpperCase()+' · видео на паузе');
    if(source.kind==='direct'){
     for(const [track,format] of [['video',video],['audio',audio]]){if(track==='audio'&&task.cached)continue;let timer;const response=await task.race(Promise.race([fetch(valid(format.url),{signal:task.abort.signal}),new Promise((_,reject)=>timer=setTimeout(()=>reject(Error('Сервер не ответил за 3 минуты.')),180000))])).finally(()=>clearTimeout(timer));if(!response.ok||!response.body)throw Error('Прямая загрузка отклонена: HTTP '+response.status);await consume(track,response.body.getReader());}
    }else{
     task.stream=YTWin7Sabr.createStream({videoId:task.videoId,serverAbrStreamingUrl:source.url,videoPlaybackUstreamerConfig:source.config,template:source.template,formats:[video,audio],durationMs:task.duration,clientInfo:{clientName:1,clientVersion:'2.0'},onProgress:ms=>status(ms/task.duration*89,'Фоновая загрузка '+task.height+'p: '+Math.min(89,Math.floor(ms/task.duration*89))+'% · '+(task.bytes/1048576).toFixed(1)+' МБ · видео на паузе')});
     task.stream.on('error',e=>{task.lastError=e;});
     const streams=await task.stream.start({videoFormat:video.itag,audioFormat:audio.itag,maxRetries:2,stallDetectionMs:120000});
     await task.race(Promise.all([consume('video',streams.videoStream.getReader()),consume('audio',streams.audioStream.getReader(),task.cached)]));if(task.lastError)throw task.lastError;
    }
    if(task.cancelled)throw Error('Отменено');await task.serial;
    const complete=new Promise((resolve,reject)=>{task.resultResolve=resolve;task.resultReject=reject;});native.postMessage({action:'backgroundFinish'});let finishTimer;const result=await task.race(Promise.race([complete,new Promise((_,reject)=>finishTimer=setTimeout(()=>reject(Error('Сборка файла не ответила за 10 минут.')),600000))])).finally(()=>clearTimeout(finishTimer));tell(result);resultReceived=true;
   }catch(e){if(!task.cancelled)tell({action:'result',ok:false,retryableFormat:/sabr\.no_video_selected/i.test(String(e.message||e)),rejectedItag:message.source?.video?.itag,error:String(e.message||e).replace(/https?:\/\/\S+/g,'[адрес]').slice(0,250)});}finally{clearTimeout(task.readyTimer);task.serial.catch(()=>{});stop();}
  });
 });
})();
