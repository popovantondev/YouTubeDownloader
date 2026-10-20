(() => {
 'use strict';if(window.__YTWIN7_UI20)return;window.__YTWIN7_UI20=true;
 let job=null,captureBusy=false,lastProgress=0,lastText='Выбери формат. Видео загружается без просмотра.';
 const id=()=>new URLSearchParams(location.search).get('v');
 const node=name=>document.getElementById('ytw7-'+name);
 const text=key=>window.YTD?.text(key)||key;
 const tr=value=>window.YTD?window.YTD.translate(value):value;
 function progress(value,message){lastProgress=Math.max(0,Math.min(100,window.YTW7.finite(value,lastProgress)));lastText=message||lastText;if(node('progress'))node('progress').value=lastProgress;if(node('status'))node('status').textContent=tr(lastText)+' ('+lastProgress+'%)';}
 function busy(){window.__ytw7MediaBusy=!!job;for(const button of document.querySelectorAll('[data-ytd-media]'))button.disabled=!!job||captureBusy;for(const button of document.querySelectorAll('[data-ytw7-format]'))button.disabled=!!job||captureBusy;if(node('media-cancel'))node('media-cancel').hidden=!job||job.handingOff;}
 function done(message,p=0){if(job?.timer)clearTimeout(job.timer);if(job?.poll)clearInterval(job.poll);job=null;busy();progress(p,message);}
 function identity(){const player=document.getElementById('movie_player');if(player?.classList.contains('ad-showing'))throw Error(text('waitingAudio'));const v=document.querySelector('video.html5-main-video');if(!v||!Number.isFinite(v.duration)||v.duration<1)throw Error(text('waitingAudio'));return {videoId:id(),title:document.title.replace(/\s*-\s*YouTube\s*$/,'').slice(0,512),duration:Math.round(v.duration*1000),language:window.YTD?.settings.language||'en'};}
 function send(message){return new Promise((resolve,reject)=>chrome.runtime.sendMessage(message,r=>{const e=chrome.runtime.lastError;if(e||!r||!r.ok)reject(Error(e?e.message:r?.error||'Ошибка расширения'));else resolve(r);}));}
 async function waitIdentity(operation){const start=Date.now();for(;;){if(job!==operation||id()!==operation.videoId)throw Error('Отменено');try{return identity();}catch(_){if(Date.now()-start>=150000)throw Error(text('waitExpired'));progress(0,text('waitingAudio'));await new Promise(resolve=>setTimeout(resolve,600));}}}
 async function start(which){
  if(job||captureBusy)return;const operation={videoId:id(),which,token:'probe_'+Date.now()+Math.random().toString(36).slice(2)};job=operation;busy();
  try{
   Object.assign(operation,await waitIdentity(operation));if(job!==operation)return;
   if(which==='peak'){const desktop=await send({action:'checkDesktop',...operation});if(job!==operation)return;if(!desktop.installed)throw Error(text('desktopMissing'));}
   progress(0,'Подготавливаю '+(which==='peak'?'AAC → PeakMP3':which==='mp3'?'MP3':which==='aac'?'AAC':'360p')+'…');
   if(['mp3','aac','peak'].includes(which)){const r=await send({action:which==='peak'?'cachedProcessedMP3':which==='aac'?'cachedAAC':'cachedMP3',...operation,operationId:operation.token});if(job!==operation)return;if(!r.needsUrl)return;}
   await send({action:'preparePage'});if(job!==operation)return;
   operation.timer=setTimeout(()=>{if(job===operation)done('YouTube долго не отвечает. Повтори попытку.');},180000);
   window.postMessage({source:'YT_WIN7_ANDROID_03_REQ',id:operation.token,download:true,audioOnly:which!=='360'},location.origin);
  }catch(e){if(job===operation)done(e.message);}
 }
 window.addEventListener('message',async event=>{
  if(event.source!==window||event.origin!==location.origin||!job||event.data?.source!=='YT_WIN7_ANDROID_03_RESP'||event.data.id!==job.token)return;
  const operation=job;clearTimeout(operation.timer);if(event.data.videoId!==operation.videoId){done('Открыт другой ролик.');return;}
  const result=event.data.result||{};if(!result.canDownload||!result.downloadUrl){done((operation.which==='360'?'Нет прямого MP4 360p: ':text('audioUnavailable'))+(result.reason||'YouTube не предоставил ссылку.'));return;}
  if(Number.isSafeInteger(result.duration)&&result.duration>=1000&&result.duration<=21600000)operation.duration=result.duration;
  try{
   const response=await send({action:operation.which==='peak'?'downloadProcessedMP3':operation.which==='mp3'?'downloadMP3':operation.which==='aac'?'downloadAAC':'download360',videoId:operation.videoId,title:result.title||operation.title,duration:operation.duration,url:result.downloadUrl,language:operation.language,operationId:operation.token});
   if(job!==operation)return;if(operation.which!=='360'){progress(0,'Получаю звук в локальную папку приложения…');return;}
   progress(0,'Скачиваю 360p…');operation.poll=setInterval(async()=>{if(job!==operation)return;if(id()!==operation.videoId){done('Открыт другой ролик. Загрузка 360p продолжается в браузере.');return;}try{const p=await send({action:'downloadProgress',...operation,downloadId:response.downloadId});if(job!==operation)return;if(p.state==='complete')done('360p скачан. Звук сохраняется для повторного использования.',100);else if(p.state==='interrupted')done('Загрузка 360p прервана.');else progress(p.progress,'Скачиваю 360p…');}catch(e){if(job===operation)done(e.message);}},1000);
  }catch(e){if(job===operation)done(e.message);}
 });
 chrome.runtime.onMessage.addListener(message=>{if(message?.type!=='yt-win7-status'||message.videoId!==id())return;if(message.operationId&&(!job||message.operationId!==job.token))return;if(message.done)done(message.message,message.progress);else{if(job?.which==='peak'&&message.progress>=96){job.handingOff=true;busy();}progress(message.progress,message.message);}});
 window.addEventListener('ytw7-progress',event=>progress(event.detail.value,event.detail.text));
 window.addEventListener('ytw7-capture-busy',event=>{captureBusy=!!event.detail;busy();});
 function closeMenu(){if(node('mp3-menu'))node('mp3-menu').hidden=true;if(node('mp3-options'))node('mp3-options').setAttribute('aria-expanded','false');}
 function mount(){
  if(location.pathname!=='/watch'||!id()||node('panel'))return;const target=document.querySelector('ytd-watch-metadata')||document.querySelector('#below');if(!target)return;
  const panel=document.createElement('div');panel.id='ytw7-panel';panel.style.cssText='background:#edf6f0;color:#173c2a;border:1px solid #82b99a;border-radius:12px;padding:14px;margin:12px 0;font:14px Arial';
  const title=document.createElement('b');title.textContent='YouTube Downloader';panel.appendChild(title);
  const row=document.createElement('div');row.style.cssText='display:flex;gap:10px;flex-wrap:wrap;margin:10px 0';
  for(const [type,label]of[['360','360p · H.264'],['mp3','♫ MP3 320'],['aac','♫ AAC']]){
   const button=document.createElement('button');button.id='ytw7-'+type;button.type='button';if(type==='360')window.YTW7.decorate(button,'360p · H.264');else button.textContent=label;
   button.style.cssText='padding:10px;background:#147b45;color:white;border:0;border-radius:7px;cursor:pointer;font:bold 13px Arial;align-items:center;gap:6px';button.onclick=()=>{closeMenu();start(type);};button.setAttribute('data-ytd-media',type);
   if(type!=='mp3'){row.appendChild(button);continue;}
   const wrapper=document.createElement('div');wrapper.id='ytw7-mp3-wrap';wrapper.style.cssText='display:inline-flex;position:relative;gap:2px';wrapper.appendChild(button);
   const arrow=document.createElement('button');arrow.id='ytw7-mp3-options';arrow.type='button';arrow.textContent='▾';arrow.style.cssText='padding:8px;background:#147b45;color:white;border:0;border-radius:7px;cursor:pointer;font:bold 15px Arial';arrow.setAttribute('data-ytd-media','options');arrow.setAttribute('aria-haspopup','menu');arrow.setAttribute('aria-expanded','false');
   const menu=document.createElement('div');menu.id='ytw7-mp3-menu';menu.setAttribute('role','menu');menu.hidden=true;menu.style.cssText='position:absolute;z-index:2100;top:100%;left:0;min-width:235px;padding:5px;border:1px solid #82b99a;border-radius:8px;box-shadow:0 5px 18px #0003';
   for(const [key,which]of[['mp3Plain','mp3'],['mp3Peak','peak']]){const choice=document.createElement('button');choice.type='button';choice.setAttribute('role','menuitem');choice.dataset.audioChoice=key;choice.setAttribute('data-ytd-media',which);choice.style.cssText='display:block;width:100%;text-align:left;padding:10px;border:0;border-radius:5px;cursor:pointer;background:transparent;color:inherit;font:13px Arial';choice.onclick=()=>{closeMenu();start(which);};menu.appendChild(choice);}
   arrow.onclick=event=>{event.stopPropagation();menu.hidden=!menu.hidden;arrow.setAttribute('aria-expanded',String(!menu.hidden));if(!menu.hidden)menu.querySelector('button').focus();};wrapper.append(arrow,menu);row.appendChild(wrapper);
  }
  panel.appendChild(row);const cancel=document.createElement('button');cancel.id='ytw7-media-cancel';cancel.type='button';cancel.hidden=true;cancel.style.cssText='padding:6px 12px;margin-bottom:8px;border:1px solid #82b99a;border-radius:6px;cursor:pointer';cancel.onclick=()=>{const operation=job;if(!operation||operation.handingOff)return;if(operation.duration&&operation.which!=='360')send({action:'cancelMedia',...operation}).catch(()=>{});done('Отменено');};panel.appendChild(cancel);
  const bar=document.createElement('progress');bar.id='ytw7-progress';bar.max=100;bar.style.cssText='width:100%;height:20px';panel.appendChild(bar);
  const status=document.createElement('div');status.id='ytw7-status';status.style.cssText='font:12px/1.5 Arial;white-space:pre-wrap;margin-top:8px';panel.appendChild(status);target.appendChild(panel);applySettings();
  const saved=window.__ytw7CaptureStatus;if(saved){captureBusy=saved.busy;lastProgress=saved.value;lastText=saved.text;}busy();progress(lastProgress,lastText);
 }
 function applySettings(){
  if(!window.YTD)return;const settings=window.YTD.settings;
  for(const type of['360','mp3','aac']){const b=node(type);if(!b)continue;b.style.display=settings.visible[type==='360'?'quick360':type]?'inline-flex':'none';if(type==='mp3')b.textContent='♫ MP3 320 '+text('kbps');if(type==='aac')b.textContent='♫ '+text('aac');}
  if(node('mp3-wrap'))node('mp3-wrap').style.display=settings.visible.mp3?'inline-flex':'none';
  if(node('mp3-options'))node('mp3-options').setAttribute('aria-label',text('audioMenu'));
  for(const button of document.querySelectorAll('[data-audio-choice]'))button.textContent=text(button.dataset.audioChoice);
  if(node('media-cancel'))node('media-cancel').textContent=text('cancel');
  const dark=settings.theme==='dark'||settings.theme==='system'&&window.matchMedia?.('(prefers-color-scheme:dark)').matches;
  if(node('panel')){node('panel').style.background=dark?'#12261c':'#edf6f0';node('panel').style.color=dark?'#e7f5ec':'#173c2a';}if(node('mp3-menu'))node('mp3-menu').style.background=dark?'#12261c':'#edf6f0';
  progress(lastProgress,lastText);if(!job&&!captureBusy&&!Object.values(settings.visible).some(Boolean)&&node('status'))node('status').textContent=text('noFormats');
 }
 document.addEventListener('click',event=>{if(!node('mp3-wrap')?.contains(event.target))closeMenu();});document.addEventListener('keydown',event=>{if(event.key==='Escape')closeMenu();});
 window.addEventListener('ytd-settings-change',applySettings);window.YTD?.ready.then(applySettings);window.YTW7.fast360=()=>start('360');
 document.addEventListener('yt-navigate-finish',()=>{if(job&&id()!==job.videoId)done('Открыт другой ролик.');closeMenu();setTimeout(mount,400);});mount();setInterval(mount,1800);
})();
