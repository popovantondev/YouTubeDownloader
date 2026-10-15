(() => {
 'use strict';if(globalThis.YTD)return;
 const KEY='youtubeDownloader.settings.v1';const visible={quick360:true,mp3:true,aac:false,original144:false,original240:false,original360:false,original480:true,original720:true,original1080:false};
 const language=String(globalThis.chrome?.i18n?.getUILanguage?.()||globalThis.navigator?.language||'en').slice(0,2);let legacy=1;try{legacy=Number(localStorage.getItem('ytw7_capture_rate23'))||1;}catch(_){}
 const defaults={language:['ru','de','en'].includes(language)?language:'en',visible,mode:'background',captureRate:[1,2,4].includes(legacy)?legacy:1,theme:'system'};
 const sanitize=value=>({language:['ru','de','en'].includes(value?.language)?value.language:defaults.language,visible:Object.fromEntries(Object.keys(visible).map(k=>[k,typeof value?.visible?.[k]==='boolean'?value.visible[k]:visible[k]])),mode:value?.mode==='capture'?'capture':'background',captureRate:[1,2,4].includes(Number(value?.captureRate))?Number(value.captureRate):defaults.captureRate,theme:['light','dark','system'].includes(value?.theme)?value.theme:'system'});
 const y=globalThis.YTD={settings:sanitize(defaults),defaults:sanitize(defaults),key:KEY,sanitize,ready:Promise.resolve()};
 const emit=()=>globalThis.dispatchEvent?.(new CustomEvent('ytd-settings-change',{detail:y.settings}));
 y.save=async value=>{const next=sanitize(value);await new Promise((resolve,reject)=>chrome.storage.local.set({[KEY]:next},()=>chrome.runtime.lastError?reject(Error(chrome.runtime.lastError.message)):resolve()));y.settings=next;emit();};
 if(chrome?.storage?.local){y.ready=new Promise(resolve=>chrome.storage.local.get(KEY,result=>{y.settings=sanitize(result?.[KEY]);resolve();emit();}));chrome.storage.onChanged.addListener((changes,area)=>{if(area==='local'&&changes[KEY]){y.settings=sanitize(changes[KEY].newValue);emit();}});}
})();
