"""Verify new media tools really supply the desktop audio outputs and input formats."""
from pathlib import Path
import subprocess,os,struct
R=Path(__file__).resolve().parents[1];T=R/'.test-artifacts/native';source=T/'input.m4a'
def run(args):return subprocess.check_output([str(x) for x in args],stderr=subprocess.STDOUT)
for fmt,codec,width,rate in [('f32le','pcm_f32le',4,44100),('s16le','pcm_s16le',2,8000)]:
 data=run([R/'ffmpeg.exe','-loglevel','error','-xerror','-i',source,'-map','0:a:0','-ac','2','-ar',rate,'-c:a',codec,'-f',fmt,'pipe:1'])
 assert abs(len(data)/(2*width*rate)-5)<.1
 print('PASS: actual',fmt,'audio pipe supplies full duration for preview/waveform')
flac=T/'fixture.flac';run([os.environ['YTD_FIXTURE_FFMPEG'],'-loglevel','error','-y','-i',source,'-c:a','flac',flac]);run([R/'ffmpeg.exe','-loglevel','error','-xerror','-i',flac,'-f','null','-'])
print('PASS: bundled FFmpeg fully decodes FLAC')
for name in ['YTWin7SabrHost.exe','BrowserSetup.exe','InstallerEngine.exe','PeakMP3/PeakMP3.exe','ffmpeg.exe','ffprobe.exe']:
 b=(R/name).read_bytes();pe=struct.unpack_from('<I',b,0x3c)[0];assert struct.unpack_from('<H',b,pe+4)[0]==0x14c,name
 if name not in ['ffmpeg.exe','ffprobe.exe']:
  start=b.index(b'BSJB');n=struct.unpack_from('<I',b,start+12)[0];assert b'v2.0.50727' in b[start+16:start+16+n],name
 print('PASS: x86'+(' / CLR2 metadata' if name not in ['ffmpeg.exe','ffprobe.exe'] else '')+': '+name)
