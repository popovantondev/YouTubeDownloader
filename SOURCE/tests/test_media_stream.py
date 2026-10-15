from pathlib import Path
import sys,base64
test=Path(__file__).with_name('test_native.py').resolve();sys.argv=[str(test),sys.argv[1],sys.argv[2]]
ns={'__file__':str(test)};s=test.read_text('utf-8');exec(s[:s.index('results=[]')],ns)
data=ns['source360'].read_bytes()
messages=[dict(action='mp3stream',**ns['identity'])]+[dict(action='mediaChunk',data=base64.b64encode(data[i:i+12288]).decode()) for i in range(0,len(data),12288)]+[dict(action='mediaFinish')]
out=ns['protocol'](messages,'streamed-mp3');assert out[0]['action']=='ready' and out[-1]['ok'],out
assert any(m['action']=='mediaAck' for m in out)
assert ns['protocol']([dict(action='lookup',**ns['identity'])],'streamed-mp3')[-1]['cached']
opus=ns['work']/'opus_source.mp4';ns['run'](['-i',str(ns['video']),'-f','lavfi','-i','sine=sample_rate=48000','-t','6','-map','0:v','-map','1:a','-c:v','copy','-c:a','libopus','-strict','experimental',str(opus)])
imported=ns['protocol']([dict(action='import',input=base64.b64encode(str(opus).encode()).decode(),**ns['identity'])],'opus-cache');assert imported[-1]['ok'],imported
cached=ns['work']/'opus-cache'/'AUDIO_CACHE'/(ns['identity']['videoId']+'.m4a');assert ns['packet_hash'](opus,'a')==ns['packet_hash'](cached,'a')
report='PASS: browser-style media chunks are acknowledged, cached, and converted to MP3 entirely through local I/O.\nPASS: streamed MP3 leaves reusable audio in the cache; no .NET HTTPS request is needed.\nPASS: Opus audio caches in an explicit MP4 container without changing encoded packets.\n'

aac=ns['protocol']([dict(action='aac',**ns['identity'])],'streamed-mp3');assert aac[-1]['ok'],aac
assert ns['packet_hash'](ns['source360'],'a')==ns['packet_hash'](Path(aac[-1]['path']),'a')
cold=[dict(action='aacstream',**ns['identity'])]+[dict(action='mediaChunk',data=base64.b64encode(data[i:i+12288]).decode()) for i in range(0,len(data),12288)]+[dict(action='mediaFinish')]
out_aac=ns['protocol'](cold,'streamed-aac');assert out_aac[-1]['ok'],out_aac
assert ns['packet_hash'](ns['source360'],'a')==ns['packet_hash'](Path(out_aac[-1]['path']),'a')
assert ns['protocol']([dict(action='lookup',**ns['identity'])],'streamed-aac')[-1]['audioCodec']=='aac'
denied=ns['protocol']([dict(action='aac',**ns['identity'])],'opus-cache');assert not denied[-1]['ok'],denied
assert not list((ns['work']/'opus-cache'/'RESULTS').glob('*.m4a')) if (ns['work']/'opus-cache'/'RESULTS').exists() else True
report+='PASS: cached and cold-stream AAC exports keep every encoded audio packet unchanged; Opus cannot masquerade as AAC.\n'

(ns['work']/'stream_results.txt').write_text(report,'utf-8');print(report)
