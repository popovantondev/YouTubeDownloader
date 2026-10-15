(() => {
  'use strict';
  if (window.__YT_WIN7_ANDROID_03__) return;
  window.__YT_WIN7_ANDROID_03__ = true;
  const REQUEST = 'YT_WIN7_ANDROID_03_REQ';
  const RESPONSE = 'YT_WIN7_ANDROID_03_RESP';
  const CLIENT_VERSION = '21.26.364';
  function frameFormat(f) {
    return { itag: Number(f.itag) || 0, width: Number(f.width) || 0,
      height: Number(f.height) || 0, fps: Number(f.fps) || 0,
      mime: String(f.mimeType || '').slice(0, 128),
      direct: typeof f.url === 'string' && f.url.startsWith('https://'),
      ciphered: !!(f.signatureCipher || f.cipher) };
  }
  async function probe(videoId, download) {
    let key = '';
    try { key = String(window.ytcfg?.get?.('INNERTUBE_API_KEY') || ''); } catch (_) {}
    if (!key || !/^[A-Za-z0-9_-]{20,100}$/.test(key))
      return {ok:false,reason:'YouTube не предоставил API-ключ страницы. Обнови страницу и повтори.'};
    const client = {
      clientName: 'ANDROID', clientVersion: CLIENT_VERSION, androidSdkVersion:30,
      osName:'Android', osVersion:'11',
      userAgent:'com.google.android.youtube/21.26.364 (Linux; U; Android 11) gzip'
    };
    const body = {videoId, context:{client},
      playbackContext:{contentPlaybackContext:{html5Preference:'HTML5_PREF_WANTS'}}};
    let response;
    try {
      response = await fetch('/youtubei/v1/player?key='+encodeURIComponent(key)+'&prettyPrint=false', {
        method:'POST',credentials:'same-origin',cache:'no-store',
        headers:{'Content-Type':'application/json','X-YouTube-Client-Name':'3',
          'X-YouTube-Client-Version':CLIENT_VERSION},
        body: JSON.stringify(body)
      });
    } catch (_) { return {ok:false,reason:'Сетевой запрос к YouTube не выполнен.'}; }
    if (!response.ok) return {ok:false,status:response.status,
      reason:'YouTube отклонил запрос альтернативного клиента: HTTP '+response.status};
    let data;
    try { data=await response.json(); } catch (_) {
      return {ok:false,reason:'YouTube ответил не JSON.'};
    }
    const sd=data?.streamingData || {};
    const progressive=Array.isArray(sd.formats)?sd.formats:[];
    const adaptive=Array.isArray(sd.adaptiveFormats)?sd.adaptiveFormats:[];
    const candidates=progressive.filter(f=>
      Number(f.itag)===18 && typeof f.url==='string' &&
      /^video\/mp4/i.test(String(f.mimeType||'')) &&
      /avc1/i.test(String(f.mimeType||'')) &&
      /mp4a/i.test(String(f.mimeType||'')));
    const selected = candidates[0] || null;
    const result={ok:true,playability:String(data.playabilityStatus?.status||'unknown'),
      reason:String(data.playabilityStatus?.reason||'').slice(0,180),
      progressive:progressive.map(frameFormat),adaptiveCount:adaptive.length,
      sabrOnly:!progressive.some(f=>typeof f.url==='string'&&f.url.startsWith('https://')) &&
        !!sd.serverAbrStreamingUrl,
      direct360:!!selected,
      canDownload:false};
    if(download && selected) {
      try {
        const u=new URL(selected.url);
        if (u.protocol!=='https:' || !/(^|\.)googlevideo\.com$/i.test(u.hostname))
          return {...result,reason:'Получен адрес потока с неподходящего домена.'};
        result.canDownload=true;
        result.downloadUrl=selected.url; // internal use only; removed by content report
        result.title=String(data.videoDetails?.title||'YouTube-'+videoId).slice(0,180);
      } catch (_) {result.reason='YouTube вернул некорректный адрес.';}
    }
    return result;
  }
  window.addEventListener('message', async event => {
    const data=event.data;
    if(event.source!==window || event.origin!==location.origin || data?.source!==REQUEST ||
       typeof data.id!=='string' || !/^[A-Za-z0-9_\-]{8,80}$/.test(data.id))return;
    const v=new URL(location.href).searchParams.get('v');
    if(!v || !/^[a-zA-Z0-9_-]{11}$/.test(v))return;
    const result=await probe(v,!!data.download);
    window.postMessage({source:RESPONSE,id:data.id,videoId:v,result},location.origin);
  });
})();
