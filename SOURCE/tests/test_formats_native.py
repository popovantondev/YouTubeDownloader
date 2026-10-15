from pathlib import Path
import sys,json,subprocess,base64

test_path=Path(__file__).with_name('test_native.py').resolve()
sys.argv=[str(test_path),sys.argv[1],sys.argv[2]]
source=test_path.read_text('utf-8')
ns={'__file__':str(test_path)}
exec(source[:source.index('results=[]')],ns)
work,ff,probe=ns['work'],ns['ff'],ns['probe']
run,stream,protocol,hashes=ns['run'],ns['stream'],ns['protocol'],ns['packet_hash']
sid,identity,audio=ns['sid'],ns['identity'],ns['audio']
results=[]
def capture(path,tag,height,codec,state,sourceHeight=None,tag_override=None,messages=None):
 records=messages if messages is not None else stream(path,tag,1)
 return protocol([dict(action='start',session=sid,height=height,sourceHeight=sourceHeight or height,itag=tag_override or tag,codec=codec,**identity)]+records+stream(audio,140,2)+[dict(action='finish',session=sid)],state)
for height,width,tag in [(144,256,160),(240,426,133),(360,640,134),(1080,1920,137)]:
 path=work/('added'+str(height)+'.mp4')
 run(['-f','lavfi','-i',f'color=c=blue:s={width}x{height}:r=25','-t','6','-an','-c:v','libx264','-preset','ultrafast','-bf','0','-g','50','-movflags','+frag_keyframe+empty_moov+default_base_moof','-frag_duration','2000000',str(path)])
 out=capture(path,tag,height,'h264','height'+str(height));assert out[-1]['ok'],out
 assert hashes(path,'v')==hashes(Path(out[-1]['path']),'v')
 results.append('PASS: '+str(height)+'p H.264 captured as the exact selected format; encoded video packets unchanged.')
out=capture(ns['video'],136,720,'vp9','wrong-codec');assert not out[-1]['ok'];results.append('PASS: H.264 cannot be reported as VP9; a selected codec mismatch is rejected.')
out=capture(ns['video'],136,720,'h264','wrong-itag',tag_override=137);assert not out[-1]['ok'];results.append('PASS: another received format does not silently replace the selected format.')
out=capture(ns['v480'],135,720,'h264','cropped',sourceHeight=480);assert out[-1]['ok'],out
assert hashes(ns['v480'],'v')==hashes(Path(out[-1]['path']),'v');results.append('PASS: coded height from metadata is respected for cropped/aspect-ratio variants, without scaling.')
av1=work/'av1-480.mp4';run(['-f','lavfi','-i','color=c=blue:s=854x480:r=5','-t','6','-an','-c:v','libaom-av1','-cpu-used','8','-g','10','-movflags','+frag_keyframe+empty_moov+default_base_moof','-frag_duration','2000000',str(av1)])
out=capture(av1,397,480,'av1','av1-full');assert out[-1]['ok'],out;assert hashes(av1,'v')==hashes(Path(out[-1]['path']),'v');results.append('PASS: full AV1 capture assembled and validated as AV1; encoded packets unchanged.')
vp9=work/'vp9-480.webm';run(['-f','lavfi','-i','color=c=blue:s=854x480:r=5','-t','6','-an','-c:v','libvpx-vp9','-cpu-used','8','-g','10','-cluster_time_limit','2000',str(vp9)])
raw=vp9.read_bytes();magic=b'\x1f\x43\xb6\x75';positions=[];p=0
while True:
 p=raw.find(magic,p)
 if p<0:break
 positions.append(p);p+=4
packet_data=json.loads(subprocess.check_output([str(probe),'-v','error','-select_streams','v','-show_packets','-of','json',str(vp9)]))['packets']
starts=[]
for i,pos in enumerate(positions):
 end=positions[i+1] if i+1<len(positions) else len(raw)
 starts.append(min(round(float(packet['pts_time'])*1000) for packet in packet_data if pos<=int(packet['pos'])<end))
messages=[]
pieces=[(-1,True,raw[:positions[0]],0,0)]+[(i,False,raw[pos:positions[i+1] if i+1<len(positions) else len(raw)],starts[i],(starts[i+1] if i+1<len(starts) else 6000)-starts[i]) for i,pos in enumerate(positions)]
for seq,init,piece,start,duration in pieces:
 r=dict(session=sid,itag=244,segment=seq,request=1,init=init,start=start,duration=duration,length=len(piece),offset=0)
 messages.append(dict(action='header',**r))
 for j in range(0,len(piece),12288):messages.append(dict(action='chunk',**r,data=base64.b64encode(piece[j:j+12288]).decode()))
 messages.append(dict(action='end',**r))
out=capture(vp9,244,480,'vp9','vp9-full',messages=messages);assert out[-1]['ok'],out;assert hashes(vp9,'v')==hashes(Path(out[-1]['path']),'v');results.append('PASS: full VP9 WebM clusters captured and remuxed to MP4 as VP9; encoded packets unchanged.')
report='\n'.join(results)+'\n'
(work/'formats_native_results.txt').write_text(report,'utf-8');print(report)
