from pathlib import Path
import sys,base64,json,subprocess
test=Path(__file__).with_name('test_native.py').resolve();sys.argv=[str(test),sys.argv[1],sys.argv[2]]
ns={'__file__':str(test)};s=test.read_text('utf-8');exec(s[:s.index('results=[]')],ns)
original_protocol=ns['protocol']
def numbered_protocol(messages,state):
 messages=[dict(m) for m in messages];part=0
 for m in messages:
  if m['action']=='trackChunk':part+=1;m['part']=part
 return original_protocol(messages,state)
ns['protocol']=numbered_protocol
def transfer(path,track):
 data=path.read_bytes()
 return [dict(action='trackChunk',track=track,part=i//32768+1,data=base64.b64encode(data[i:i+32768]).decode()) for i in range(0,len(data),32768)]
begin=dict(action='backgroundStart',height=720,sourceHeight=720,codec='h264',**ns['identity'])
messages=[begin]+transfer(ns['video'],'video')+transfer(ns['audio'],'audio')+[dict(action='backgroundFinish')]
out=ns['protocol'](messages,'background-complete');assert out[-1]['ok'],out
assert ns['packet_hash'](Path(out[-1]['path']),'v')==ns['packet_hash'](ns['video'],'v')
assert ns['packet_hash'](Path(out[-1]['path']),'a')==ns['packet_hash'](ns['audio'],'a')
assert any(m['action']=='trackAck' for m in out)
cached=ns['protocol']([begin]+transfer(ns['video'],'video')+[dict(action='backgroundFinish')],'background-complete');assert cached[0]['audioCached'] and cached[-1]['ok'],cached
assert ns['packet_hash'](Path(cached[-1]['path']),'a')==ns['packet_hash'](ns['audio'],'a')
partial=ns['work']/'partial.mp4';init,parts=ns['fragments'](ns['video']);partial.write_bytes(init+parts[0])
bad=ns['protocol']([begin]+transfer(partial,'video')+transfer(ns['audio'],'audio')+[dict(action='backgroundFinish')],'background-partial');assert not bad[-1]['ok'],bad
assert not list((ns['work']/'background-partial').rglob('*.mp4'))
cancel=ns['protocol']([begin]+transfer(ns['video'],'video')+[dict(action='cancel')],'background-cancel');assert not list((ns['work']/'background-cancel'/'MEDIA_TEMP').glob('*'))
wrong=ns['protocol']([{**begin,'height':480,'sourceHeight':480}]+transfer(ns['video'],'video')+transfer(ns['audio'],'audio')+[dict(action='backgroundFinish')],'background-wrong-height');assert not wrong[-1]['ok']
# Reproducible UMP fixture: both tracks and real fragmented media; no external URL/token.
fixture={'videoId':ns['vid'],'duration':6000,'video':{'itag':136,'lastModified':'10000','xtags':'','width':1280,'height':720,'mimeType':'video/mp4; codecs="avc1.4d401f"','bitrate':1000000},'audio':{'itag':140,'lastModified':'10000','xtags':'','mimeType':'audio/mp4; codecs="mp4a.40.2"','bitrate':128000},'tracks':{}}
for kind,path in [('video',ns['video']),('audio',ns['audio'])]:
 init,parts=ns['fragments'](path);track=[{'init':True,'seq':-1,'start':0,'duration':0,'data':base64.b64encode(init).decode()}]
 for i,part in enumerate(parts):
  item=ns['work']/(kind+str(i)+'.mp4');item.write_bytes(init+part)
  result=subprocess.run([str(ns['probe']),'-v','error','-show_packets','-show_entries','packet=pts_time,duration_time','-of','json',str(item)],capture_output=True);assert result.returncode==0
  packets=json.loads(result.stdout)['packets'];start=float(packets[0]['pts_time']);end=float(packets[-1]['pts_time'])+float(packets[-1].get('duration_time',1/25 if kind=='video' else 1024/48000));track.append({'init':False,'seq':i,'start':round(start*1000),'duration':round((end-start)*1000),'data':base64.b64encode(part).decode()})
 fixture['tracks'][kind]=track
(ns['work']/'ump_fixture.json').write_text(json.dumps(fixture),'utf-8')
report='PASS: background tracks are acknowledged and remuxed without changing video/audio packets.\nPASS: cached audio needs no incoming audio track; partial video and wrong height are rejected.\nPASS: cancel removes background scratch files from LocalAppData-style test folders.\n'
(ns['work']/'background_native_results.txt').write_text(report,'utf-8');print(report)
