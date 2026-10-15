from pathlib import Path
import json,struct,subprocess,base64,zipfile,hashlib

import sys
pkg=Path(__file__).resolve().parents[2]
work=Path(sys.argv[2] if len(sys.argv)>2 else 'ytw7-native-tests').resolve();work.mkdir(exist_ok=True)
ff=Path(__import__('os').environ.get('YTD_FIXTURE_FFMPEG',str(pkg/'ffmpeg.exe')));host=pkg/'YTWin7SabrHost.exe'
if len(sys.argv)<2:raise SystemExit('Usage: python test_native.py path/to/ffprobe.exe [scratch-directory]')
probe=Path(sys.argv[1]).resolve()
def run(args):
 p=subprocess.run([str(ff),'-nostdin','-hide_banner','-loglevel','error','-y']+args,capture_output=True)
 assert p.returncode==0,p.stderr.decode(errors='replace')
def metadata(path):
 p=subprocess.run([str(probe),'-v','error','-show_streams','-show_format','-of','json',str(path)],capture_output=True)
 assert p.returncode==0,p.stderr
 return json.loads(p.stdout)
def protocol(messages,state):
 raw=b''
 for msg in messages:
  j=json.dumps(msg,ensure_ascii=False).encode();raw+=struct.pack('<I',len(j))+j
 p=subprocess.run([str(host),'--test-root',str(work/state)],input=raw,capture_output=True,timeout=60)
 assert not p.stderr,p.stderr
 out=[];pos=0
 while pos<len(p.stdout):
  n=struct.unpack_from('<I',p.stdout,pos)[0];pos+=4
  out.append(json.loads(p.stdout[pos:pos+n]));pos+=n
 return out
def fragments(path):
 data=path.read_bytes();pos=0;init=b'';parts=[];cur=b''
 while pos<len(data):
  n=struct.unpack_from('>I',data,pos)[0];kind=data[pos+4:pos+8];assert n>=8
  block=data[pos:pos+n];pos+=n
  if kind==b'moof':
   if cur:parts.append(cur)
   cur=block
  elif cur:cur+=block
  else:init+=block
 if cur:parts.append(cur)
 return init,parts
def packet_hash(path,kind):
 p=subprocess.run([str(probe),'-v','error','-select_streams',kind,'-show_packets','-show_data_hash','sha256','-show_entries','packet=data_hash','-of','json',str(path)],capture_output=True)
 assert p.returncode==0,p.stderr
 return [x['data_hash'] for x in json.loads(p.stdout)['packets']]

common=['-f','lavfi','-i','color=c=blue:s=1280x720:r=25','-t','6','-an','-c:v','libx264','-preset','ultrafast','-bf','0','-g','50','-pix_fmt','yuv420p','-movflags','+frag_keyframe+empty_moov+default_base_moof','-frag_duration','2000000']
video=work/'video720.mp4';run(common+[str(video)])
v480=work/'video480.mp4';run(['-f','lavfi','-i','color=c=red:s=854x480:r=25','-t','6','-an','-c:v','libx264','-preset','ultrafast','-bf','0','-g','50','-movflags','+frag_keyframe+empty_moov+default_base_moof','-frag_duration','2000000',str(v480)])
audio=work/'audio.m4a';run(['-f','lavfi','-i','sine=frequency=800:sample_rate=48000','-t','6','-vn','-c:a','aac','-b:a','128k','-movflags','+frag_keyframe+empty_moov+default_base_moof','-frag_duration','2000000',str(audio)])
source360=work/'source360.mp4';run(['-i',str(video),'-i',str(audio),'-map','0:v','-map','1:a','-vf','scale=640:360','-c:v','libx264','-preset','ultrafast','-c:a','copy',str(source360)])
sid='testVideo01-test-session';vid='testVideo01'
identity=dict(videoId=vid,title='Тест исходного качества',duration=6000)
def stream(path,tag,req,omit=None,truncate=False):
 init,parts=fragments(path);messages=[]
 entries=[(-1,True,init)]+[(i,False,p) for i,p in enumerate(parts)]
 for seq,isinit,p in entries:
  if seq==omit:continue
  r=dict(session=sid,itag=tag,segment=seq,request=req,init=isinit,start=max(seq,0)*2000,duration=2000 if not isinit else 0,length=len(p),offset=0)
  messages.append(dict(action='header',**r))
  if truncate and not isinit and seq==1:p=p[:-20]
  for j in range(0,len(p),12288):messages.append(dict(action='chunk',**r,data=base64.b64encode(p[j:j+12288]).decode()))
  messages.append(dict(action='end',**r))
 return messages
def capture(vpath,tag,h,state,**options):
 messages=[dict(action='start',session=sid,height=h,**identity)]+stream(vpath,tag,1,**options)+stream(audio,140,2)+[dict(action='finish',session=sid)]
 return protocol(messages,state)

results=[]
out=capture(video,136,720,'complete720');assert out[-1]['ok'],out
dest=Path(out[-1]['path']);meta=metadata(dest)
assert next(s for s in meta['streams'] if s['codec_type']=='video')['height']==720
assert packet_hash(video,'v')==packet_hash(dest,'v'),'Video packets changed'
assert packet_hash(audio,'a')==packet_hash(dest,'a'),'Audio packets changed'
results.append('PASS: H.264/AAC 720p captured; output video/audio packet hashes identical to source.')
out=capture(v480,135,480,'complete480');assert out[-1]['ok'],out
assert packet_hash(v480,'v')==packet_hash(Path(out[-1]['path']),'v')
results.append('PASS: H.264/AAC 480p captured without changing video packets.')
for name,options in [('missing',{'omit':1}),('truncated',{'truncate':True})]:
 out=capture(video,136,720,name,**options);assert not out[-1]['ok'],out
 assert not (work/name/'RESULTS').exists()
 results.append('PASS: '+name+' segment rejected; no finished MP4 created.')
out=capture(v480,136,720,'wrongheight');assert not out[-1]['ok'];results.append('PASS: wrong source resolution rejected, no upscale or transcode.')
# A logical segment may be delivered in separate range requests. Reconstruct it.
init,vparts=fragments(video)
split=[]
for seq,isinit,part in [(-1,True,init)]+[(i,False,p) for i,p in enumerate(vparts)]:
 cut=len(part)//2
 for offset,piece in [(0,part[:cut]),(cut,part[cut:])]:
  r=dict(session=sid,itag=136,segment=seq,request=10+seq*2+(offset>0)+2,headerId=1,init=isinit,start=max(seq,0)*2000,duration=2000 if not isinit else 0,length=len(part),offset=offset)
  split.append(dict(action='header',**r))
  for j in range(0,len(piece),12288):split.append(dict(action='chunk',**r,data=base64.b64encode(piece[j:j+12288]).decode()))
  split.append(dict(action='end',**r))
out=protocol([dict(action='start',session=sid,height=720,**identity)]+split+stream(audio,140,2)+[dict(action='finish',session=sid)],'partialranges');assert out[-1]['ok'],out
assert packet_hash(video,'v')==packet_hash(Path(out[-1]['path']),'v')
results.append('PASS: media/init segments split across byte-range requests reconstructed with unchanged video packets.')
imp=protocol([dict(action='import',input=base64.b64encode(str(source360).encode()).decode(),**identity)],'reuse');assert imp[-1]['ok'],imp
lookup=protocol([dict(action='lookup',**identity)],'reuse');assert lookup[-1]['cached']
mp3=protocol([dict(action='mp3',**identity)],'reuse');assert mp3[-1]['ok'],mp3
assert next(s for s in metadata(Path(mp3[-1]['path']))['streams'] if s['codec_type']=='audio')['codec_name']=='mp3'
results.append('PASS: existing 360p imported; MP3 generated from cached sound without any URL or network download.')
messages=[dict(action='start',session=sid,height=720,**identity)]+stream(video,136,1)+[dict(action='finish',session=sid)]
out=protocol(messages,'reuse');assert out[0]['audioCached'] and out[-1]['ok'],out
results.append('PASS: capture completed using cached 360p audio and no incoming audio segments.')
cancel=protocol([dict(action='start',session=sid,height=720,**identity)]+stream(video,136,1)+[dict(action='cancel',session=sid)],'cancel')
assert not (work/'cancel'/'CAPTURE_TEMP'/sid).exists();results.append('PASS: cancel removes capture scratch folder.')
# Verify remux of AV1/VP9 and Opus with the exact bundled FFmpeg.
for codec,encoder in [('vp9','libvpx-vp9'),('av1','libaom-av1')]:
 path=work/(codec+'.webm')
 args=['-f','lavfi','-i','color=c=green:s=854x480:r=5','-t','1','-an','-c:v',encoder,'-cpu-used','8']
 run(args+[str(path)])
 mp4=work/(codec+'.mp4');run(['-i',str(path),'-i',str(audio),'-map','0:v:0','-map','1:a:0','-c','copy','-strict','experimental',str(mp4)])
 assert packet_hash(path,'v')==packet_hash(mp4,'v')
 results.append('PASS: '+codec.upper()+' remuxed to MP4; encoded video packets unchanged.')
opus=work/'opus.webm';run(['-f','lavfi','-i','sine=sample_rate=48000','-t','1','-c:a','libopus',str(opus)])
opusmp4=work/'opus.mp4';run(['-i',str(video),'-i',str(opus),'-map','0:v','-map','1:a','-c','copy','-strict','experimental',str(opusmp4)])
assert packet_hash(opus,'a')==packet_hash(opusmp4,'a');results.append('PASS: Opus audio remuxed to MP4 without changing encoded packets.')
report='\n'.join(results)+'\n\nNot tested: live YouTube capture, actual Windows 7, Chrome 109 runtime.\n'
(work/'native_results.txt').write_text(report,'utf-8');print(report)
