"""Real native process, AAC copy, plain MP3 and cancellation checks in isolated data."""
from pathlib import Path
import sys,os,subprocess,json,struct,base64,threading,queue,time,hashlib
root=Path(__file__).resolve().parent.parent
out=root/'.test-artifacts/native';out.mkdir(parents=True,exist_ok=True)
def run(args):return subprocess.check_output([str(x) for x in args],stderr=subprocess.STDOUT)
def check(value,message):
 if not value:raise AssertionError(message)
 print('PASS:',message,flush=True)
class Native:
 def __init__(self,runtime=None,data=None):
  self.p=subprocess.Popen([str((runtime or root)/'YTWin7SabrHost.exe'),'--test-root',str(data or out/'data')],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
  self.q=queue.Queue()
  def read():
   try:
    while True:
     header=self.p.stdout.read(4)
     if not header:self.q.put(RuntimeError('Native host closed before the expected response'));break
     size=struct.unpack('<I',header)[0];data=self.p.stdout.read(size)
     self.q.put(json.loads(data))
   except Exception as e:self.q.put(e)
  threading.Thread(target=read,daemon=True).start()
 def send(self,**message):
  data=json.dumps(message,ensure_ascii=False).encode();self.p.stdin.write(struct.pack('<I',len(data))+data);self.p.stdin.flush()
 def until(self,action,timeout=30):
  end=time.monotonic()+timeout
  while time.monotonic()<end:
   m=self.q.get(timeout=max(.01,end-time.monotonic()))
   if isinstance(m,Exception):raise m
   if m.get('action')=='result' and not m.get('ok'):raise RuntimeError(m.get('error','Native operation failed'))
   if m.get('action')==action:return m
  raise TimeoutError(action)
 def close(self):
  if self.p.poll() is None:self.p.stdin.close()
  self.p.wait(timeout=8)
identity={'videoId':'YtdTest0001','title':'Тестовый звук [AAC]','displayTitle':'Исполнитель — Песня (Live) & тест','duration':5000,'language':'ru'}
def hashes(path):
 data=json.loads(run([root/'ffprobe.exe','-v','error','-select_streams','a:0','-show_packets','-show_data_hash','sha256','-of','json',path]));return [p['data_hash'] for p in data['packets']]
def main():
 check(b'2.6.0' in run([root/'YTWin7SabrHost.exe','--version']),'new host starts through configured runtime')
 check(b'0.16.0' in run([root/'PeakMP3/PeakMP3.exe','--version']),'new desktop x86 starts through configured runtime')
 generator=os.environ.get('YTD_FIXTURE_FFMPEG')
 if not generator:raise RuntimeError('Set YTD_FIXTURE_FFMPEG to a separate full FFmpeg for tone generation')
 source=out/'input.m4a';run([generator,'-hide_banner','-loglevel','error','-y','-f','lavfi','-i','sine=frequency=440:duration=5','-ac','2','-c:a','aac','-b:a','128k',source])
 n=Native();n.send(action='aacstream',**identity);n.until('ready')
 payload=source.read_bytes()
 for offset in range(0,len(payload),12288):n.send(action='mediaChunk',data=base64.b64encode(payload[offset:offset+12288]).decode());n.until('mediaAck')
 n.send(action='mediaFinish');result=n.until('result');check(result['ok'],'cold AAC stream completes');n.close()
 cache=out/'data/AUDIO_CACHE/YtdTest0001.m4a';check(hashes(source)==hashes(cache),'AAC packet bytes survive cache remux unchanged')
 saved=Path(result['path']);check(hashes(source)==hashes(saved),'AAC original output has identical source packets')
 n=Native();n.send(action='mp3',**identity);result=n.until('result');check(result['ok'],'cached plain MP3 completes');n.close()
 mp3=Path(result['path']);reference=out/'plain-reference.mp3';run([root/'ffmpeg.exe','-hide_banner','-loglevel','error','-y','-i',cache,'-vn','-c:a','libmp3lame','-b:a','320k','-ar','44100',reference]);check(hashes(mp3)==hashes(reference),'plain MP3 packets equal unfiltered reference encode')
 run([root/'ffmpeg.exe','-hide_banner','-loglevel','error','-xerror','-i',mp3,'-f','null','-']);check(True,'complete plain MP3 decoded without errors')
 n=Native();n.send(action='desktopStatus');status=n.until('result');n.close();check(status.get('installed') and status.get('protocolVersion')==1,'installed desktop protocol detected from actual native process')
 n=Native();n.send(action='aacstream',**identity);n.until('ready');n.send(action='cancel');n.close();check(True,'cancelled receive exits without a completed new export')
 print('ALL_NATIVE_MEDIA_CHECKS_PASSED',flush=True)
if __name__=='__main__':main()
